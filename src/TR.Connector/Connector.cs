using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TR.Connectors.Api.Entities;
using TR.Connectors.Api.Interfaces;
using TR.Connector.DTOs;

namespace TR.Connector;

public partial class Connector : IConnector, IDisposable
{
    public ILogger Logger { get; set; }

    private string url = "";
    private string login = "";
    private string password = "";

    private HttpClient? _httpClient;
    private string token = "";

    //Пустой конструктор - пусть таким и остаётся для обратной совместимости
    public Connector() {}

    /// <summary>
    /// Избавляемся от повторяющейся инициализации HttpClient в каждом методе
    /// </summary>
    /// <returns>Экземпляр HttpClient для методов</returns>
    private HttpClient GetHttpClient()
    {
        if (_httpClient != null)
            return _httpClient;

        _httpClient = new HttpClient();

        if (!string.IsNullOrEmpty(url))
            _httpClient.BaseAddress = new Uri(url);

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        return _httpClient;
    }

    public void StartUp(string connectionString) // Переработаный StartUp, который теперь пробрасывает полный экземпляр HttpClient
    {
        foreach (var item in connectionString.Split(';'))
        {
            var parts = item.Split('=', 2);
            if (parts.Length != 2) continue;

            switch (parts[0].ToLowerInvariant())
            {
                case "url": url = parts[1]; break;
                case "login": login = parts[1]; break;
                case "password": password = parts[1]; break;
            }
        }
        
        var client = GetHttpClient();
        client.BaseAddress = new Uri(url);

        var body = new { login, password };
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            UnicodeEncoding.UTF8,
            "application/json");

        var response = client.PostAsync("api/v1/login", content).Result;
        var tokenResponse = JsonSerializer.Deserialize<ConnectorDTOs.TokenResponse>(
            response.Content.ReadAsStringAsync().Result);

        token = tokenResponse.Data.Access_token;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(url)
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task StartUpAsync(string connectionString, CancellationToken cT = default)
    {
        foreach (var item in connectionString.Split(';'))
        {
            var parts = item.Split('=', 2);
            if (parts.Length != 2) continue;

            switch (parts[0].ToLowerInvariant())
            {
                case "url": url = parts[1]; break;
                case "login": login = parts[1]; break;
                case "password": password = parts[1]; break;
            }
        }

        var client = GetHttpClient();
        client.BaseAddress = new Uri(url);

        var body = new { login, password };
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            UnicodeEncoding.UTF8,
            "application/json");

        var response = await client.PostAsync("api/v1/login", content, cT);
        var tokenResponse = JsonSerializer.Deserialize<ConnectorDTOs.TokenResponse>(
            await response.Content.ReadAsStringAsync(cT));

        token = tokenResponse.Data.Access_token;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(url)
        };
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken cT = default)
    {
        //var httpClient = new HttpClient(); - от этих строк...
        //httpClient.BaseAddress = new Uri(url); - ... можно избавиться...
        //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); - ... во всех методах...

        var httpClient = GetHttpClient(); // ... создавая httpClient специальным методом

        //Получаем ИТРоли
        var response = await httpClient.GetAsync("api/v1/roles/all", cT);
        var itRoleResponse = JsonSerializer.Deserialize<ConnectorDTOs.RoleResponse>(await response.Content.ReadAsStringAsync(cT));
        var itRolePermissions =
            itRoleResponse.Data.Select(_ => new Permission($"ItRole,{_.Id}", _.Name, _.CorporatePhoneNumber));

        //Получаем права
        response = await httpClient.GetAsync("api/v1/rights/all", cT);
        var RightResponse = JsonSerializer.Deserialize<ConnectorDTOs.RoleResponse>(await response.Content.ReadAsStringAsync(cT));
        var RightPermissions = RightResponse.Data.Select(_ =>
            new Permission($"RequestRight,{_.Id}", _.Name, _.CorporatePhoneNumber));

        return itRolePermissions.Concat(RightPermissions);
    }

    public IEnumerable<Permission> GetAllPermissions() // оставил синхронную обёртку, чтобы не править тесты
    {
        return GetAllPermissionsAsync().GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userLogin, CancellationToken cT = default)
    {
        var httpClient = GetHttpClient();

        //Получаем ИТРоли
        var response = await httpClient.GetAsync($"api/v1/users/{userLogin}/roles", cT);
        var itRoleResponse = JsonSerializer.Deserialize<ConnectorDTOs.UserRoleResponse>(response.Content.ReadAsStringAsync(cT).Result);
        var result1 = itRoleResponse.Data.Select(_ => $"ItRole,{_.Id}").ToList();

        //Получаем права
        response = await httpClient.GetAsync($"api/v1/users/{userLogin}/rights", cT);
        var RightResponse = JsonSerializer.Deserialize<ConnectorDTOs.UserRoleResponse>(response.Content.ReadAsStringAsync(cT).Result);
        var result2 = RightResponse.Data.Select(_ => $"RequestRight,{_.Id}").ToList();

        return result1.Concat(result2).ToList();
    }

    public IEnumerable<string> GetUserPermissions(string userLogin)
    {
        return GetUserPermissionsAsync(userLogin).GetAwaiter().GetResult();
    }

    public async Task AddUserPermissionsAsync(string userLogin, IEnumerable<string> rightIds, CancellationToken cT = default)
    {
        var httpClient = GetHttpClient();

        //проверяем что пользователь не залочен.
        var response = await httpClient.GetAsync($"api/v1/users/all", cT);
        var userResponse = JsonSerializer.Deserialize<ConnectorDTOs.UserResponse>(await response.Content.ReadAsStringAsync(cT));
        var user = userResponse.Data.FirstOrDefault(_ => _.Login == userLogin);

        if (user != null && user.Status == "Lock")
        {
            Logger.Error($"Пользователь {userLogin} залочен.");
            return;
        }
        //Назначаем права.
        else if (user != null && user.Status == "Unlock") // переработано на асинхронность
        {
            var tasks = new List<Task>();

            foreach (var rightId in rightIds)
            {
                var rightStr = rightId.Split(',');
                Task task = rightStr[0] switch
                {
                    "ItRole" => httpClient.PutAsync(
                        $"api/v1/users/{userLogin}/add/role/{rightStr[1]}",
                        null,
                        cT),
                    "RequestRight" => httpClient.PutAsync(
                        $"api/v1/users/{userLogin}/add/right/{rightStr[1]}",
                        null,
                        cT),
                    _ => throw new Exception($"Тип доступа {rightStr[0]} не определен")
                };

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                if (task is Task<HttpResponseMessage> httpTask)
                {
                    var httpResponse = await httpTask;
                }
            }
        }
    }

    public void AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
    {
        AddUserPermissionsAsync(userLogin, rightIds).GetAwaiter().GetResult();
    }

    public async Task RemoveUserPermissionsAsync(string userLogin, IEnumerable<string> rightIds, CancellationToken cT = default)
    {
        var httpClient = GetHttpClient();

        //проверяем что пользователь не залочен.
        var response = await httpClient.GetAsync($"api/v1/users/all", cT);
        var userResponse = JsonSerializer.Deserialize<ConnectorDTOs.UserResponse>(await response.Content.ReadAsStringAsync(cT));
        var user = userResponse.Data.FirstOrDefault(_ => _.Login == userLogin);

        if (user != null && user.Status == "Lock")
        {
            Logger.Error($"Пользователь {userLogin} залочен.");
            return;
        }
        //отзываем права.
        else if (user != null && user.Status == "Unlock") // тоже переработано на асинхронность
        {
            var tasks = new List<Task>();

            foreach (var rightId in rightIds)
            {
                var rightStr = rightId.Split(',');
                Task task = rightStr[0] switch
                {
                    "ItRole" => httpClient.DeleteAsync($"api/v1/users/{userLogin}/drop/role/{rightStr[1]}", cT),
                    "RequestRight" => httpClient.DeleteAsync($"api/v1/users/{userLogin}/drop/right/{rightStr[1]}", cT),
                    _ => throw new Exception($"Тип доступа {rightStr[0]} не определен")
                };

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                if (task is Task<HttpResponseMessage> httpTask)
                {
                    var httpResponse = await httpTask;
                }
            }
        }
    }

    public void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
    {
        RemoveUserPermissionsAsync(userLogin, rightIds).GetAwaiter().GetResult();
    }

    public Task<IEnumerable<Property>> GetAllPropertiesAsync(CancellationToken cT = default)
    {
        var props = new List<Property>();
        foreach (var propertyInfo in new ConnectorDTOs.UserPropertyData().GetType().GetProperties())
        {
            if (propertyInfo.Name == "login") continue;

            props.Add(new Property(propertyInfo.Name, propertyInfo.Name));
        }
        return Task.FromResult<IEnumerable<Property>>(props);
    }

    public IEnumerable<Property> GetAllProperties()
    {
        return GetAllPropertiesAsync().GetAwaiter().GetResult();
    }

    public async Task<IEnumerable<UserProperty>> GetUserPropertiesAsync(string userLogin, CancellationToken cT = default)
    {
        var httpClient = GetHttpClient();

        var response = await httpClient.GetAsync($"api/v1/users/{userLogin}", cT);
        var userResponse = JsonSerializer.Deserialize<ConnectorDTOs.UserPropertyResponse>(await response.Content.ReadAsStringAsync(cT));

        var user = userResponse.Data ?? throw new NullReferenceException($"Пользователь {userLogin} не найден");

        if (user.Status == "Lock")
            throw new Exception($"Невозможно получить свойства, пользователь {userLogin} залочен");

        return user.GetType().GetProperties()
            .Select(_ => new UserProperty(_.Name, _.GetValue(user) as string));
    }

    public IEnumerable<UserProperty> GetUserProperties(string userLogin)
    {
        return GetUserPropertiesAsync(userLogin).GetAwaiter().GetResult();
    }

    public async Task UpdateUserPropertiesAsync(IEnumerable<UserProperty> properties, string userLogin, CancellationToken cT = default)
    {
        var httpClient = GetHttpClient();

        var response = await httpClient.GetAsync($"api/v1/users/{userLogin}", cT);
        var userResponse = JsonSerializer.Deserialize<ConnectorDTOs.UserPropertyResponse>(await response.Content.ReadAsStringAsync(cT));

        var user = userResponse.Data ?? throw new NullReferenceException($"Пользователь {userLogin} не найден");
        if (user.Status == "Lock")
            throw new Exception($"Невозможно обновить свойства, пользователь {userLogin} залочен");

        foreach (var property in properties)
        {
            foreach (var userProp in user.GetType().GetProperties())
            {
                if (property.Name == userProp.Name)
                {
                    userProp.SetValue(user, property.Value);
                }
            }
        }

        var content = new StringContent(JsonSerializer.Serialize(user), UnicodeEncoding.UTF8, "application/json");
        httpClient.PutAsync("api/v1/users/edit", content, cT).Wait(cT);
    }

    public void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
    {
        UpdateUserPropertiesAsync(properties, userLogin).GetAwaiter().GetResult();
    }

    public async Task<bool> IsUserExistsAsync(string userLogin, CancellationToken cT = default)
    {
        var httpClient = GetHttpClient();

        var response = await httpClient.GetAsync($"api/v1/users/all", cT);
        var userResponse = JsonSerializer.Deserialize<ConnectorDTOs.UserResponse>(response.Content.ReadAsStringAsync(cT).Result);
        var user = userResponse.Data.FirstOrDefault(_ => _.Login == userLogin);

        if (user != null) return true;

        return false;
    }

    public bool IsUserExists(string userLogin)
    {
        return IsUserExistsAsync(userLogin).GetAwaiter().GetResult();
    }

    public void CreateUser(UserToCreate user)
    {
        var httpClient = GetHttpClient();

        var newUser = new ConnectorDTOs.CreateUserDTO()
        {
            Login = user.Login,
            Password = user.HashPassword,

            LastName = user.Properties.FirstOrDefault(p => p.Name.Equals("lastName", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty,
            FirstName = user.Properties.FirstOrDefault(p => p.Name.Equals("firstName", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty,
            MiddleName = user.Properties.FirstOrDefault(p => p.Name.Equals("middleName", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty,

            TelephoneNumber = user.Properties.FirstOrDefault(p => p.Name.Equals("telephoneNumber", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty,
            IsLead = bool.TryParse(user.Properties.FirstOrDefault(p => p.Name.Equals("isLead", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty, out bool isLeadValue) && isLeadValue,

            Status = string.Empty
        };

        var content = new StringContent(JsonSerializer.Serialize(newUser), UnicodeEncoding.UTF8, "application/json");
        httpClient.PostAsync("api/v1/users/create", content).Wait();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}

using TR.Connectors.Api.Entities;

namespace TR.Connectors.Api.Interfaces;

// хороший тон - комментарии к методам и классам, добавил их

/// <summary>
/// Ключевой класс, обеспечивающий подключение и взаимодействие с API
/// </summary>
public interface IConnector
{
    public ILogger Logger { get; set; }

    /// <summary>
    /// Синхронный метод инициализации подключения (для обратной совместимости)
    /// </summary>
    /// <param name="connectionString"></param>
    void StartUp(string connectionString); // это метод можно и оставить для обратной совместимости...

    /// <summary>
    /// Асинхронный метод инициализации подключения
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="cT"></param>
    /// <returns></returns>
    Task StartUpAsync(string connectionString, CancellationToken cT = default); // ... добавив при этом его асинхронную версию
    
    // остальные методы переделал на асинхронные, а ещё не удобно было, что методы в интерфейсе и в классе в не по очереди
    
    /// <summary>
    /// Получает список всех доступных прав и ролей
    /// </summary>
    /// <returns>Строка с перечнем ролей и прав</returns>
    Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken cT = default);

    IEnumerable<Permission> GetAllPermissions(); // тестики-то работают на синхронных методах, значит их пока придётся оставить

    /// <summary>
    /// Получает перечень прав и конкретного пользователя
    /// </summary>
    /// <param name="userLogin"></param>
    /// <returns>Перечень прав в виде списка</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userLogin, CancellationToken cT = default);

    IEnumerable<string> GetUserPermissions(string userLogin);

    /// <summary>
    /// Ищет пользователя по его логину и добавляет ему права
    /// </summary>
    /// <param name="userLogin"></param>
    /// <param name="rightIds"></param>
    Task AddUserPermissionsAsync(string userLogin, IEnumerable<string> rightIds, CancellationToken cT = default);

    void AddUserPermissions(string userLogin, IEnumerable<string> rightIds);

    /// <summary>
    /// Ищет пользователя по его логину и удаляет его права
    /// </summary>
    /// <param name="userLogin"></param>
    /// <param name="rightIds"></param>
    Task RemoveUserPermissionsAsync(string userLogin, IEnumerable<string> rightIds, CancellationToken cT = default);

    void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds);

    /// <summary>
    /// Получает список свойств пользователей
    /// </summary>
    /// <returns>Перечень пользователей и их свойств в виде списка</returns>
    Task<IEnumerable<Property>> GetAllPropertiesAsync(CancellationToken cT = default);

    IEnumerable<Property> GetAllProperties();

    /// <summary>
    /// Ищет пользователя по его логину и возвращает его права
    /// </summary>
    /// <param name="userLogin"></param>
    /// <returns>Права конкретного пользователя в виде списка</returns>
    Task<IEnumerable<UserProperty>> GetUserPropertiesAsync(string userLogin, CancellationToken cT = default);

    IEnumerable<UserProperty> GetUserProperties(string userLogin);

    /// <summary>
    /// Ищет пользователя по его логину и обновляет свойства на новые
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="userLogin"></param>
    Task UpdateUserPropertiesAsync(IEnumerable<UserProperty> properties, string userLogin, CancellationToken cT = default);

    void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin);


    /// <summary>
    /// Проверяет существование пользователя по логину
    /// </summary>
    /// <param name="userLogin"></param>
    /// <returns>True или False</returns>
    Task<bool> IsUserExistsAsync(string userLogin, CancellationToken cT = default);

    bool IsUserExists(string userLogin);

    /// <summary>
    /// Создаёт нового пользователя
    /// </summary>
    /// <param name="user"></param>
    void CreateUser(UserToCreate user);

}

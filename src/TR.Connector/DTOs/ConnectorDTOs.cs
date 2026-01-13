namespace TR.Connector.DTOs; // перенесено в отдельную папку и переименовано

public partial class ConnectorDTOs
{
    //-------TokenResponse------------//
    internal class TokenResponseData
    {
        public string Access_token { get; set; }
        public int Expires_in { get; set; }
    }

    internal class TokenResponse
    {
        public TokenResponseData Data { get; set; }
        public bool Success { get; set; }
        public object? ErrorText { get; set; }
        public object Count { get; set; }
    }
    //-------TokenResponse------------//

    //-------RoleResponse------------//
    internal class RoleResponseData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string CorporatePhoneNumber { get; set; }
    }

    internal class RoleResponse
    {
        public List<RoleResponseData> Data { get; set; }
        public bool Success { get; set; }
        public object? ErrorText { get; set; }
        public int Count { get; set; }
    }
    //-------RoleResponse------------//

    //-------RightResponse------------//
    internal class RightResponseData
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public object Users { get; set; }
    }

    internal class RightResponse
    {
        public List<RightResponseData> Data { get; set; }
        public bool Success { get; set; }
        public object? ErrorText { get; set; }
        public int Count { get; set; }
    }
    //-------RightResponse------------//


    //-------UserRoleResponse------------//
    internal class UserRoleResponse
    {
        public List<RoleResponseData> Data { get; set; }
        public bool Success { get; set; }
        public object? ErrorText { get; set; }
        public int Count { get; set; }
    }
    //-------UserRoleResponse------------//

    //-------UserRightResponse------------//
    internal class UserRightResponse
    {
        public List<RightResponseData> Data { get; set; }
        public bool Success { get; set; }
        public object? ErrorText { get; set; }
        public int Count { get; set; }
    }
    //-------UserRightResponse------------//


    //-------UserResponse------------//
    internal class UserResponseData
    {
        public string Login { get; set; }
        public string Status { get; set; }
    }

    internal class UserResponse
    {
        public List<UserResponseData> Data { get; set; }
        public bool Success { get; set; }
        public object? ErrorText { get; set; }
        public int Count { get; set; }
    }
    //-------UserResponse------------//

    //-------UserPropertyResponse------------//
    internal class UserPropertyData
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string TelephoneNumber { get; set; }
        public bool IsLead { get; set; }
        public string Login { get; set; }
        public string Status { get; set; }
    }

    internal class UserPropertyResponse
    {
        public UserPropertyData Data { get; set; }
        public bool Success { get; set; }
        public object? ErrorText { get; set; }
        public int Count { get; set; }
    }

    internal class CreateUserDTO : UserPropertyData
    {
        public string Password { get; set; }
    }
    //-------UserPropertyResponse------------//
}

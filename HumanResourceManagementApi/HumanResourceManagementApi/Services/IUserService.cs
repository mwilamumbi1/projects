namespace Core.HumanResourceManagementApi.Services
{
    public interface IUserService
    {
        Task<List<string>> GetPermissionsByUserIdAsync(int userId);
    }
}
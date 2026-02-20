using Core.HumanResourceManagementApi.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Core.HumanResourceManagementApi.Services
{
    public class UserService : IUserService
    {
        private readonly HRDataContext _context;

        public UserService(HRDataContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetPermissionsByUserIdAsync(int userId)
        {
            var permissions = new List<string>();
 
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "core.GetPermissionsByUserId"; // Replace with your actual stored procedure name
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@UserID", userId));

            if (command.Connection.State != ConnectionState.Open)
            {
                await command.Connection.OpenAsync();
            }

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                permissions.Add(reader.GetString(reader.GetOrdinal("PermissionName")));
            }

            return permissions;
        }
    }
}
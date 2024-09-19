using Dapper;
using Npgsql;
using System.Data.SqlClient;

namespace PeopleManagement.Application.Queries.Employee
{
    public class EmployeeQueries(string connectionString) : IEmployeeQueries
    {
        private string _connectionString = connectionString;


        public async Task<IEnumerable<EmployeeSimpleDto>> GetEmployeeList(int pageSize, int pageNumber)
        {
            var sql = @"
                    SELECT 
                        e.Id, 
                        e.Name, 
                        e.Registration, 
                        e.Status, 
                        e.RoleId, 
                        r.Name AS RoleName
                    FROM Employees e
                    LEFT JOIN Roles r ON e.RoleId = r.Id
                    ORDER BY e.Name
                    LIMIT @PageSize OFFSET (@Offset);";

            var parameters = new
            {
                PageSize = pageSize,
                Offset = (pageNumber - 1) * pageSize
            };

            using var connection = new NpgsqlConnection(_connectionString);
            
            return await connection.QueryAsync<EmployeeSimpleDto>(sql, parameters);
            
        }
 

    }
}

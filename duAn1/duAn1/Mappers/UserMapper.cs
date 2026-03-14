using duAn1.Models;
using Microsoft.Data.SqlClient;

namespace duAn1.Mappers
{
    public static class UserMapper
    {
        public static User UserToMap(SqlDataReader reader)
        {
            var user = new User
            {
                Id = Convert.ToInt32(reader["id"]),
                Actived = Convert.ToBoolean(reader["actived"]),
                Avatar = reader["avata"]?.ToString(),
                Email = reader["email"]?.ToString(),
                FullName = reader["fullname"]?.ToString(),
                Password = reader["password"]?.ToString(),
                Role = Convert.ToInt32(reader["role"]),
            };

            return user;
        }
        public static List<User> UserListToMap(SqlDataReader reader)
        {
            var list = new List<User>();
            while (reader.Read())
            {
                list.Add(UserToMap(reader));
            }
            return list;
        }
    }
}

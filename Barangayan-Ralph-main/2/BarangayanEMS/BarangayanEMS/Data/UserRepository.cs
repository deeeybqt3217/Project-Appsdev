using System;
using System.Data.SQLite;
using System.Globalization;

namespace BarangayanEMS.Data
{
    internal sealed class UserRepository
    {
        private readonly PasswordHasher _passwordHasher = new PasswordHasher();

        internal bool TryCreateUser(string firstName, string lastName, string email, string password, out string errorMessage)
        {
            string normalizedEmail = NormalizeEmail(email);

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                errorMessage = "First and last name are required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                errorMessage = "A valid email address is required.";
                return false;
            }

            if (EmailExists(normalizedEmail))
            {
                errorMessage = "An account with this email already exists.";
                return false;
            }

            string passwordHash = _passwordHasher.HashPassword(password, out string passwordSalt);

            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"INSERT INTO Users (FirstName, LastName, Email, PasswordHash, PasswordSalt)
VALUES (@firstName, @lastName, @email, @hash, @salt);";
                    command.Parameters.AddWithValue("@firstName", firstName.Trim());
                    command.Parameters.AddWithValue("@lastName", lastName.Trim());
                    command.Parameters.AddWithValue("@email", normalizedEmail);
                    command.Parameters.AddWithValue("@hash", passwordHash);
                    command.Parameters.AddWithValue("@salt", passwordSalt);
                    command.ExecuteNonQuery();
                }
            }

            errorMessage = string.Empty;
            return true;
        }

        internal bool TryAuthenticate(string email, string password, out UserRecord user)
        {
            user = GetByEmail(email);
            if (user == null)
            {
                return false;
            }

            if (!_passwordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
            {
                user = null;
                return false;
            }

            return true;
        }

        private bool EmailExists(string normalizedEmail)
        {
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT 1 FROM Users WHERE Email = @email LIMIT 1";
                    command.Parameters.AddWithValue("@email", normalizedEmail);
                    object result = command.ExecuteScalar();
                    return result != null && result != DBNull.Value;
                }
            }
        }

        private UserRecord GetByEmail(string email)
        {
            string normalizedEmail = NormalizeEmail(email);
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return null;
            }

            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"SELECT Id, FirstName, LastName, Email, PasswordHash, PasswordSalt, CreatedAt
FROM Users
WHERE Email = @email
LIMIT 1;";
                    command.Parameters.AddWithValue("@email", normalizedEmail);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        return MapUser(reader);
                    }
                }
            }
        }

        private static UserRecord MapUser(SQLiteDataReader reader)
        {
            return new UserRecord
            {
                Id = reader.GetInt64(reader.GetOrdinal("Id")),
                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                PasswordSalt = reader.GetString(reader.GetOrdinal("PasswordSalt")),
                CreatedAt = DateTime.TryParse(reader["CreatedAt"].ToString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime created)
                    ? created
                    : DateTime.UtcNow
            };
        }

        private static string NormalizeEmail(string email)
        {
            return string.IsNullOrWhiteSpace(email)
                ? string.Empty
                : email.Trim().ToLowerInvariant();
        }
    }
}

using System;
using System.Data.SQLite;
using System.IO;

namespace BarangayanEMS.Data
{
    internal static class DatabaseBootstrapper
    {
        private const string DatabaseFolderName = "Database";
        private const string DatabaseFileName = "BarangayanEMS.db";
        private static readonly string DatabaseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DatabaseFolderName);
        private static readonly string DatabasePath = Path.Combine(DatabaseDirectory, DatabaseFileName);
        private static readonly string ConnectionStringValue = $"Data Source={DatabasePath};Version=3;Foreign Keys=True;";

        internal static string ConnectionString => ConnectionStringValue;

        internal static void EnsureCreated()
        {
            if (!Directory.Exists(DatabaseDirectory))
            {
                Directory.CreateDirectory(DatabaseDirectory);
            }

            bool isNewDatabase = !File.Exists(DatabasePath);
            if (isNewDatabase)
            {
                SQLiteConnection.CreateFile(DatabasePath);
            }

            using (var connection = new SQLiteConnection(ConnectionStringValue))
            {
                connection.Open();
                EnableForeignKeys(connection);
                CreateTables(connection);

                if (isNewDatabase)
                {
                    SeedDemoUser(connection);
                }
            }
        }

        private static void EnableForeignKeys(SQLiteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA foreign_keys = ON;";
                command.ExecuteNonQuery();
            }
        }

        private static void CreateTables(SQLiteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    FirstName TEXT NOT NULL,
    LastName TEXT NOT NULL,
    Email TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    PasswordSalt TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);
";
                command.ExecuteNonQuery();
            }
        }

        private static void SeedDemoUser(SQLiteConnection connection)
        {
            var hasher = new PasswordHasher();
            string passwordHash = hasher.HashPassword("demo123", out string passwordSalt);

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"INSERT INTO Users (FirstName, LastName, Email, PasswordHash, PasswordSalt)
VALUES (@firstName, @lastName, @email, @hash, @salt);";
                command.Parameters.AddWithValue("@firstName", "Demo");
                command.Parameters.AddWithValue("@lastName", "Resident");
                command.Parameters.AddWithValue("@email", "demo@barangayan.gov");
                command.Parameters.AddWithValue("@hash", passwordHash);
                command.Parameters.AddWithValue("@salt", passwordSalt);
                command.ExecuteNonQuery();
            }
        }
    }
}

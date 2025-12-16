using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace BarangayanEMS.Data
{
    internal sealed class FeedbackRepository
    {
        internal sealed class FeedbackRecord
        {
            public long Id { get; set; }
            public string Type { get; set; }
            public string Message { get; set; }
            public DateTime CreatedAt { get; set; }
            public string UserName { get; set; }
        }

        public FeedbackRepository()
        {
            EnsureTable();
        }

        private void EnsureTable()
        {
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Feedbacks (
Id INTEGER PRIMARY KEY AUTOINCREMENT,
Type TEXT NOT NULL,
Message TEXT NOT NULL,
CreatedAt TEXT NOT NULL,
UserName TEXT
);";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Insert(string type, string message, string userName)
        {
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Feedbacks (Type, Message, CreatedAt, UserName)
VALUES (@Type, @Message, @CreatedAt, @UserName);";
                    cmd.Parameters.AddWithValue("@Type", type ?? "General");
                    cmd.Parameters.AddWithValue("@Message", message ?? string.Empty);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@UserName", userName ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<FeedbackRecord> GetLatest(int count = 50)
        {
            var list = new List<FeedbackRecord>();
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id, Type, Message, CreatedAt, UserName FROM Feedbacks ORDER BY Id DESC LIMIT @Count";
                    cmd.Parameters.AddWithValue("@Count", count);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new FeedbackRecord
                            {
                                Id = r.GetInt64(r.GetOrdinal("Id")),
                                Type = r["Type"].ToString(),
                                Message = r["Message"].ToString(),
                                CreatedAt = DateTime.TryParse(r["CreatedAt"].ToString(), out var dt) ? dt : DateTime.UtcNow,
                                UserName = r["UserName"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace BarangayanEMS.Data
{
    internal sealed class DocumentRequestRepository
    {
        internal sealed class DocumentRequestRecord
        {
            public long Id { get; set; }
            public string RequestId { get; set; }
            public string Type { get; set; }
            public string RequesterName { get; set; }
            public DateTime DateFiled { get; set; }
            public string Status { get; set; }
            public string ContactNumber { get; set; }
            public string Purpose { get; set; }
            public DateTime? PickupDate { get; set; }
            public int Copies { get; set; }
            public string AdditionalRequirements { get; set; }
        }

        public List<DocumentRequestRecord> GetAll()
        {
            var results = new List<DocumentRequestRecord>();
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, RequestId, Type, RequesterName, DateFiled, Status, ContactNumber, Purpose, PickupDate, Copies, AdditionalRequirements
FROM DocumentRequests
ORDER BY Id DESC;";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            results.Add(new DocumentRequestRecord
                            {
                                Id = r.GetInt64(r.GetOrdinal("Id")),
                                RequestId = r["RequestId"].ToString(),
                                Type = r["Type"].ToString(),
                                RequesterName = r["RequesterName"].ToString(),
                                DateFiled = DateTime.TryParse(r["DateFiled"].ToString(), out var df) ? df : DateTime.Today,
                                Status = r["Status"].ToString(),
                                ContactNumber = r["ContactNumber"].ToString(),
                                Purpose = r["Purpose"].ToString(),
                                PickupDate = DateTime.TryParse(r["PickupDate"].ToString(), out var pd) ? (DateTime?)pd : null,
                                Copies = int.TryParse(r["Copies"].ToString(), out var c) ? c : 1,
                                AdditionalRequirements = r["AdditionalRequirements"].ToString()
                            });
                        }
                    }
                }
            }
            return results;
        }

        public DocumentRequestRecord GetByRequestId(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return null;
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, RequestId, Type, RequesterName, DateFiled, Status, ContactNumber, Purpose, PickupDate, Copies, AdditionalRequirements
FROM DocumentRequests WHERE RequestId = @RequestId LIMIT 1;";
                    cmd.Parameters.AddWithValue("@RequestId", requestId);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;
                        return new DocumentRequestRecord
                        {
                            Id = r.GetInt64(r.GetOrdinal("Id")),
                            RequestId = r["RequestId"].ToString(),
                            Type = r["Type"].ToString(),
                            RequesterName = r["RequesterName"].ToString(),
                            DateFiled = DateTime.TryParse(r["DateFiled"].ToString(), out var df) ? df : DateTime.Today,
                            Status = r["Status"].ToString(),
                            ContactNumber = r["ContactNumber"].ToString(),
                            Purpose = r["Purpose"].ToString(),
                            PickupDate = DateTime.TryParse(r["PickupDate"].ToString(), out var pd) ? (DateTime?)pd : null,
                            Copies = int.TryParse(r["Copies"].ToString(), out var c) ? c : 1,
                            AdditionalRequirements = r["AdditionalRequirements"].ToString()
                        };
                    }
                }
            }
        }

        public string Insert(DocumentRequestRecord rec)
        {
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                // Compute next RequestId based on next Id
                long nextId;
                using (var idCmd = connection.CreateCommand())
                {
                    idCmd.CommandText = "SELECT IFNULL(MAX(Id),0)+1 FROM DocumentRequests";
                    object val = idCmd.ExecuteScalar();
                    nextId = (val == null || val == DBNull.Value) ? 1 : Convert.ToInt64(val);
                }
                string requestId = $"REQ-{nextId:D4}";

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO DocumentRequests
(RequestId, Type, RequesterName, DateFiled, Status, ContactNumber, Purpose, PickupDate, Copies, AdditionalRequirements)
VALUES
(@RequestId, @Type, @RequesterName, @DateFiled, @Status, @ContactNumber, @Purpose, @PickupDate, @Copies, @AdditionalRequirements);";
                    cmd.Parameters.AddWithValue("@RequestId", requestId);
                    cmd.Parameters.AddWithValue("@Type", rec.Type ?? "");
                    cmd.Parameters.AddWithValue("@RequesterName", rec.RequesterName ?? "");
                    cmd.Parameters.AddWithValue("@DateFiled", rec.DateFiled == default(DateTime) ? DateTime.Today : rec.DateFiled);
                    cmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(rec.Status) ? "Pending" : rec.Status);
                    cmd.Parameters.AddWithValue("@ContactNumber", rec.ContactNumber ?? "");
                    cmd.Parameters.AddWithValue("@Purpose", rec.Purpose ?? "");
                    cmd.Parameters.AddWithValue("@PickupDate", (object)(rec.PickupDate?.ToString("yyyy-MM-dd")) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Copies", rec.Copies <= 0 ? 1 : rec.Copies);
                    cmd.Parameters.AddWithValue("@AdditionalRequirements", rec.AdditionalRequirements ?? "");
                    cmd.ExecuteNonQuery();
                }
                return requestId;
            }
        }

        public void UpdateDetails(DocumentRequestRecord rec)
        {
            if (rec == null || string.IsNullOrWhiteSpace(rec.RequestId)) return;
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE DocumentRequests SET
Type = @Type,
RequesterName = @RequesterName,
ContactNumber = @ContactNumber,
Purpose = @Purpose,
PickupDate = @PickupDate,
Copies = @Copies,
AdditionalRequirements = @AdditionalRequirements
WHERE RequestId = @RequestId";
                    cmd.Parameters.AddWithValue("@Type", rec.Type ?? "");
                    cmd.Parameters.AddWithValue("@RequesterName", rec.RequesterName ?? "");
                    cmd.Parameters.AddWithValue("@ContactNumber", rec.ContactNumber ?? "");
                    cmd.Parameters.AddWithValue("@Purpose", rec.Purpose ?? "");
                    cmd.Parameters.AddWithValue("@PickupDate", (object)(rec.PickupDate?.ToString("yyyy-MM-dd")) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Copies", rec.Copies <= 0 ? 1 : rec.Copies);
                    cmd.Parameters.AddWithValue("@AdditionalRequirements", rec.AdditionalRequirements ?? "");
                    cmd.Parameters.AddWithValue("@RequestId", rec.RequestId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateStatus(string requestId, string newStatus)
        {
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "UPDATE DocumentRequests SET Status = @Status WHERE RequestId = @RequestId";
                    cmd.Parameters.AddWithValue("@Status", newStatus ?? "Pending");
                    cmd.Parameters.AddWithValue("@RequestId", requestId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(string requestId)
        {
            using (var connection = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM DocumentRequests WHERE RequestId = @RequestId";
                    cmd.Parameters.AddWithValue("@RequestId", requestId);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

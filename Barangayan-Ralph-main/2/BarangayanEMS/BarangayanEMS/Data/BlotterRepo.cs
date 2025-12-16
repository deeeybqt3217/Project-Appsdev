using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace BarangayanEMS.Data
{
    // Fallback definition to fix missing compile include for BlotterRepository
    internal sealed class BlotterRepository
    {
        internal sealed class BlotterRecord
        {
            public long Id { get; set; }
            public string CaseNo { get; set; }
            public string ReportType { get; set; }
            public string PriorityLevel { get; set; }
            public string Barangay { get; set; }
            public string Complainant { get; set; }
            public string Respondent { get; set; }
            public DateTime IncidentDate { get; set; }
            public string IncidentLocation { get; set; }
            public string Description { get; set; }
            public string Witnesses { get; set; }
            public string Status { get; set; }
            public DateTime DateReported { get; set; }
        }

        public List<BlotterRecord> GetAll()
        {
            var list = new List<BlotterRecord>();
            using (var conn = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Id, CaseNo, ReportType, PriorityLevel, Barangay, Complainant, Respondent, IncidentDate, IncidentLocation, Description, Witnesses, Status, DateReported FROM BlotterReports ORDER BY Id DESC";
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new BlotterRecord
                            {
                                Id = r.GetInt64(r.GetOrdinal("Id")),
                                CaseNo = r["CaseNo"].ToString(),
                                ReportType = r["ReportType"].ToString(),
                                PriorityLevel = r["PriorityLevel"].ToString(),
                                Barangay = r["Barangay"].ToString(),
                                Complainant = r["Complainant"].ToString(),
                                Respondent = r["Respondent"].ToString(),
                                IncidentDate = DateTime.TryParse(r["IncidentDate"].ToString(), out var d) ? d : DateTime.Today,
                                IncidentLocation = r["IncidentLocation"].ToString(),
                                Description = r["Description"].ToString(),
                                Witnesses = r["Witnesses"].ToString(),
                                Status = r["Status"].ToString(),
                                DateReported = DateTime.TryParse(r["DateReported"].ToString(), out var dr) ? dr : DateTime.Today
                            });
                        }
                    }
                }
            }
            return list;
        }

        public string Insert(BlotterRecord rec)
        {
            using (var conn = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                conn.Open();

                long nextId;
                using (var idCmd = conn.CreateCommand())
                {
                    idCmd.CommandText = "SELECT IFNULL(MAX(Id),0)+1 FROM BlotterReports";
                    object val = idCmd.ExecuteScalar();
                    nextId = (val == null || val == DBNull.Value) ? 1 : Convert.ToInt64(val);
                }
                string caseNo = $"BL-{nextId:D4}";

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO BlotterReports
(CaseNo, ReportType, PriorityLevel, Barangay, Complainant, Respondent, IncidentDate, IncidentLocation, Description, Witnesses, Status)
VALUES
(@CaseNo, @ReportType, @PriorityLevel, @Barangay, @Complainant, @Respondent, @IncidentDate, @IncidentLocation, @Description, @Witnesses, @Status);";
                    cmd.Parameters.AddWithValue("@CaseNo", caseNo);
                    cmd.Parameters.AddWithValue("@ReportType", rec.ReportType ?? "Other");
                    cmd.Parameters.AddWithValue("@PriorityLevel", rec.PriorityLevel ?? "Low");
                    cmd.Parameters.AddWithValue("@Barangay", rec.Barangay ?? "");
                    cmd.Parameters.AddWithValue("@Complainant", rec.Complainant ?? "");
                    cmd.Parameters.AddWithValue("@Respondent", rec.Respondent ?? "");
                    cmd.Parameters.AddWithValue("@IncidentDate", rec.IncidentDate.ToString("yyyy-MM-dd"));
                    cmd.Parameters.AddWithValue("@IncidentLocation", rec.IncidentLocation ?? "");
                    cmd.Parameters.AddWithValue("@Description", rec.Description ?? "");
                    cmd.Parameters.AddWithValue("@Witnesses", rec.Witnesses ?? "");
                    cmd.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(rec.Status) ? "Pending" : rec.Status);
                    cmd.ExecuteNonQuery();
                }
                return caseNo;
            }
        }

        public void Delete(string caseNo)
        {
            using (var conn = new SQLiteConnection(DatabaseBootstrapper.ConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM BlotterReports WHERE CaseNo = @CaseNo";
                    cmd.Parameters.AddWithValue("@CaseNo", caseNo);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}

using System;

namespace BarangayanEMS.Data
{
    internal sealed class UserRecord
    {
        internal long Id { get; set; }
        internal string FirstName { get; set; }
        internal string LastName { get; set; }
        internal string Email { get; set; }
        internal string PasswordHash { get; set; }
        internal string PasswordSalt { get; set; }
        internal DateTime CreatedAt { get; set; }
    }
}

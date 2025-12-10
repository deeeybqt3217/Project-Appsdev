using System;
using System.Security.Cryptography;

namespace BarangayanEMS.Data
{
    internal sealed class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        internal string HashPassword(string password, out string salt)
        {
            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            salt = Convert.ToBase64String(saltBytes);
            return HashPassword(password, salt);
        }

        internal bool Verify(string password, string hash, string salt)
        {
            string computed = HashPassword(password, salt);
            return AreEqual(Convert.FromBase64String(computed), Convert.FromBase64String(hash));
        }

        private string HashPassword(string password, string salt)
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                return Convert.ToBase64String(pbkdf2.GetBytes(KeySize));
            }
        }

        private static bool AreEqual(byte[] first, byte[] second)
        {
            if (first.Length != second.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < first.Length; i++)
            {
                diff |= first[i] ^ second[i];
            }

            return diff == 0;
        }
    }
}

using System;
using System.Security.Cryptography;

namespace Stock_Sync
{
    public static class SecurityHelper
    {
        private const int SaltSize = 16;       // 128 bit
        private const int KeySize = 32;        // 256 bit
        private const int Iterations = 10000;  // PBKDF2 iterations

        /// <summary>
        /// Hashes a plaintext password using PBKDF2 with SHA-1/SHA-256 and a cryptographically secure random salt.
        /// Stored format: PBKDF2${iterations}${salt_base64}${hash_base64}
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[SaltSize];
                rng.GetBytes(salt);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
                {
                    byte[] key = pbkdf2.GetBytes(KeySize);
                    return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
                }
            }
        }

        /// <summary>
        /// Verifies a password against a stored hash or legacy plaintext string.
        /// </summary>
        public static bool VerifyPassword(string password, string storedPassword, out bool needsUpgrade)
        {
            needsUpgrade = false;

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedPassword))
                return false;

            // Check if stored password is in PBKDF2 format
            if (storedPassword.StartsWith("PBKDF2$"))
            {
                string[] parts = storedPassword.Split('$');
                if (parts.Length != 4)
                    return false;

                if (!int.TryParse(parts[1], out int iterations))
                    return false;

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedKey = Convert.FromBase64String(parts[3]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
                {
                    byte[] actualKey = pbkdf2.GetBytes(expectedKey.Length);
                    return SlowEquals(expectedKey, actualKey);
                }
            }

            // Fallback for legacy plaintext password
            if (storedPassword.Equals(password, StringComparison.Ordinal))
            {
                needsUpgrade = true; // Flag for automatic migration to PBKDF2
                return true;
            }

            return false;
        }

        /// <summary>
        /// Constant-time comparison to prevent timing attacks.
        /// </summary>
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return false;

            int diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }
    }
}

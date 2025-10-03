using System;
using System.Security.Cryptography;
using System.Text;

namespace RestaurantManagementSystem.Services
{
    public static class PasswordHasher
    {
        private const int Iterations = 1000;
        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 32; // 256 bits
        
        /// <summary>
        /// Verifies a password against a hash.
        /// </summary>
        /// <param name="password">The password to verify</param>
        /// <param name="hashedPassword">The hash format is {iterations}:{salt}:{hash}</param>
        /// <returns>True if the password matches the hash</returns>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
                return false;
            
            // Extract iterations, salt and hash from stored string
            var parts = hashedPassword.Split(':');
            if (parts.Length != 3)
                return false;
            
            if (!int.TryParse(parts[0], out int iterations))
                return false;
            
            var salt = parts[1];
            var storedHash = parts[2];
            
            // Compute hash with the same salt
            var computedHash = HashPassword(password, salt);
            
            // Compare the computed hash with the stored hash
            return computedHash == hashedPassword;
        }
        
        /// <summary>
        /// Hashes a password using PBKDF2 with the provided salt.
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <param name="salt">The salt to use</param>
        /// <returns>A string in the format {iterations}:{salt}:{hash}</returns>
        public static string HashPassword(string password, string salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, 
                Encoding.UTF8.GetBytes(salt), 
                Iterations, 
                HashAlgorithmName.SHA256))
            {
                var hashBytes = pbkdf2.GetBytes(HashSize);
                return $"{Iterations}:{salt}:{Convert.ToHexString(hashBytes).ToLower()}";
            }
        }
        
        /// <summary>
        /// Generates a random salt and hashes the password.
        /// </summary>
        /// <param name="password">The password to hash</param>
        /// <returns>A tuple containing the salt and the hashed password</returns>
        public static (string salt, string hashedPassword) HashPassword(string password)
        {
            var salt = GenerateSalt();
            var hashedPassword = HashPassword(password, salt);
            return (salt, hashedPassword);
        }
        
        /// <summary>
        /// Generates a random salt.
        /// </summary>
        /// <returns>A random salt as a hex string</returns>
        private static string GenerateSalt()
        {
            var saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToHexString(saltBytes).ToLower();
        }
        
        /// <summary>
        /// Generates a hash for the password "password" with salt "abc123"
        /// for the admin user. This is used for initial setup.
        /// </summary>
        /// <returns>The hardcoded hash for the admin user</returns>
        public static string GetAdminPasswordHash()
        {
            return HashPassword("password", "abc123");
        }
    }
}

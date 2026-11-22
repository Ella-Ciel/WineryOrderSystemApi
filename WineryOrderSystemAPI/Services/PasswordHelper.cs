using System.Security.Cryptography;
using System.Text;

namespace WineryOrderSystemAPI.Services
{
    public static class PasswordHelper
    {
        public static (string Hash, string Salt) HashPassword(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(32);
            var salt = Convert.ToBase64String(saltBytes);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));
            var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));

            return (hash, salt);
        }

        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(storedSalt));
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            return computedHash == storedHash;
        }
    }
}
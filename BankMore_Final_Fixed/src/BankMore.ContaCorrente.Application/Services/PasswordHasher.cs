using System.Security.Cryptography;
using System.Text;

namespace BankMore.ContaCorrente.Application.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var saltedPassword = password + salt;
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        public bool VerifyPassword(string password, string hashedPassword, string salt)
        {
            var newHash = HashPassword(password, salt);
            return newHash == hashedPassword;
        }
    }
}

namespace BankMore.ContaCorrente.Application.Services
{
    public interface IPasswordHasher
    {
        string HashPassword(string password, string salt);
        bool VerifyPassword(string password, string hashedPassword, string salt);
    }
}

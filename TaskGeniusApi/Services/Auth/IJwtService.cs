namespace TaskGeniusApi.Services.Auth
{
    public interface IJwtService
    {
        string GenerateToken(int userId, string userEmail);
        bool ValidateToken(string token);
    }
}

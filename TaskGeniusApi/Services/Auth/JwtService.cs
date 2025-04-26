using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace TaskGeniusApi.Services.Auth
{
    public class JwtService : IJwtService // Corregir la declaración de la clase
    {
        private readonly string SecretKey;
        private readonly string Issuer;
        private readonly string Audience;
        private readonly int ExpirationInMinutes;

        public JwtService(IConfiguration configuration)
        {
            SecretKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured");
            Issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
            Audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
            ExpirationInMinutes = int.TryParse(configuration["Jwt:ExpirationInMinutes"], out var minutes) ? minutes : 30;
        }

        public string GenerateToken(int userId, string userEmail)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, userEmail)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(ExpirationInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public bool ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = Issuer,
                    ValidateAudience = true,
                    ValidAudience = Audience,
                    ValidateLifetime = true,
                }, out SecurityToken validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

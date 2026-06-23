using FCG.UsersAPI.Application.Interfaces;
using FCG.UsersAPI.Domain.Entities;
using FCG.UsersAPI.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FCG.UsersAPI.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;

        public TokenService(IConfiguration configuration, IUserRepository userRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
        }

        public string GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("userId", user.Id.ToString()),
                new Claim("Email", user.Email.Value),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            }),
                Expires = DateTime.UtcNow.AddMinutes(
                    int.Parse(_configuration["Jwt:ExpirationMinutes"])),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<User> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            // Lança SecurityTokenException se o token for inválido ou expirado
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)
                           ?? principal.FindFirst("userId");

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new SecurityTokenException("Token não contém um identificador de utilizador válido.");

            var user = await _userRepository.GetByIdAsync(userId);

            if (user is null)
                throw new SecurityTokenException("Utilizador associado ao token não foi encontrado.");

            return user;
        }
    }

}

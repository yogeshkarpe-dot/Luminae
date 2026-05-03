using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Luminae.API.Models;
using Microsoft.IdentityModel.Tokens;

namespace Luminae.API.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    Guid? ValidateToken(string token);
}

public class TokenService(IConfiguration config) : ITokenService
{
    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(int.Parse(config["Jwt:ExpiryInDays"]!)),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!);
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = config["Jwt:Audience"],
                ValidateLifetime = true
            }, out var validated);

            var jwt = (JwtSecurityToken)validated;
            return Guid.Parse(jwt.Subject);
        }
        catch { return null; }
    }
}
using AdPulse.Api.Common;
using AdPulse.Api.Dtos;
using AdPulse.Api.Models;
using AdPulse.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AdPulse.Api.Services;

public class AuthService(IRepository<User> users, IConfiguration config, IWebHostEnvironment env)
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var user = await users.Query().FirstOrDefaultAsync(u => u.UserName == req.UserName);
        if (user is null || _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            throw ApiException.BadRequest("用户名或密码错误");

        var expires = DateTime.UtcNow.AddHours(12);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey()));
        var token = new JwtSecurityToken(
            issuer: "AdPulse",
            audience: "AdPulseWeb",
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("display_name", user.DisplayName),
            },
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expires,
            new UserInfo(user.Id, user.UserName, user.DisplayName, user.Role));
    }

    public async Task<UserInfo> MeAsync(ClaimsPrincipal principal)
    {
        var user = await users.GetAsync(principal.UserId()) ?? throw ApiException.Unauthorized();
        return new UserInfo(user.Id, user.UserName, user.DisplayName, user.Role);
    }

    internal string JwtKey()
    {
        var key = config["Jwt:Key"];
        if (!string.IsNullOrWhiteSpace(key)) return key;
        if (env.IsDevelopment()) return "AdPulse-Dev-Only-Signing-Key-Change-In-Production-0123456789";
        throw new InvalidOperationException("生产环境必须配置 Jwt:Key");
    }
}
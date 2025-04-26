using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskGeniusApi.DTOs.Auth;
using TaskGeniusApi.Services.Auth;
using TaskGeniusApi.Services.Users;

namespace TaskGeniusApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IJwtService jwtService, IUsersService usersService) : ControllerBase
{
    private readonly IJwtService _jwtService = jwtService;
    private readonly IUsersService _usersService = usersService;

    [HttpPost("register")]
    public async Task<RegisterResponseDto?> Register([FromBody] RegisterRequestDto registerDto)
    {
        var user = await _usersService.CreateUserAsync(new DTOs.Users.CreateUserDto
        {
            Name = registerDto.Name,
            Email = registerDto.Email,
            Password = registerDto.Password
        });

        var token = _jwtService.GenerateToken(user.Id, user.Email);

        return new RegisterResponseDto
        {
            Token = token,
            Expiration = DateTime.UtcNow.AddMinutes(30),
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    [HttpPost("login")]
    public async Task<LoginResponseDto?> Login([FromBody] LoginRequestDto loginDto)
    {
        var user = await _usersService.GetUserByEmailAsync(loginDto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var token = _jwtService.GenerateToken(user.Id, user.Email);

        return new LoginResponseDto
        {
            Token = token,
            Expiration =  DateTime.UtcNow.AddMinutes(30),
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
}
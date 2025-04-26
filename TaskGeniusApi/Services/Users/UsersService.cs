namespace TaskGeniusApi.Services.Users;
using Microsoft.EntityFrameworkCore;
using TaskGeniusApi.DTOs.Users;
using TaskGeniusApi.Data;
using BCrypt.Net;
using TaskGeniusApi.Models;

public class UsersService(ApplicationDbContext context) : IUsersService
{
    private readonly ApplicationDbContext _context = context;

    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null!;
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserDto> GetUserByEmailAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return null!;
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto userDto)
    {
        var user = new UserModel
        {
            Name = userDto.Name,
            Email = userDto.Email,
            PasswordHash = BCrypt.HashPassword(userDto.Password),
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
    public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto userDto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null!;
        user.Name = userDto.Name;
        user.Email = userDto.Email;
        if (!string.IsNullOrEmpty(userDto.Password))
        {
            user.PasswordHash = BCrypt.HashPassword(userDto.Password);
        }
        await _context.SaveChangesAsync();
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }
    public async Task DeleteUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return;
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
    }
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _context.Users.ToListAsync();
        return users.Select(user => new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        });
    }
}
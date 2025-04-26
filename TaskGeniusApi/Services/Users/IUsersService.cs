using System.Collections.Generic;
using System.Threading.Tasks;
using TaskGeniusApi.DTOs.Users;
using TaskGeniusApi.Models;

namespace TaskGeniusApi.Services.Users
{
    public interface IUsersService
    {
        Task<UserDto> GetUserByIdAsync(int id);
        Task<UserDto> GetUserByEmailAsync(string email);
        Task<UserDto> CreateUserAsync(CreateUserDto userDto);
        Task<UserDto> UpdateUserAsync(int id, UpdateUserDto userDto);
        Task DeleteUserAsync(int id);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
    }
}
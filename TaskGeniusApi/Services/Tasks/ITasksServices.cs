namespace TaskGeniusApi.Services.Tasks;
using TaskGeniusApi.DTOs.Tasks;


public interface ITasksServices
{
    Task <TaskDto> CreateTaskAsync(CreateTaskDto taskDto);
    Task<TaskDto> GetTaskByIdAsync(int id);
    Task<TaskDto> UpdateTaskAsync(UpdateTaskDto taskDto);
    Task DeleteTaskAsync(int id);
    Task<IEnumerable<TaskDto>> GetTasksByUserIdAsync(string userId);
}
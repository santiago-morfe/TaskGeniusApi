namespace TaskGeniusApi.Services.Tasks;
using TaskGeniusApi.Models;
using TaskGeniusApi.DTOs.Tasks;
using TaskGeniusApi.Data;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

public class TasksServices(ApplicationDbContext dbContext) : ITasksServices
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<TaskDto> CreateTaskAsync(CreateTaskDto taskDto)
    {
        var task = new TaskModel
        {
            Title = taskDto.Title,
            Description = taskDto.Description,
            UserId = int.Parse(taskDto.UserId),
            IsCompleted = taskDto.IsCompleted,
        };

        if (taskDto.DueDate != null && taskDto.DueDate != default(DateTime))
        {
            task.DueDate = taskDto.DueDate;
        }

        // que no se puedean tener mas de 20 tareas
        var userTasksCount = await _dbContext.Tasks.CountAsync(t => t.UserId == task.UserId);
        if (userTasksCount >= 20)
        {
            throw new InvalidOperationException("User cannot have more than 20 tasks.");
        }

        await _dbContext.Tasks.AddAsync(task);
        await _dbContext.SaveChangesAsync();

        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted,
            UserId = task.UserId.ToString(),
        };
    }

    public async Task<TaskDto> GetTaskByIdAsync(int id)
    {
        var task = await _dbContext.Tasks.FindAsync(id);
        if (task == null) return null!;

        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted,
            UserId = task.UserId.ToString(),
        };
    }

    public async Task<TaskDto> UpdateTaskAsync(UpdateTaskDto taskDto)
    {
        var task = await _dbContext.Tasks.FindAsync(taskDto.Id);
        if (task == null) return null!;

        task.Title = taskDto.Title ?? task.Title;
        task.Description = taskDto.Description ?? task.Description;
        task.DueDate = taskDto.DueDate ?? task.DueDate;
        task.IsCompleted = taskDto.IsCompleted?? task.IsCompleted;

        _dbContext.Tasks.Update(task);
        await _dbContext.SaveChangesAsync();

        return new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted,
            UserId = task.UserId.ToString(),
        };
    }

    public async Task DeleteTaskAsync(int id)
    {
        var task = await _dbContext.Tasks.FindAsync(id);
        if (task == null) return;

        _dbContext.Tasks.Remove(task);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<TaskDto>> GetTasksByUserIdAsync(string userId)
    {
        var tasks = await _dbContext.Tasks.Where(t => t.UserId.ToString() == userId).ToListAsync();
        return tasks.Select(task => new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted,
            UserId = task.UserId.ToString(),
        });
    }
}
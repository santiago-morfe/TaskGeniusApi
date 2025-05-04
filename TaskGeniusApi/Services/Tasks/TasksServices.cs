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

    public async Task<TaskDto> UpdateTaskAsync(int id, UpdateTaskDto taskDto)
    {
        var task = await _dbContext.Tasks.FindAsync(id);
        if (task == null) return null!;

        task.Title = taskDto.Title ?? task.Title;
        task.Description = taskDto.Description ?? task.Description;
        task.DueDate = taskDto.DueDate;
        task.IsCompleted = taskDto.IsCompleted;

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
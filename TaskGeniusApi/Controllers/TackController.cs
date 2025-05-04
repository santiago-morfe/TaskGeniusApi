using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskGeniusApi.DTOs.Tasks;
using TaskGeniusApi.Services.Tasks;

namespace TaskGeniusApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskController(ITasksServices tasksService) : ControllerBase
{
    private readonly ITasksServices _tasksService = tasksService;

    private string GetUserIdFromToken()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID not found in token");
    }

    [HttpGet]
    public async Task<List<TaskDto>> GetAllTasks()
    {
        var userId = GetUserIdFromToken();
        var tasks = await _tasksService.GetTasksByUserIdAsync(userId);
        return [.. tasks.Select(task => new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted,
            UserId = task.UserId,
            CreatedAt = task.CreatedAt
        })];
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaskDto>> GetTaskById(int id)
    {
        var userId = GetUserIdFromToken();
        var task = await _tasksService.GetTaskByIdAsync(id);
        if (task == null || task.UserId != userId)
        {
            return NotFound(new { Message = "Task not found or access denied" });
        }
        return task;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequestDto createTaskRequestDto)
    {
        // validar si la fecha de vencimiento es válida

        var createTaskDto = new CreateTaskDto
        {
            Title = createTaskRequestDto.Title,
            Description = createTaskRequestDto.Description,
            IsCompleted = createTaskRequestDto.IsCompleted
        };
        if (createTaskRequestDto.DueDate != null)
        {
            createTaskDto.DueDate = createTaskRequestDto.DueDate;
        }
        else
        {
            createTaskDto.DueDate = null;
        }

        var userId = GetUserIdFromToken();
        createTaskDto.UserId = userId;
        var task = await _tasksService.CreateTaskAsync(createTaskDto);
        return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTask([FromBody] UpdateTaskDto updateTaskDto)
    {
        var userId = GetUserIdFromToken();
        var task = await _tasksService.GetTaskByIdAsync(updateTaskDto.Id);
        if (task == null || task.UserId != userId)
        {
            return NotFound("Task not found or access denied");
        }
        var updatedTask = await _tasksService.UpdateTaskAsync(updateTaskDto.Id, updateTaskDto);
        return Ok(updatedTask);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var userId = GetUserIdFromToken();
        var task = await _tasksService.GetTaskByIdAsync(id);
        if (task == null || task.UserId != userId)
        {
            return NotFound("Task not found or access denied");
        }
        await _tasksService.DeleteTaskAsync(id);
        return NoContent();
    }
}


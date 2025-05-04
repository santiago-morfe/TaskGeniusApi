using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskGeniusApi.DTOs.Genius;
using TaskGeniusApi.Services.Genius;
using TaskGeniusApi.Services.Tasks;

namespace TaskGeniusApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeniusController(IGeniusService geniusService, ITasksServices tasksService) : ControllerBase
{
    private readonly IGeniusService _geniusService = geniusService;
    private readonly ITasksServices _tasksService = tasksService;

    [HttpGet("advice")]
    public async Task<TaskAdviceResponseDto> GetTaskAdvice()
    {
        var userId = GetUserIdFromToken();
        var tasks = await _tasksService.GetTasksByUserIdAsync(userId);

        if (tasks == null || !tasks.Any())
        {
            return new TaskAdviceResponseDto
            {
                Advice = "No tasks found for the user."
            };
        }

        var requestDto = new TaskAdviceRequestDto
        {
            Tasks = [.. tasks
                .Where(task => !task.IsCompleted)
                .Select(task => new TaskDetailDto
                {
                    Title = task.Title,
                    Description = task.Description,
                    DueDate = task.DueDate
                })]
        };

        var advice = await _geniusService.GetAdviceAsync(requestDto);
        return advice;
    }

    [HttpPost("titleSuggestion")]
    public async Task<TitleSuggestionResponseDto> GetTitleSuggestion([FromBody] TitleSuggestionRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Description))
        {
            throw new ArgumentException("Task description cannot be null or empty.", nameof(requestDto.Description));
        }
        return await _geniusService.GetTitleSuggestionAsync(requestDto.Description);
    }

    [HttpPost("descriptionFormatting")]
    public async Task<DescriptionFormattingResponseDto> GetDescriptionFormatting([FromBody] DescriptionFormattingRequestDto requestDto)
    {
        if (string.IsNullOrWhiteSpace(requestDto.Description))
        {
            throw new ArgumentException("Task description cannot be null or empty.", nameof(requestDto.Description));
        }
        return await _geniusService.GetDescriptionFormattingAsync(requestDto.Description);
    }

    private string GetUserIdFromToken()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID not found in token");
    }
}
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

    [HttpGet("titleSuggestion")]
    public async Task<TitleSuggestionResponseDto> GetTitleSuggestion([FromQuery] TitleSuggestionRequestDto requestDto)
    {
        return await _geniusService.GetTitleSuggestionAsync(requestDto.TaskDescription);
    }

    [HttpGet("descriptionFormatting")]
    public async Task<DescriptionFormattingResponseDto> GetDescriptionFormatting([FromQuery] DescriptionFormattingRequestDto requestDto)
    {
        return await _geniusService.GetDescriptionFormattingAsync(requestDto.TaskDescription);
    }

    private string GetUserIdFromToken()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("User ID not found in token");
    }
}
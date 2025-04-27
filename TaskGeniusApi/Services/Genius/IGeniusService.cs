namespace TaskGeniusApi.Services.Genius;
using System.Threading.Tasks;
using TaskGeniusApi.DTOs.Genius;

public interface IGeniusService
{
    Task<TaskAdviceResponseDto> GetAdviceAsync(TaskAdviceRequestDto requestDto);
    Task<TitleSuggestionResponseDto> GetTitleSuggestionAsync(string taskDescription);
    Task<DescriptionFormattingResponseDto> GetDescriptionFormattingAsync(string taskDescription);
}
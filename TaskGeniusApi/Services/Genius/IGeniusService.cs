namespace TaskGeniusApi.Services.Genius;
using System.Threading.Tasks;
using TaskGeniusApi.DTOs.Genius;

public interface IGeniusService
{
    Task<TaskAdviceResponseDto> GetAdviceAsync(TaskAdviceRequestDto requestDto);
}
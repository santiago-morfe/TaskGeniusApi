namespace TaskGeniusApi.DTOs.Genius;


public class TitleSuggestionResponseDto
{
    public string Title { get; set; } = string.Empty;
}

public class TitleSuggestionRequestDto
{
    public string TaskDescription { get; set; } = string.Empty;
}

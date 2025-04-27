namespace TaskGeniusApi.DTOs.Genius;

public class DescriptionFormattingResponseDto
{
    public required string FormattedDescription { get; set; }
}

public class DescriptionFormattingRequestDto
{
    public required string TaskDescription { get; set; }

}
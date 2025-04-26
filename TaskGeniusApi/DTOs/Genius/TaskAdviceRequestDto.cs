namespace TaskGeniusApi.DTOs.Genius;
using System;


public class TaskDetailDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public DateTime DueDate { get; set; }
    public int UserId { get; set; }
}

public class TaskAdviceRequestDto
{
    public required List<TaskDetailDto> Tasks { get; set; }
}

public class TaskAdviceResponseDto
{
    public required string Advice { get; set; }
}
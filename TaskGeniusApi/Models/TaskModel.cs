using System.ComponentModel.DataAnnotations;

namespace TaskGeniusApi.Models
{
    public class TaskModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public required string Title { get; set; }
        [Required]
        public required string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; } = null;
        public bool IsCompleted { get; set; } = false;
        [Required]
        public required int UserId { get; set; }
        public UserModel User { get; set; } = null!;
    }
}
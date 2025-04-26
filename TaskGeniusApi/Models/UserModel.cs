using System.ComponentModel.DataAnnotations;

namespace TaskGeniusApi.Models
{
    public class UserModel
    {
        [Key] 
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public required string Name { get; set; }
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public required string Email { get; set; }
        [Required]
        public required string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaskModel> Tasks { get; set; } = [];
    }
}

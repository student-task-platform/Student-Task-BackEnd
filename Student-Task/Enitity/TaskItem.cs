using System.ComponentModel.DataAnnotations;

namespace Student_Task.Enitity
{
    public class TaskItem
    {
        // Primary key
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? DeadlineUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        // Foreign key -> Users table
        [Required]
        public int UserId { get; set; }

        // Navigation property
        public User? User { get; set; }
    }
}

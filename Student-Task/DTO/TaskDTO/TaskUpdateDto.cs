using System.ComponentModel.DataAnnotations;

namespace Student_Task.DTO.TaskDTO
{
    public class TaskUpdateDto
    {
        [Required]
        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public bool IsCompleted { get; set; }

        // NEW
        public DateTime? DeadlineUtc { get; set; }
    }
}

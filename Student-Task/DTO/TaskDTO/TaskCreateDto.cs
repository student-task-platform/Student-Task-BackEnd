using System.ComponentModel.DataAnnotations;

namespace Student_Task.DTO
{
    public class TaskCreateDto
    {
        [Required]
        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DateTime? DeadlineUtc { get; set; }
    }
}

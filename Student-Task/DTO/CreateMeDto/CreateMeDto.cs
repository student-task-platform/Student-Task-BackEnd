using System.ComponentModel.DataAnnotations;

namespace Student_Task.DTO
{
    public class CreateMeDto
    {
        [Required]
        [MaxLength(80)]
        public string FullName { get; set; } = string.Empty;
    }
}

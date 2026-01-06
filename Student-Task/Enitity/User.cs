using System.ComponentModel.DataAnnotations;

namespace Student_Task.Enitity
{
    public class User
    {
        public int Id { get; set; }

        // Firebase unique id (string)
        [Required]
        [MaxLength(128)]
        public string FirebaseUid { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string FullName { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Optional navigation
        public List<TaskItem> Tasks { get; set; } = new();
    }
}

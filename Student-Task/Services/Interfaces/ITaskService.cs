using Student_Task.DTO;
using Student_Task.DTO.TaskDTO;

namespace Student_Task.Services.Interfaces
{
    public interface ITaskService
    {
        Task<List<TaskResponseDto>> GetAllAsync(int userId);
        Task<TaskResponseDto?> GetByIdAsync(int id, int userId);
        Task<TaskResponseDto> CreateAsync(TaskCreateDto dto, int userId);
        Task<bool> UpdateAsync(int id, TaskUpdateDto dto, int userId);
        Task<bool> DeleteAsync(int id, int userId);
    }
}

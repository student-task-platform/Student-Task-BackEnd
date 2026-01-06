using Student_Task.DTO;
using Student_Task.DTO.TaskDTO;
using Student_Task.Enitity;
using Student_Task.Repositories.Interfaces;
using Student_Task.Services.Interfaces;

namespace Student_Task.Services.Service
{
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _repo;

        public TaskService(ITaskRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<TaskResponseDto>> GetAllAsync(int userId)
        {
            var tasks = await _repo.GetAllAsync(userId);
            return tasks.Select(MapToResponse).ToList();
        }

        public async Task<TaskResponseDto?> GetByIdAsync(int id, int userId)
        {
            var task = await _repo.GetByIdAsync(id, userId);
            return task == null ? null : MapToResponse(task);
        }

        public async Task<TaskResponseDto> CreateAsync(TaskCreateDto dto, int userId)
        {
            var task = new TaskItem
            {
                Title = dto.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                DeadlineUtc = dto.DeadlineUtc,
                IsCompleted = false,
                CreatedAtUtc = DateTime.UtcNow,
                UserId = userId
            };

            var created = await _repo.CreateAsync(task);
            return MapToResponse(created);
        }

        public async Task<bool> UpdateAsync(int id, TaskUpdateDto dto, int userId)
        {
            var existing = await _repo.GetByIdAsync(id, userId);
            if (existing == null) return false;

            existing.Title = dto.Title.Trim();
            existing.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            existing.IsCompleted = dto.IsCompleted;
            existing.DeadlineUtc = dto.DeadlineUtc;
            existing.UpdatedAtUtc = DateTime.UtcNow;

            return await _repo.UpdateAsync(existing);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            return await _repo.DeleteAsync(id, userId);
        }

        private static TaskResponseDto MapToResponse(TaskItem task)
        {
            return new TaskResponseDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                IsCompleted = task.IsCompleted,
                DeadlineUtc = task.DeadlineUtc,
                CreatedAtUtc = task.CreatedAtUtc,
                UpdatedAtUtc = task.UpdatedAtUtc,
                UserId = task.UserId
            };
        }
    }
}

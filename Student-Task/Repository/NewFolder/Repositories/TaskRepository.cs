using Microsoft.EntityFrameworkCore;
using Student_Task.Data;
using Student_Task.Enitity;
using Student_Task.Repositories.Interfaces;

namespace Student_Task.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly AppDbContext _db;

        public TaskRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<TaskItem>> GetAllAsync(int userId)
        {
            return await _db.Tasks
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetByIdAsync(int id, int userId)
        {
            return await _db.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }

        public async Task<TaskItem> CreateAsync(TaskItem task)
        {
            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();
            return task;
        }

        public async Task<bool> UpdateAsync(TaskItem task)
        {
            _db.Tasks.Update(task);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var existing = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
            if (existing == null) return false;

            _db.Tasks.Remove(existing);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Student_Task.DTO;
using Student_Task.DTO.TaskDTO;
using Student_Task.Security;
using Student_Task.Services.Interfaces;

namespace Student_Task.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _service;
        private readonly FirebaseUserResolver _userResolver;

        public TasksController(
            ITaskService service,
            FirebaseUserResolver userResolver)
        {
            _service = service;
            _userResolver = userResolver;
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            var user = await _userResolver.ResolveAsync(Request.Headers.Authorization);
            return user?.Id;
        }

        // GET: /api/tasks
        [HttpGet]
        public async Task<ActionResult<List<TaskResponseDto>>> GetAll()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Unauthorized();

            var tasks = await _service.GetAllAsync(userId.Value);
            return Ok(tasks);
        }

        // GET: /api/tasks/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TaskResponseDto>> GetById(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Unauthorized();

            var task = await _service.GetByIdAsync(id, userId.Value);
            if (task == null) return NotFound(new { message = "Task not found." });

            return Ok(task);
        }

        // POST: /api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskResponseDto>> Create([FromBody] TaskCreateDto dto)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Unauthorized();

            var created = await _service.CreateAsync(dto, userId.Value);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: /api/tasks/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TaskUpdateDto dto)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Unauthorized();

            var ok = await _service.UpdateAsync(id, dto, userId.Value);
            if (!ok) return NotFound(new { message = "Task not found." });

            return NoContent();
        }

        // DELETE: /api/tasks/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return Unauthorized();

            var ok = await _service.DeleteAsync(id, userId.Value);
            if (!ok) return NotFound(new { message = "Task not found." });

            return NoContent();
        }
    }
}

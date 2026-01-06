using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Student_Task.Data;
using Student_Task.DTO;
using Student_Task.Enitity;
using Student_Task.Security;

namespace Student_Task.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FirebaseTokenVerifier _tokenVerifier;

        public UsersController(AppDbContext db, FirebaseTokenVerifier tokenVerifier)
        {
            _db = db;
            _tokenVerifier = tokenVerifier;
        }

        // POST: /api/users/me  (create local user if not exists)
        [HttpPost("me")]
        public async Task<ActionResult<object>> CreateMe([FromBody] CreateMeDto dto)
        {
            var uid = await _tokenVerifier.GetUidFromBearerTokenAsync(Request.Headers.Authorization);
            if (uid == null) return Unauthorized(new { message = "Invalid or missing token." });

            var existing = await _db.Users.FirstOrDefaultAsync(u => u.FirebaseUid == uid);
            if (existing != null)
            {
                return Ok(new { userId = existing.Id, fullName = existing.FullName, firebaseUid = existing.FirebaseUid });
            }

            var user = new User
            {
                FirebaseUid = uid,
                FullName = dto.FullName.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok(new { userId = user.Id, fullName = user.FullName, firebaseUid = user.FirebaseUid });
        }

        // GET: /api/users/me
        [HttpGet("me")]
        public async Task<ActionResult<object>> GetMe()
        {
            var uid = await _tokenVerifier.GetUidFromBearerTokenAsync(Request.Headers.Authorization);
            if (uid == null) return Unauthorized(new { message = "Invalid or missing token." });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.FirebaseUid == uid);
            if (user == null) return NotFound(new { message = "User not created in backend yet. Call POST /api/users/me once." });

            return Ok(new { userId = user.Id, fullName = user.FullName, firebaseUid = user.FirebaseUid });
        }
    }
}

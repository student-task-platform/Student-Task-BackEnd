    using FirebaseAdmin.Auth;
    using Microsoft.EntityFrameworkCore;
    using Student_Task.Data;
    using Student_Task.Enitity;

    namespace Student_Task.Security
    {
        public class FirebaseUserResolver
        {
            private readonly AppDbContext _db;

            public FirebaseUserResolver(AppDbContext db)
            {
                _db = db;
            }

            public async Task<User?> ResolveAsync(string? authorizationHeader)
            {
                if (string.IsNullOrWhiteSpace(authorizationHeader))
                    return null;

                if (!authorizationHeader.StartsWith("Bearer "))
                    return null;

                var token = authorizationHeader.Substring("Bearer ".Length).Trim();

                if (string.IsNullOrWhiteSpace(token))
                    return null;

                try
                {
                    var decoded = await FirebaseAuth
                        .DefaultInstance
                        .VerifyIdTokenAsync(token);

                    return await _db.Users
                        .FirstOrDefaultAsync(u => u.FirebaseUid == decoded.Uid);
                }
                catch
                {
                    return null;
                }
            }
        }
    }

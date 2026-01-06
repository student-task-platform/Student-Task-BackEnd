using FirebaseAdmin.Auth;

namespace Student_Task.Security
{
    public class FirebaseTokenVerifier
    {
        public async Task<string?> GetUidFromBearerTokenAsync(string? authorizationHeader)
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

                return decoded.Uid;
            }
            catch
            {
                return null;
            }
        }
    }
}

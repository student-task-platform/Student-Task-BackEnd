using System;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

using Student_Task.Data;

// Repositories
using Student_Task.Repositories;
using Student_Task.Repositories.Interfaces;

// Services
using Student_Task.Services.Interfaces;
using Student_Task.Services.Service;

// Firebase helpers
using Student_Task.Security;

namespace Student_Task
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =====================================================
            // Controllers & Swagger
            // =====================================================
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // =====================================================
            // Database (PostgreSQL)
            // =====================================================
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Missing ConnectionStrings:DefaultConnection. " +
                    "Set env var ConnectionStrings__DefaultConnection."
                );
            }

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString)
            );

            // =====================================================
            // CORS (Frontend → Backend)
            // =====================================================
            var allowAnyOrigin =
                string.Equals(builder.Configuration["CORS:AllowAnyOrigin"], "true", StringComparison.OrdinalIgnoreCase);

            var allowedOriginsCsv = builder.Configuration["CORS:AllowedOrigins"] ?? string.Empty;
            var allowedOrigins = allowedOriginsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();

            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    if (allowAnyOrigin)
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                        return;
                    }

                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                        return;
                    }

                    if (builder.Environment.IsDevelopment())
                    {
                        policy.WithOrigins("http://localhost:3000")
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                });
            });

            // =====================================================
            // AUTHENTICATION (Firebase JWT)
            // =====================================================
            var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];
            if (string.IsNullOrWhiteSpace(firebaseProjectId))
            {
                throw new InvalidOperationException(
                    "Missing Firebase:ProjectId. Set env var Firebase__ProjectId."
                );
            }

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",

                        ValidateAudience = true,
                        ValidAudience = firebaseProjectId,

                        ValidateLifetime = true
                    };
                });

            builder.Services.AddAuthorization();

            // =====================================================
            // Firebase Admin INIT (PRODUCTION SAFE)
            // =====================================================
            EnsureFirebaseAdminInitialized(builder);

            // =====================================================
            // Repositories
            // =====================================================
            builder.Services.AddScoped<ITaskRepository, TaskRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // =====================================================
            // Services
            // =====================================================
            builder.Services.AddScoped<ITaskService, TaskService>();

            // =====================================================
            // Firebase helpers
            // =====================================================
            builder.Services.AddScoped<FirebaseTokenVerifier>();
            builder.Services.AddScoped<FirebaseUserResolver>();

            var app = builder.Build();

            // =====================================================
            // Forwarded Headers (Render / Proxy safe)
            // =====================================================
            var forwardedHeadersOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto
            };

            forwardedHeadersOptions.KnownNetworks.Clear();
            forwardedHeadersOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardedHeadersOptions);

            // =====================================================
            // Database migrations (controlled)
            // =====================================================
            var runMigrations =
                app.Environment.IsDevelopment() ||
                string.Equals(builder.Configuration["RUN_MIGRATIONS"], "true", StringComparison.OrdinalIgnoreCase);

            if (runMigrations)
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            // =====================================================
            // Swagger (optional in prod)
            // =====================================================
            var enableSwagger =
                app.Environment.IsDevelopment() ||
                string.Equals(builder.Configuration["ENABLE_SWAGGER"], "true", StringComparison.OrdinalIgnoreCase);

            if (enableSwagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors();

            // app.UseHttpsRedirection(); // optional (Render already terminates TLS)

            app.UseAuthentication();
            app.UseAuthorization();

            // Health check (Render friendly)
            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            app.MapControllers();
            app.Run();
        }

        // =====================================================
        // Firebase Admin Helpers (NO obsolete APIs)
        // =====================================================
        private static void EnsureFirebaseAdminInitialized(WebApplicationBuilder builder)
        {
            try
            {
                if (FirebaseApp.DefaultInstance != null)
                    return;
            }
            catch
            {
                // Not initialized yet
            }

            var projectId = builder.Configuration["Firebase:ProjectId"] ?? string.Empty;

            var serviceAccountJson =
                builder.Configuration["FIREBASE_SERVICE_ACCOUNT_JSON"];

            var credentialsPath =
                Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

            GoogleCredential googleCredential;

            if (!string.IsNullOrWhiteSpace(serviceAccountJson))
            {
                googleCredential =
                    LoadGoogleCredentialFromJson(serviceAccountJson);
            }
            else if (!string.IsNullOrWhiteSpace(credentialsPath) && File.Exists(credentialsPath))
            {
                googleCredential =
                    LoadGoogleCredentialFromFile(credentialsPath);
            }
            else
            {
                throw new InvalidOperationException(
                    "Firebase Admin credentials not found. " +
                    "Set FIREBASE_SERVICE_ACCOUNT_JSON (recommended) " +
                    "or provide GOOGLE_APPLICATION_CREDENTIALS path."
                );
            }

            FirebaseApp.Create(new AppOptions
            {
                Credential = googleCredential,
                ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId
            });
        }

        private static GoogleCredential LoadGoogleCredentialFromJson(string json)
        {
            var normalizedJson = json.Replace("\\n", "\n");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(normalizedJson));

            var serviceAccountCredential =
                CredentialFactory.FromStream<ServiceAccountCredential>(stream);

            return serviceAccountCredential.ToGoogleCredential();
        }

        private static GoogleCredential LoadGoogleCredentialFromFile(string path)
        {
            using var stream = File.OpenRead(path);

            var serviceAccountCredential =
                CredentialFactory.FromStream<ServiceAccountCredential>(stream);

            return serviceAccountCredential.ToGoogleCredential();
        }
    }
}

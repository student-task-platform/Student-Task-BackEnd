    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Microsoft.AspNetCore.Authentication.JwtBearer;
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

                // =========================
                // Controllers & Swagger
                // =========================
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // =========================
                // Database
                // =========================
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(
                        builder.Configuration.GetConnectionString("DefaultConnection")
                    )
                );

                // =========================
                // CORS (Frontend → Backend)
                // =========================
                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy
                            .WithOrigins("http://localhost:3000")
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                    });
                });

                // =========================
                // AUTHENTICATION (REQUIRED FOR [Authorize])
                // =========================
                var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];

                if (string.IsNullOrWhiteSpace(firebaseProjectId))
                {
                        throw new InvalidOperationException(
                        "Missing Firebase:ProjectId in appsettings.json"
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
                        options.Authority =
                            $"https://securetoken.google.com/{firebaseProjectId}";

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer =
                                $"https://securetoken.google.com/{firebaseProjectId}",

                            ValidateAudience = true,
                            ValidAudience = firebaseProjectId,

                            ValidateLifetime = true
                        };
                    });

                builder.Services.AddAuthorization();

                // =========================
                // Firebase Admin INIT
                // =========================
                var pathFromEnv = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                var pathFromConfig = builder.Configuration["Firebase:ServiceAccountPath"];

                var projectRootPath =
                    Path.Combine(builder.Environment.ContentRootPath, "firebase-service-account.json");

                var outputBinPath =
                    Path.Combine(AppContext.BaseDirectory, "firebase-service-account.json");

                var candidatePaths = new List<string>();

                if (!string.IsNullOrWhiteSpace(pathFromEnv))
                    candidatePaths.Add(Path.GetFullPath(pathFromEnv));

                if (!string.IsNullOrWhiteSpace(pathFromConfig))
                    candidatePaths.Add(Path.GetFullPath(pathFromConfig));

                candidatePaths.Add(projectRootPath);
                candidatePaths.Add(outputBinPath);

                var firebaseKeyPath = candidatePaths.FirstOrDefault(File.Exists);

                if (firebaseKeyPath == null)
                {
                    throw new FileNotFoundException(
                        "Firebase service account JSON file not found.\nSearched:\n - " +
                        string.Join("\n - ", candidatePaths)
                    );
                }

                Console.WriteLine($"[Firebase] Using service account file: {firebaseKeyPath}");

                GoogleCredential googleCredential;
                using (var stream = File.OpenRead(firebaseKeyPath))
                {
                    var serviceAccountCredential =
                        CredentialFactory.FromStream<ServiceAccountCredential>(stream);

                    googleCredential = serviceAccountCredential.ToGoogleCredential();
                }

                FirebaseApp? existingApp = null;
                try
                {
                    existingApp = FirebaseApp.DefaultInstance;
                }
                catch
                {
                    existingApp = null;
                }

                if (existingApp == null)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = googleCredential
                    });
                }

                // =========================
                // Repositories
                // =========================
                builder.Services.AddScoped<ITaskRepository, TaskRepository>();
                builder.Services.AddScoped<IUserRepository, UserRepository>();

                // =========================
                // Services
                // =========================
                builder.Services.AddScoped<ITaskService, TaskService>();

                // =========================
                // Firebase helpers
                // =========================
                builder.Services.AddScoped<FirebaseTokenVerifier>();
                builder.Services.AddScoped<FirebaseUserResolver>();

                var app = builder.Build();

                using (var scope = app.Services.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                }

                if (app.Environment.IsDevelopment())
                    {
                        app.UseSwagger();
                        app.UseSwaggerUI();
                    }

                app.UseCors();

                // 🔥 ORDER IS CRITICAL
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();
                app.Run();
            }
        }
    }

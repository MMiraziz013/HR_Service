using System.Text;
using System.Text.Json.Serialization;
using Clean.Application;
using Clean.Application.Abstractions;
using Clean.Application.Jobs;
using Clean.Application.Security.Permission;
using Clean.Infrastructure;
using Clean.Infrastructure.Data;
using Clean.Infrastructure.Data.Seed;
using HR_Service.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using Serilog;
using Serilog.Formatting.Json;

namespace HR_Service;

public static class Program
{
    [Obsolete("Obsolete")]
    public static async Task Main(string[] args)
    {
        Directory.CreateDirectory("logs");

        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                new JsonFormatter(),
                path: "logs/log-.json",
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true)
            .CreateLogger();

        Log.Information("✅ Serilog initialized successfully.");

        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseSerilog();

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Enter your access token",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });
        });

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var key = builder.Configuration["JWT:Key"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JWT:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JWT:Audience"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
                };

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        builder.Services.AddScoped<LoggingMiddleware>();
        builder.Services.AddIdentityServices(builder.Configuration);
        builder.Services.AddApplicationServices(builder.Configuration);
        builder.Services.AddInfrastructureServices(builder.Configuration);
        
        
        //TODO: Check if quartz was executed later.
        builder.Services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            // ✅ Use persistent store
            q.UsePersistentStore(options =>
            {
                options.UseProperties = true;
                options.RetryInterval = TimeSpan.FromSeconds(15);

                options.UsePostgres(pgOptions =>
                {
                    var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                    var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

                    if (!string.IsNullOrEmpty(dbPassword))
                    {
                        baseConnectionString = baseConnectionString!.Replace("Password=", $"Password={dbPassword}");
                    }

                    pgOptions.ConnectionString = baseConnectionString!;
                });

                // Optional: use JSON serialization for job data
                options.UseJsonSerializer();
            });

            // ✅ Register job
            var jobKey = new JobKey("VacationBalanceJob");
            q.AddJob<VacationBalanceJob>(opts => opts.WithIdentity(jobKey));

            // ✅ Trigger: run daily at 2:00 UTC
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("VacationBalanceTrigger")
                .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(2, 0))
            );
        });

        // ✅ Add Quartz Hosted Service (runs scheduler in background)
        builder.Services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });



        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IDataContext>();
            await db.MigrateAsync();

            var services = scope.ServiceProvider;

            var seeder = services.GetRequiredService<SeedDataInitializer>();
            await seeder.InitializeAsync();
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            options.RoutePrefix = string.Empty;
        });
        
        
        //TODO: Check if we can implement quartz dashboard later
        // var scheduler = await app.Services.GetRequiredService<ISchedulerFactory>().GetScheduler();
        
        // app.UseSilkierQuartz( =>
        // {
        //     config.Scheduler = scheduler;
        // });



        app.UseHttpsRedirection();
        app.UseMiddleware<LoggingMiddleware>();
 
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        Log.Information("🚀 Application is starting...");
        await app.RunAsync();
    }
}

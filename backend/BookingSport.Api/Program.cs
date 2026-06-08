using System.Text;
using BookingSport.Api.Data;
using BookingSport.Api.Enums;
using BookingSport.Api.Hubs;
using BookingSport.Api.Logging;
using BookingSport.Api.Middleware;
using BookingSport.Api.Services.Auth;
using BookingSport.Api.Services.Availability;
using BookingSport.Api.Services.Bookings;
using BookingSport.Api.Services.Courts;
using BookingSport.Api.Services.Dashboard;
using BookingSport.Api.Services.Email;
using BookingSport.Api.Services.Jobs;
using BookingSport.Api.Services.PriceRules;
using BookingSport.Api.Services.Reports;
using BookingSport.Api.Services.Users;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
var jwtSecret = builder.Configuration["Jwt:Secret"];
var frontendOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? throw new InvalidOperationException("CORS allowed origins are not configured.");

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    var fileOptions = context.Configuration.GetSection("Logging:File").Get<LogFileOptions>() ?? new LogFileOptions();

    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "BookingSport.Api")
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .WriteTo.Console();

    if (fileOptions.Enabled)
    {
        var logFilePath = LogFilePathResolver.ResolveFilePath(context.HostingEnvironment.ContentRootPath, fileOptions);

        loggerConfiguration.WriteTo.File(
            logFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: fileOptions.RetainedFileCountLimit,
            restrictedToMinimumLevel: LogEventLevel.Information);
    }
});

builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins(frontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition")
            .AllowCredentials();
    });
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped(typeof(PasswordHasher<>));
builder.Services.AddScoped<IAuthSettingsService, AuthSettingsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAvailabilityService, AvailabilityService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IBookingRealtimeNotifier, SignalRBookingRealtimeNotifier>();
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IBookingEmailJob, BookingEmailJob>();
builder.Services.AddScoped<IAdminBookingReportService, AdminBookingReportService>();
builder.Services.AddScoped<IPriceRuleService, PriceRuleService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ILogSanitizer, LogSanitizer>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<ApiLoggingOptions>(builder.Configuration.GetSection("ApiLogging"));

var hangfireEnabled = builder.Configuration.GetValue<bool?>("Hangfire:Enabled")
    ?? !builder.Environment.IsEnvironment("IntegrationTests");

if (hangfireEnabled)
{
    builder.Services.AddHangfire(configuration =>
        configuration.UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(connectionString)));
    builder.Services.AddHangfireServer(options =>
    {
        var configuredWorkerCount = builder.Configuration.GetValue<int?>("Hangfire:WorkerCount");
        options.WorkerCount = configuredWorkerCount.GetValueOrDefault() > 0
            ? configuredWorkerCount!.Value
            : Environment.ProcessorCount;
    });
    builder.Services.AddScoped<IBookingConfirmationEmailQueue, HangfireBookingConfirmationEmailQueue>();
}
else
{
    builder.Services.AddScoped<IBookingConfirmationEmailQueue, NoOpBookingConfirmationEmailQueue>();
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret ?? string.Empty)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/bookings"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(UserRole.Admin.ToString()));
    options.AddPolicy("UserOnly", policy => policy.RequireRole(UserRole.Customer.ToString()));
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseSerilogRequestLogging();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<ApiLoggingMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

if (app.Environment.IsEnvironment("IntegrationTests"))
{
    app.MapGet("/test/throw", (HttpContext _) =>
    {
        throw new InvalidOperationException("Integration test exception.");
    });
}

app.MapControllers();
app.MapHub<BookingHub>("/hubs/bookings");

app.Run();

public partial class Program;

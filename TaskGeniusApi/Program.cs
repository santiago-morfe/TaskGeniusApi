using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskGeniusApi.Data;
using TaskGeniusApi.Services.Auth;
using TaskGeniusApi.Services.Users;
using TaskGeniusApi.Services.Tasks;
using TaskGeniusApi.Services.Genius;
// rate limit
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configuración de servicios
ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

// Configuración de la pipeline HTTP
ConfigureMiddleware(app, builder.Environment, args);

app.Run();

void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Configuración básica
    services.AddControllers();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    services.AddHttpClient();

    // Configuración de la base de datos
    ConfigureDatabase(services, configuration);

    // Configuración de autenticación JWT
    ConfigureJwtAuthentication(services, configuration);

    // Configuración de CORS
    ConfigureCors(services);

    // Configuración de limitación de tasa
    services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("FixedWindowPolicy", limiterOptions =>
        {
            limiterOptions.PermitLimit = 30; // Máximo de 30 peticiones
            limiterOptions.Window = TimeSpan.FromMinutes(1); // Por minuto
        });
    });

    // Registro de servicios de aplicación
    RegisterApplicationServices(services);
}

void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
               .EnableSensitiveDataLogging()
               .LogTo(Console.WriteLine, LogLevel.Information);
    });
}

void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
{
    services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ??
                    throw new InvalidOperationException("JWT Key is not configured")))
        };
    });
}

void ConfigureCors(IServiceCollection services)
{
    services.AddCors(options =>
    {
        options.AddPolicy("PermitirTodo", policy =>
        {
            policy.WithOrigins("http://localhost:5173", "https://taskgeniusapi-production-5575.up.railway.app","https://taskgeniusapp.netlify.app/login")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

void RegisterApplicationServices(IServiceCollection services)
{
    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<IUsersService, UsersService>();
    services.AddScoped<ITasksServices, TasksServices>();
    services.AddScoped<IGeniusService, GeniusService>();
}

void ConfigureMiddleware(WebApplication app, IWebHostEnvironment env, string[] args)
{
    // Manejo de migraciones
    HandleMigrations(app, args);

    // Configuración de Swagger solo en desarrollo
    if (env.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
    }

    // Middleware estándar
    app.UseCors("PermitirTodo"); // Aseguramos que se use la política "PermitirTodo" en todos los entornos
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.MapControllers();
}

void HandleMigrations(WebApplication app, string[] args)
{
    // Ejecutar migraciones si se especifica el flag --migrate o en desarrollo
    if (args.Contains("--migrate") || app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            db.Database.Migrate();
            Console.WriteLine("Migraciones aplicadas exitosamente.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al aplicar migraciones: {ex.Message}");
        }

        if (args.Contains("--migrate"))
        {
            Environment.Exit(0); // Termina la ejecución después de migrar
        }
    }
}
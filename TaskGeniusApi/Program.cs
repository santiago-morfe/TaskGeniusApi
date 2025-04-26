// Configuración básica
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskGeniusApi.Data;
using TaskGeniusApi.Services.Auth;
using TaskGeniusApi.Services.Users;
using TaskGeniusApi.Services.Tasks;
using TaskGeniusApi.Services.Genius;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container
builder.Services.AddControllers();

// SQLite Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
builder.Services.AddAuthentication(options =>
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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured")))
    };
});

// HttpClient para APIs externas
builder.Services.AddHttpClient("ExternalApi", client =>
{
    var baseUrl = builder.Configuration["ExternalApi:BaseUrl"] ?? throw new InvalidOperationException("External API BaseUrl is not configured");
    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// CORS Configuration optimizada
builder.Services.AddCors(options =>
{
    options.AddPolicy("RailwayPolicy", policyBuilder => 
        policyBuilder.WithOrigins(builder.Configuration["AllowedOrigins"]?.Split(';') ?? Array.Empty<string>())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Data Protection Configuration
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("TaskGeniusApi")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(14));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registro de servicios
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<ITasksServices, TasksServices>();
builder.Services.AddScoped<IGeniusService, GeniusService>();

var app = builder.Build();

// Configuración del pipeline
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All,
    ForwardLimit = 2,
    AllowedHosts = builder.Configuration["AllowedHosts"]?.Split(';')?.ToList() ?? new List<string>()
});

// Middleware personalizado para forzar método HTTP
app.Use(async (context, next) =>
{
    var forwardedMethod = context.Request.Headers["X-Forwarded-Method"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwardedMethod))
    {
        context.Request.Method = forwardedMethod;
    }
    await next();
});

// Solo usar HTTPS Redirection en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}


// Manejar migraciones desde línea de comandos
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    return; // Opcional: terminar después de migraciones
}
app.UseCors("RailwayPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoint para debug de headers
app.MapGet("/debug/headers", (HttpContext context) => 
    context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()));

// Crear directorio para keys si no existe
var keysDir = Path.Combine(app.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysDir);

app.Run();
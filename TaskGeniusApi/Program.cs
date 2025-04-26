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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registro de servicios
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<ITasksServices, TasksServices>();
builder.Services.AddScoped<IGeniusService, GeniusService>();

// Registrar HttpClient
builder.Services.AddHttpClient();

var app = builder.Build();

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

app.Run();
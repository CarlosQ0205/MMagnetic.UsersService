using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MMagnetic.UsersService.Data;
using MMagnetic.UsersService.Models;
using System.Text;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
builder.Host.UseSerilog((ctx, lc) => lc
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
);

// Logging configured via Serilog

// CORS: permitir comunicación desde el frontend .NET (Blazor, etc.)
var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalFrontend", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// -------------------------------------------------------
// 1. BASE DE DATOS
// -------------------------------------------------------
builder.Services.AddDbContext<UsersDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionBaseDatos"));
});

// -------------------------------------------------------
// 2. CONTROLADORES
// -------------------------------------------------------
builder.Services.AddControllers();

// -------------------------------------------------------
// 3. CONFIGURAR JWT
// -------------------------------------------------------
var jwtSecreto = builder.Configuration["Jwt:Secreto"]
    ?? throw new Exception("Falta Jwt:Secreto en appsettings.json.");

var jwtKey = Encoding.UTF8.GetBytes(jwtSecreto);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Emisor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey)
        };
    });

// -------------------------------------------------------
// 4. SWAGGER CON AUTORIZACI�N
// -------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MMagnetic Users API",
        Version = "v1"
    });

    // Activar bot�n de "Authorize" en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT con Bearer. Ejemplo: Bearer {token}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// -------------------------------------------------------
// Seed roles y admin inicial en desarrollo
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    dbContext.Database.EnsureCreated();
    await SeedRolesAndAdminAsync(dbContext, builder.Configuration);
}

// -------------------------------------------------------
// 5. USO DE SWAGGER
// -------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Seed and debug logging removed for production readiness.

// -------------------------------------------------------
// 6. MIDDLEWARES: CORS + AUTENTICACI�N + AUTORIZACI�N
// -------------------------------------------------------
app.UseCors("AllowLocalFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

async Task SeedRolesAndAdminAsync(UsersDbContext context, IConfiguration configuration)
{
    var adminRole = await EnsureRoleExistsAsync(context, "Admin");
    await EnsureRoleExistsAsync(context, "User");

    var adminConfig = configuration.GetSection("AdminUser");
    if (!adminConfig.Exists())
        return;

    var numeroDocumento = adminConfig["NumeroDocumento"];
    var correoElectronico = adminConfig["CorreoElectronico"];
    var password = adminConfig["Password"];
    var primerNombre = adminConfig["PrimerNombre"];
    var primerApellido = adminConfig["PrimerApellido"];
    var tipoDocumento = adminConfig["TipoDocumento"] ?? "CC";
    var roles = adminConfig.GetSection("Roles").Get<List<string>>() ?? new List<string> { "Admin" };

    if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(correoElectronico) || string.IsNullOrWhiteSpace(numeroDocumento))
        return;

    var existeAdmin = await context.Usuarios
        .AnyAsync(u => u.NumeroDocumento == numeroDocumento || u.CorreoElectronico == correoElectronico);

    if (existeAdmin)
        return;

    var saltBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
    var salt = Convert.ToBase64String(saltBytes);
    var hash = BCrypt.Net.BCrypt.HashPassword(password + salt);

    var usuario = new Usuario
    {
        UsuarioId = Guid.NewGuid(),
        TipoDocumento = tipoDocumento,
        NumeroDocumento = numeroDocumento,
        PrimerNombre = primerNombre,
        PrimerApellido = primerApellido,
        CorreoElectronico = correoElectronico,
        Salt = salt,
        PasswordHash = hash,
        FechaCreacion = DateTime.UtcNow,
        EsActivo = true
    };

    context.Usuarios.Add(usuario);
    await context.SaveChangesAsync();

    foreach (var rolNombre in roles.Distinct(StringComparer.OrdinalIgnoreCase))
    {
        var rol = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == rolNombre);
        if (rol != null)
        {
            context.UsuariosRoles.Add(new UsuarioRol
            {
                UsuarioRolId = Guid.NewGuid(),
                UsuarioId = usuario.UsuarioId,
                RolId = rol.RolId,
                FechaAsignacion = DateTime.UtcNow
            });
        }
    }

    await context.SaveChangesAsync();
}

async Task<Rol> EnsureRoleExistsAsync(UsersDbContext context, string roleName)
{
    var rol = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == roleName);
    if (rol != null)
        return rol;

    rol = new Rol
    {
        RolId = Guid.NewGuid(),
        Nombre = roleName,
        Descripcion = roleName == "Admin" ? "Administrador del sistema" : "Rol de usuario estándar",
        FechaCreacion = DateTime.UtcNow,
        EsActivo = true
    };

    context.Roles.Add(rol);
    await context.SaveChangesAsync();
    return rol;
}

app.Run();

// Expose Program class for integration tests
public partial class Program { }

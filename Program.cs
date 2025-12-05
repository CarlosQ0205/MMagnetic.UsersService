using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MMagnetic.UsersService.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
// 4. SWAGGER CON AUTORIZACIÓN
// -------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "MMagnetic Users API",
        Version = "v1"
    });

    // Activar botón de "Authorize" en Swagger
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
// 5. USO DE SWAGGER
// -------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// -------------------------------------------------------
// 6. MIDDLEWARES: AUTENTICACIÓN + AUTORIZACIÓN
// -------------------------------------------------------
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

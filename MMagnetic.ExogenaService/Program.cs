using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Services.CargaMasiva;
using MMagnetic.ExogenaService.Services.Clientes;
using MMagnetic.ExogenaService.Services.Formato1019;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// -------------------------------------------------------
// CORS: permitir comunicación desde el frontend (mismo esquema que UsersService).
// -------------------------------------------------------
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
// JWT: valida los tokens emitidos por MMagnetic.UsersService (misma clave,
// emisor y audiencia; este servicio no emite tokens, solo los valida).
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
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        };
    });
builder.Services.AddAuthorization();

// -------------------------------------------------------
// Bases de datos: MM_Clientes, MM_DIAN y MM_Formatos son bases separadas.
// -------------------------------------------------------
builder.Services.AddDbContext<ClientesDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MMClientes")));

builder.Services.AddDbContext<DianDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MMDian")));

builder.Services.AddDbContext<FormatosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MMFormatos")));

// -------------------------------------------------------
// Formato 1019: validador, homologador DIAN, ensamblador y clasificador.
// -------------------------------------------------------
builder.Services.AddScoped<IFormato1019Validator, Formato1019Validator>();
builder.Services.AddScoped<IHomologadorDianService, HomologadorDianService>();
builder.Services.AddScoped<IFormato1019EnsambladorService, Formato1019EnsambladorService>();
builder.Services.AddScoped<IFormato1019ClasificadorService, Formato1019ClasificadorService>();
builder.Services.AddScoped<IFormato1019ExportService, Formato1019ExportService>();

// -------------------------------------------------------
// Carga de datos: Clientes, Cotitulares, Datos_Financieros (manual y masiva).
// -------------------------------------------------------
builder.Services.AddScoped<IArchivoTabularReader, ArchivoTabularReader>();
builder.Services.AddScoped<IResolutorCatalogoDianService, ResolutorCatalogoDianService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ICotitularService, CotitularService>();
builder.Services.AddScoped<IDatoFinancieroService, DatoFinancieroService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowLocalFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

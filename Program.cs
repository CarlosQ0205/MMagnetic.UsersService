using Microsoft.EntityFrameworkCore;
using MMagnetic.UsersService.Data;
using MMagnetic.UsersService.Services;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURAR CONEXIÓN A SQL SERVER
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// INYECCIÓN DE DEPENDENCIAS
builder.Services.AddScoped<IUserService, UserService>();

// AGREGAR CONTROLADORES
builder.Services.AddControllers();

// ACTIVAR SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware para desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

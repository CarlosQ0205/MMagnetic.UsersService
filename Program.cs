using Microsoft.EntityFrameworkCore;
using MMagnetic.UsersService.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Cadena de conexión (ajústala con la tuya real)
builder.Services.AddDbContext<UsersDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("UsersDB"));
});

// 2. Añadir controladores
builder.Services.AddControllers();

// 3. Swagger (Opcional pero recomendado)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Swagger - solo en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 5. Autorización (lo activaremos cuando construyamos JWT)
app.UseAuthorization();

app.MapControllers();

app.Run();

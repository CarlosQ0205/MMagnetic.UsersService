using Microsoft.EntityFrameworkCore;
using MMagnetic.ExogenaService.Data;
using MMagnetic.ExogenaService.Services.Formato1019;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

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

app.UseAuthorization();

app.MapControllers();

app.Run();

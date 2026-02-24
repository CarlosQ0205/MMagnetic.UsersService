using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMagnetic.UsersService.Data;
using MMagnetic.UsersService.Models;

namespace MMagnetic.UsersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly UsersDbContext _context;

        public UsuariosController(UsersDbContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------------
        // 1. Registrar Usuario
        // ------------------------------------------------------------
        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verificar si el correo ya existe
            var existe = await _context.Usuarios
                .AnyAsync(u => u.CorreoElectronico == request.CorreoElectronico);

            if (existe)
                return BadRequest("El correo ya está registrado.");

            // Generar salt y hash
            var saltBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);
            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password + salt);

            var usuario = new Usuario
            {
                UsuarioId = Guid.NewGuid(),
                TipoDocumento = request.TipoDocumento,
                NumeroDocumento = request.NumeroDocumento,
                PrimerNombre = request.PrimerNombre,
                SegundoNombre = request.SegundoNombre,
                PrimerApellido = request.PrimerApellido,
                SegundoApellido = request.SegundoApellido,
                CorreoElectronico = request.CorreoElectronico,
                Telefono = request.Telefono,
                Salt = salt,
                PasswordHash = hash,
                FechaCreacion = DateTime.UtcNow,
                EsActivo = true
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return Ok(new { usuario.UsuarioId, usuario.PrimerNombre, usuario.PrimerApellido, usuario.CorreoElectronico });
        }

        // ------------------------------------------------------------
        // 2. Obtener todos los usuarios
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return Ok(usuarios);
        }

        // ------------------------------------------------------------
        // 3. Obtener usuario por ID
        // ------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            return Ok(usuario);
        }

        // ------------------------------------------------------------
        // 4. Actualizar usuario
        // ------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Usuario usuarioActualizado)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            usuario.PrimerNombre = usuarioActualizado.PrimerNombre;
            usuario.SegundoNombre = usuarioActualizado.SegundoNombre;
            usuario.PrimerApellido = usuarioActualizado.PrimerApellido;
            usuario.SegundoApellido = usuarioActualizado.SegundoApellido;
            usuario.Telefono = usuarioActualizado.Telefono;

            await _context.SaveChangesAsync();

            return Ok(usuario);
        }

        // ------------------------------------------------------------
        // 5. Eliminar usuario
        // ------------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound();

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok("Usuario eliminado correctamente.");
        }
    }
}

// DTO para registro
public class RegisterRequest
{
    public string? TipoDocumento { get; set; }
    public string? NumeroDocumento { get; set; }
    public string? PrimerNombre { get; set; }
    public string? SegundoNombre { get; set; }
    public string? PrimerApellido { get; set; }
    public string? SegundoApellido { get; set; }
    public string? CorreoElectronico { get; set; }
    public string? Telefono { get; set; }
    public required string Password { get; set; }
}

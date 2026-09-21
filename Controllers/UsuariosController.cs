using Microsoft.AspNetCore.Authorization;
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
        // 1. Registrar Usuario (Solo Admin)
        // ------------------------------------------------------------
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var usuarios = await _context.Usuarios.ToListAsync();
            return Ok(usuarios);
        }

        // ------------------------------------------------------------
        // 2.1 Endpoint protegido
        // ------------------------------------------------------------
        [Authorize]
        [HttpGet("protected")]
        public IActionResult ProtectedEndpoint()
        {
            return Ok(new { message = "Acceso autorizado al endpoint protegido" });
        }

        // ------------------------------------------------------------
        // 3. Obtener usuario por ID
        // ------------------------------------------------------------
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

        // ============================================================
        // ENDPOINTS PARA ADMINISTRADOR
        // ============================================================

        // ------------------------------------------------------------
        // Admin: Crear Usuario
        // ------------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPost("admin/crear")]
        public async Task<IActionResult> CrearUsuarioAdmin([FromBody] CreateUserAdminRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existe = await _context.Usuarios
                .AnyAsync(u => u.NumeroDocumento == request.NumeroDocumento || u.CorreoElectronico == request.CorreoElectronico);

            if (existe)
                return BadRequest("El documento o correo ya está registrado.");

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
                EsActivo = request.EsActivo
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            await AssignRolesToUserAsync(usuario.UsuarioId, request.Roles);

            return Ok(new { usuario.UsuarioId, usuario.PrimerNombre, usuario.PrimerApellido, usuario.NumeroDocumento });
        }

        private async Task AssignRolesToUserAsync(Guid usuarioId, IEnumerable<string>? roles)
        {
            if (roles == null)
                return;

            foreach (var rolNombre in roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var rol = await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == rolNombre);
                if (rol == null)
                    continue;

                var existeAsignacion = await _context.UsuariosRoles
                    .AnyAsync(ur => ur.UsuarioId == usuarioId && ur.RolId == rol.RolId);

                if (!existeAsignacion)
                {
                    _context.UsuariosRoles.Add(new UsuarioRol
                    {
                        UsuarioRolId = Guid.NewGuid(),
                        UsuarioId = usuarioId,
                        RolId = rol.RolId,
                        FechaAsignacion = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        // ------------------------------------------------------------
        // Admin: Obtener todos los usuarios
        // ------------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/listar")]
        public async Task<IActionResult> ListarUsuariosAdmin()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.UsuariosRoles)
                .ThenInclude(ur => ur.Rol)
                .Select(u => new
                {
                    u.UsuarioId,
                    u.NumeroDocumento,
                    u.PrimerNombre,
                    u.PrimerApellido,
                    u.CorreoElectronico,
                    u.Telefono,
                    u.EsActivo,
                    u.FechaCreacion,
                    Roles = u.UsuariosRoles!.Select(ur => ur.Rol!.Nombre).ToList()
                })
                .ToListAsync();

            return Ok(usuarios);
        }

        // ------------------------------------------------------------
        // Admin: Actualizar usuario
        // ------------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpPut("admin/{id}")]
        public async Task<IActionResult> ActualizarUsuarioAdmin(Guid id, [FromBody] UpdateUserAdminRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            usuario.PrimerNombre = request.PrimerNombre ?? usuario.PrimerNombre;
            usuario.SegundoNombre = request.SegundoNombre ?? usuario.SegundoNombre;
            usuario.PrimerApellido = request.PrimerApellido ?? usuario.PrimerApellido;
            usuario.SegundoApellido = request.SegundoApellido ?? usuario.SegundoApellido;
            usuario.CorreoElectronico = request.CorreoElectronico ?? usuario.CorreoElectronico;
            usuario.Telefono = request.Telefono ?? usuario.Telefono;
            usuario.EsActivo = request.EsActivo ?? usuario.EsActivo;

            // Si envían nueva contraseña, actualizarla
            if (!string.IsNullOrEmpty(request.Password))
            {
                var saltBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
                var salt = Convert.ToBase64String(saltBytes);
                usuario.Salt = salt;
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password + salt);
            }

            await _context.SaveChangesAsync();

            return Ok(new { usuario.UsuarioId, usuario.PrimerNombre, usuario.PrimerApellido });
        }

        // ------------------------------------------------------------
        // Admin: Eliminar usuario
        // ------------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpDelete("admin/{id}")]
        public async Task<IActionResult> EliminarUsuarioAdmin(Guid id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();

            return Ok("Usuario eliminado correctamente.");
        }

        // ------------------------------------------------------------
        // Admin: Obtener usuario por ID
        // ------------------------------------------------------------
        [Authorize(Roles = "Admin")]
        [HttpGet("admin/{id}")]
        public async Task<IActionResult> ObtenerUsuarioAdmin(Guid id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.UsuariosRoles)
                .ThenInclude(ur => ur.Rol)
                .Where(u => u.UsuarioId == id)
                .Select(u => new
                {
                    u.UsuarioId,
                    u.NumeroDocumento,
                    u.PrimerNombre,
                    u.PrimerApellido,
                    u.CorreoElectronico,
                    u.Telefono,
                    u.EsActivo,
                    u.FechaCreacion,
                    Roles = u.UsuariosRoles!.Select(ur => ur.Rol!.Nombre).ToList()
                })
                .FirstOrDefaultAsync();

            if (usuario == null)
                return NotFound("Usuario no encontrado.");

            return Ok(usuario);
        }
    }

    // ============================================================
    // DTOs para Admin
    // ============================================================

    public class CreateUserAdminRequest
    {
        public required string TipoDocumento { get; set; }
        public required string NumeroDocumento { get; set; }
        public required string PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public required string PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public required string CorreoElectronico { get; set; }
        public string? Telefono { get; set; }
        public required string Password { get; set; }
        public bool EsActivo { get; set; } = true;
        public List<string>? Roles { get; set; } = new List<string> { "User" };
    }

    public class UpdateUserAdminRequest
    {
        public string? PrimerNombre { get; set; }
        public string? SegundoNombre { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? CorreoElectronico { get; set; }
        public string? Telefono { get; set; }
        public string? Password { get; set; }
        public bool? EsActivo { get; set; }
        public List<string>? Roles { get; set; }
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

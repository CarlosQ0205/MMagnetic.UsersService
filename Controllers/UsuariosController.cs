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
        public async Task<IActionResult> Registrar([FromBody] Usuario usuario)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verificar si el correo ya existe
            var existe = await _context.Usuarios
                .AnyAsync(u => u.CorreoElectronico == usuario.CorreoElectronico);

            if (existe)
                return BadRequest("El correo ya está registrado.");

            usuario.UsuarioId = Guid.NewGuid();
            usuario.FechaCreacion = DateTime.UtcNow;
            usuario.EsActivo = true;

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return Ok(usuario);
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

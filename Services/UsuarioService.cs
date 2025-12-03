using Microsoft.EntityFrameworkCore;
using MMagnetic.UsersService.Data;
using MMagnetic.UsersService.Models;

namespace MMagnetic.UsersService.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly UsersDbContext _context;

        public UsuarioService(UsersDbContext context)
        {
            _context = context;
        }

        // ============================
        // REGISTRAR USUARIO
        // ============================
        public async Task<Usuario> RegistrarUsuario(Usuario usuario)
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        // ============================
        // LOGIN
        // ============================
        public async Task<Usuario?> Login(string correoElectronico, string contraseña)
        {
            return await _context.Usuarios
                .FirstOrDefaultAsync(x => x.CorreoElectronico == correoElectronico);
        }

        // ============================
        // OBTENER POR ID
        // ============================
        public async Task<Usuario?> ObtenerPorId(Guid usuarioId)
        {
            return await _context.Usuarios.FindAsync(usuarioId);
        }

        // ============================
        // OBTENER TODOS
        // ============================
        public async Task<List<Usuario>> ObtenerTodos()
        {
            return await _context.Usuarios.ToListAsync();
        }

        // ============================
        // ACTUALIZAR USUARIO
        // ============================
        public async Task<Usuario?> ActualizarUsuario(Usuario usuario)
        {
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
            return usuario;
        }

        // ============================
        // ELIMINAR USUARIO
        // ============================
        public async Task<bool> EliminarUsuario(Guid usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return false;

            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

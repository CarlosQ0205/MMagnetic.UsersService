using MMagnetic.UsersService.Models;

namespace MMagnetic.UsersService.Services
{
    public interface IUsuarioService
    {
        Task<Usuario> RegistrarUsuario(Usuario usuario);
        Task<Usuario?> Login(string correoElectronico, string contraseña);
        Task<Usuario?> ObtenerPorId(Guid usuarioId);
        Task<List<Usuario>> ObtenerTodos();
        Task<bool> EliminarUsuario(Guid usuarioId);
        Task<Usuario?> ActualizarUsuario(Usuario usuario);
    }
}

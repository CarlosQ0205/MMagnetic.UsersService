using MMagnetic.UsersService.Models;
using System.Collections.Generic;

namespace MMagnetic.UsersService.Services
{
    public interface IUserService
    {
        // Método de login (firma que usa el controlador)
        User Login(string nombreUsuario, string contraseña);

        IEnumerable<User> GetAll();
        User GetById(int id);
        User Create(User user);
        User Update(User user);
        void Delete(int id);
    }
}

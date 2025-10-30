using Microsoft.AspNetCore.Mvc;
using MMagnetic.UsersService.Models;
using MMagnetic.UsersService.Services;

namespace MMagnetic.UsersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        // Inyección de dependencias
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET api/users
        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _userService.GetAll();
            return Ok(users);
        }

        // GET api/users/5
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var user = _userService.GetById(id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }

        // POST api/users
        [HttpPost]
        public IActionResult Create([FromBody] User user)
        {
            var createdUser = _userService.Create(user);
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }

        // PUT api/users/5
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] User user)
        {
            user.Id = id;
            var updatedUser = _userService.Update(user);
            return Ok(updatedUser);
        }

        // DELETE api/users/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            _userService.Delete(id);
            return NoContent();
        }

        // GET api/users/login?userName=xxx&password=yyy
        [HttpGet("login")]
        public IActionResult Login([FromQuery] string userName, [FromQuery] string password)
        {
            var user = _userService.Login(userName, password);

            if (user == null)
                return Unauthorized(new { message = "Credenciales incorrectas" });

            return Ok(new
            {
                message = "Login exitoso",
                usuario = new
                {
                    user.Id,
                    user.NombreUsuario,
                    user.Correo
                }
            });
        }
    }
}

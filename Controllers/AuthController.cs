using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MMagnetic.UsersService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MMagnetic.UsersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UsersDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(UsersDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ---------------------------------------------------------------
        // ----------------------   LOGIN   ------------------------------
        // ---------------------------------------------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.CorreoElectronico == request.Correo);

            if (usuario == null)
                return Unauthorized("Credenciales inválidas (usuario no existe).");

            // Validar contraseña
            if (!VerificarPassword(request.Password, usuario.PasswordHash, usuario.Salt))
                return Unauthorized("Credenciales inválidas (password incorrecto).");

            // Actualizar último acceso
            usuario.FechaUltimoAcceso = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Crear el token
            string token = GenerarJwt(usuario);

            // Crear refresh token
            var refresh = CrearRefreshToken(usuario.UsuarioId);
            await _context.RefreshTokens.AddAsync(refresh);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = token,
                RefreshToken = refresh.Token,
                Usuario = new
                {
                    usuario.UsuarioId,
                    usuario.PrimerNombre,
                    usuario.PrimerApellido,
                    usuario.CorreoElectronico
                }
            });
        }

        // ---------------------------------------------------------------
        // ----------------- REFRESH TOKEN ------------------------------
        // ---------------------------------------------------------------
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var refresh = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == request.RefreshToken && !r.EsRevocado);

            if (refresh == null)
                return Unauthorized("Refresh token inválido.");

            var usuario = await _context.Usuarios.FindAsync(refresh.UsuarioId);

            if (usuario == null)
                return Unauthorized("Usuario no existe.");

            // Revocar refresh token actual
            refresh.EsRevocado = true;

            // Crear nuevo refresh token
            var nuevoRefresh = CrearRefreshToken(usuario.UsuarioId);

            _context.RefreshTokens.Add(nuevoRefresh);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = GenerarJwt(usuario),
                RefreshToken = nuevoRefresh.Token
            });
        }

        // ---------------------------------------------------------------
        // -------------------- MÉTODOS PRIVADOS -------------------------
        // ---------------------------------------------------------------

        private string GenerarJwt(Usuario usuario)
        {
            var claves = _config["Jwt:Key"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claves));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("usuarioId", usuario.UsuarioId.ToString()),
                new Claim("correo", usuario.CorreoElectronico),
                new Claim("nombre", usuario.PrimerNombre),
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private bool VerificarPassword(string password, string hash, string salt)
        {
            var sha = SHA256.Create();
            var combinado = password + salt;
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(combinado));
            var hashString = Convert.ToBase64String(hashBytes);

            return hashString == hash;
        }

        private RefreshToken CrearRefreshToken(Guid usuarioId)
        {
            return new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                IP = HttpContext.Connection.RemoteIpAddress?.ToString(),
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddDays(7),
                EsRevocado = false
            };
        }
    }

    // ---------------------------------------------------------------
    // ---------------------- MODELOS DTO -----------------------------
    // ---------------------------------------------------------------

    public class LoginRequest
    {
        public string Correo { get; set; }
        public string Password { get; set; }
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MMagnetic.UsersService.Data;
using MMagnetic.UsersService.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

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
        // LOGIN con Documento + Password
        // ---------------------------------------------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u =>
                    u.TipoDocumento == request.TipoDocumento &&
                    u.NumeroDocumento == request.NumeroDocumento);

            if (usuario == null)
                return Unauthorized("Usuario no encontrado.");

            // Verificar contraseña con BCrypt
            if (!usuario.EsActivo)
                return Unauthorized("Usuario inactivo.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password + usuario.Salt, usuario.PasswordHash))
                return Unauthorized("Contraseña incorrecta.");

            usuario.FechaUltimoAcceso = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            string jwt = await GenerarJwt(usuario);
            var refresh = CrearRefreshToken(usuario.UsuarioId);

            await _context.RefreshTokens.AddAsync(refresh);
            await _context.SaveChangesAsync();

            var roles = await _context.UsuariosRoles
                .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                .Include(ur => ur.Rol)
                .Select(ur => ur.Rol!.Nombre)
                .ToListAsync();

            return Ok(new
            {
                Token = jwt,
                RefreshToken = refresh.Token,
                Usuario = new
                {
                    usuario.UsuarioId,
                    Nombre = usuario.PrimerNombre + " " + usuario.PrimerApellido,
                    usuario.CorreoElectronico,
                    Roles = roles
                }
            });
        }

        // ---------------------------------------------------------------
        // REFRESH TOKEN
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

            refresh.EsRevocado = true;

            var nuevo = CrearRefreshToken(usuario.UsuarioId);

            _context.RefreshTokens.Add(nuevo);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Token = await GenerarJwt(usuario),
                RefreshToken = nuevo.Token
            });
        }

        // ---------------------------------------------------------------
        // INFO USUARIO AUTENTICADO
        // ---------------------------------------------------------------
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst("usuarioId")?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            if (!Guid.TryParse(userIdClaim, out var usuarioId))
                return Unauthorized();

            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                return NotFound();

            var roles = await _context.UsuariosRoles
                .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                .Include(ur => ur.Rol)
                .Select(ur => ur.Rol!.Nombre)
                .ToListAsync();

            return Ok(new
            {
                usuario.UsuarioId,
                usuario.TipoDocumento,
                usuario.NumeroDocumento,
                usuario.PrimerNombre,
                usuario.SegundoNombre,
                usuario.PrimerApellido,
                usuario.SegundoApellido,
                usuario.CorreoElectronico,
                usuario.Telefono,
                Roles = roles
            });
        }

        // ---------------------------------------------------------------
        // MÉTODOS PRIVADOS
        // ---------------------------------------------------------------

        private async Task<string> GenerarJwt(Usuario usuario)
        {
            var secreto = _config["Jwt:Secreto"]
                ?? throw new Exception("Error: falta Jwt:Secreto en appsettings.json");

            var expiracionStr = _config["Jwt:ExpiracionMinutos"]
                ?? throw new Exception("Error: falta Jwt:ExpiracionMinutos en appsettings.json");

            int expiracion = Convert.ToInt32(expiracionStr);

            var key = Encoding.UTF8.GetBytes(secreto);

            var claims = new[]
            {
        new Claim("usuarioId", usuario.UsuarioId.ToString()),
        new Claim("documento", usuario.NumeroDocumento ?? ""),
        new Claim("correo", usuario.CorreoElectronico ?? ""),
        new Claim("nombre", usuario.PrimerNombre ?? "")
    };

            // Agregar roles del usuario al JWT
            var roles = await _context.UsuariosRoles
                .Where(ur => ur.UsuarioId == usuario.UsuarioId)
                .Include(ur => ur.Rol)
                .Select(ur => ur.Rol!.Nombre)
                .ToListAsync();

            var claimsConRoles = claims.ToList();
            foreach (var rol in roles)
            {
                claimsConRoles.Add(new Claim(ClaimTypes.Role, rol ?? ""));
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Emisor"],
                audience: _config["Jwt:Audiencia"],
                claims: claimsConRoles,
                expires: DateTime.UtcNow.AddMinutes(expiracion),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256
                )
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private RefreshToken CrearRefreshToken(Guid usuarioId)
        {
            return new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                FechaCreacion = DateTime.UtcNow,
                FechaExpiracion = DateTime.UtcNow.AddDays(7),
                EsRevocado = false,
                IP = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
        }
    }

    // ---------------------------------------------------------------
    // DTOs
    // ---------------------------------------------------------------
    public class LoginRequest
    {
        public required string TipoDocumento { get; set; }
        public required string NumeroDocumento { get; set; }
        public required string Password { get; set; }
    }

    public class RefreshRequest
    {
        public required string RefreshToken { get; set; }
    }
}

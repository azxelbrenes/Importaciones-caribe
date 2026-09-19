using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Caribe.AccesoDatos.Identidad;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Caribe.LogicaNegocio.Seguridad;

public interface IJwtService
{
    (string token, DateTimeOffset expira) CrearAccessToken(
        AppUser usuario, IEnumerable<string> roles);
}

public class JwtService : IJwtService
{
    private readonly JwtOpciones _op;

    public JwtService(IOptions<JwtOpciones> op) => _op = op.Value;

    public (string token, DateTimeOffset expira) CrearAccessToken(
        AppUser usuario, IEnumerable<string> roles)
    {
        var expira = DateTimeOffset.UtcNow.AddMinutes(_op.MinutosAccessToken);

        // El token va firmado pero NO cifrado: cualquiera puede leer su
        // contenido pegandolo en jwt.io. Por eso aqui solo van datos
        // que ya conoce el usuario, nunca nada sensible.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(ClaimTypes.Name, usuario.NombreCompleto),
            new(ClaimTypes.Email, usuario.Email ?? string.Empty),

            // Identificador unico del token. Permitiria revocarlo de
            // forma individual si algun dia hiciera falta.
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var llave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_op.Key));
        var credenciales = new SigningCredentials(llave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _op.Issuer,
            audience: _op.Audience,
            claims: claims,
            expires: expira.UtcDateTime,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

using Caribe.LogicaNegocio.Dtos.Usuarios;
using Caribe.Utilitarios;

namespace Caribe.LogicaNegocio.Implementaciones;

/// <summary>
/// La propia cuenta. Archivo aparte de la misma clase parcial.
/// </summary>
public partial class UsuarioLN
{
    public async Task<Respuesta<PerfilDto>> ObtenerPerfilAsync(
        string usuarioId, CancellationToken ct = default)
    {
        var u = await _users.FindByIdAsync(usuarioId);

        if (u is null)
            return Respuesta<PerfilDto>.NoEncontrado("El usuario no existe.");

        var roles = await _users.GetRolesAsync(u);
        var dobleFactor = await _users.GetTwoFactorEnabledAsync(u);

        return Respuesta<PerfilDto>.Ok(new PerfilDto(
            u.NombreCompleto,
            u.Email ?? string.Empty,
            roles,
            dobleFactor,
            dobleFactor ? await _users.CountRecoveryCodesAsync(u) : 0,
            u.UltimoAcceso,
            u.CreadoEn));
    }
}

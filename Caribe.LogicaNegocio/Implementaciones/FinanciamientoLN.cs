using Caribe.AccesoDatos.Contexto;
using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Dtos.Financiamiento;
using Caribe.LogicaNegocio.Interfaces;
using Caribe.Utilitarios;
using Microsoft.EntityFrameworkCore;

namespace Caribe.LogicaNegocio.Implementaciones;

public class FinanciamientoLN : IFinanciamientoLN
{
    private readonly CaribeContext _db;

    public FinanciamientoLN(CaribeContext db) => _db = db;

    public async Task<Respuesta<ConfiguracionFinanciamientoDto>> ObtenerAsync(
        CancellationToken ct = default)
    {
        var c = await Leer(ct);

        return Respuesta<ConfiguracionFinanciamientoDto>.Ok(new ConfiguracionFinanciamientoDto(
            c.Activo,
            c.PorcentajePrima,
            c.PorcentajeInteres,
            c.PlazoMinimoMeses,
            c.PlazoMaximoMeses,
            c.PlazosDisponibles,
            c.ActualizadoEn));
    }

    public async Task<Respuesta<FinanciamientoPublicoDto>> ObtenerPublicoAsync(
        CancellationToken ct = default)
    {
        var c = await Leer(ct);

        return Respuesta<FinanciamientoPublicoDto>.Ok(new FinanciamientoPublicoDto(
            c.Activo,
            c.PorcentajePrima,
            c.Activo ? c.Plazos().ToList() : []));
    }

    public async Task<Respuesta<bool>> ActualizarAsync(
        ActualizarFinanciamientoDto dto, string usuarioId, CancellationToken ct = default)
    {
        if (dto.PlazoMinimoMeses > dto.PlazoMaximoMeses)
            return Respuesta<bool>.Invalido("El plazo mínimo no puede ser mayor al máximo.");

        var plazos = ParsearPlazos(dto.PlazosDisponibles);

        if (plazos.Count == 0)
            return Respuesta<bool>.Invalido(
                "Indique al menos un plazo, separados por coma. Ejemplo: 12,24,36");

        if (plazos.Any(p => p < dto.PlazoMinimoMeses || p > dto.PlazoMaximoMeses))
            return Respuesta<bool>.Invalido(
                $"Todos los plazos deben estar entre {dto.PlazoMinimoMeses} " +
                $"y {dto.PlazoMaximoMeses} meses.");

        var c = await _db.ConfiguracionFinanciamiento.FirstOrDefaultAsync(ct);

        if (c is null)
        {
            c = new ConfiguracionFinanciamiento();
            _db.ConfiguracionFinanciamiento.Add(c);
        }

        var antes = new { c.Activo, c.PorcentajePrima, c.PorcentajeInteres, c.PlazosDisponibles };

        c.Activo = dto.Activo;
        c.PorcentajePrima = dto.PorcentajePrima;
        c.PorcentajeInteres = dto.PorcentajeInteres;
        c.PlazoMinimoMeses = dto.PlazoMinimoMeses;
        c.PlazoMaximoMeses = dto.PlazoMaximoMeses;
        c.PlazosDisponibles = string.Join(',', plazos);
        c.ActualizadoEn = DateTimeOffset.UtcNow;
        c.ActualizadoPorId = usuarioId;

        _db.Registrar("ConfiguracionFinanciamiento", c.Id, AccionAuditoria.Editar, usuarioId,
            antes: antes,
            despues: new { c.Activo, c.PorcentajePrima, c.PorcentajeInteres, c.PlazosDisponibles });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    /// <summary>
    /// La fila se crea con la migracion, asi que no deberia faltar. Si
    /// faltara, se devuelve una apagada en vez de reventar: sin
    /// financiamiento el sitio sigue vendiendo.
    /// </summary>
    private async Task<ConfiguracionFinanciamiento> Leer(CancellationToken ct) =>
        await _db.ConfiguracionFinanciamiento.AsNoTracking().FirstOrDefaultAsync(ct)
        ?? new ConfiguracionFinanciamiento { Activo = false, ActualizadoEn = DateTimeOffset.UtcNow };

    /// <summary>"12, 24 , 36" → [12, 24, 36], sin repetidos y ordenados.</summary>
    private static List<short> ParsearPlazos(string texto) =>
        texto.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => short.TryParse(p.Trim(), out var v) ? v : (short)0)
            .Where(p => p > 0)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
}

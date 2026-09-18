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
        var c = await _db.ConfiguracionFinanciamiento
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        // La fila se crea con la migracion, asi que esto no deberia
        // pasar. Si pasa, es mejor devolver una configuracion apagada
        // que reventar: sin financiamiento el sistema sigue vendiendo.
        if (c is null)
            return Respuesta<ConfiguracionFinanciamientoDto>.Ok(
                new ConfiguracionFinanciamientoDto(
                    false, 50m, 0m, 12, 36, "12,24,36", null, false,
                    DateTimeOffset.UtcNow));

        return Respuesta<ConfiguracionFinanciamientoDto>.Ok(
            new ConfiguracionFinanciamientoDto(
                c.Activo,
                c.PorcentajePrima,
                c.TasaAnual,
                c.PlazoMinimoMeses,
                c.PlazoMaximoMeses,
                c.PlazosDisponibles,
                c.TextoLegal,
                c.EstaOperativo,
                c.ActualizadoEn));
    }

    public async Task<Respuesta<bool>> ActualizarAsync(
        ActualizarFinanciamientoDto dto, string usuarioId, CancellationToken ct = default)
    {
        if (dto.PlazoMinimoMeses > dto.PlazoMaximoMeses)
            return Respuesta<bool>.Invalido(
                "El plazo mínimo no puede ser mayor al máximo.");

        var plazos = ParsearPlazos(dto.PlazosDisponibles);

        if (plazos.Count == 0)
            return Respuesta<bool>.Invalido(
                "Indique al menos un plazo, separados por coma. Ejemplo: 12,24,36");

        if (plazos.Any(p => p < dto.PlazoMinimoMeses || p > dto.PlazoMaximoMeses))
            return Respuesta<bool>.Invalido(
                $"Todos los plazos deben estar entre {dto.PlazoMinimoMeses} " +
                $"y {dto.PlazoMaximoMeses} meses.");

        // Activar con tasa en cero mostraria financiamiento sin
        // interes, que no es lo acordado. Se bloquea explicitamente
        // en vez de dejar que pase en silencio.
        if (dto.Activo && dto.TasaAnual <= 0)
            return Respuesta<bool>.Invalido(
                "Indique la tasa anual antes de activar el financiamiento.");

        // Sin texto legal no se publica nada. La Ley 7472 obliga a
        // informar el costo total del credito, y ese descargo lo
        // redacta el abogado.
        if (dto.Activo && string.IsNullOrWhiteSpace(dto.TextoLegal))
            return Respuesta<bool>.Invalido(
                "Agregue el texto legal antes de activar el financiamiento.");

        var c = await _db.ConfiguracionFinanciamiento.FirstOrDefaultAsync(ct);

        if (c is null)
        {
            c = new ConfiguracionFinanciamiento();
            _db.ConfiguracionFinanciamiento.Add(c);
        }

        var antes = new
        {
            c.Activo,
            c.TasaAnual,
            c.PorcentajePrima,
            c.PlazosDisponibles
        };

        c.Activo = dto.Activo;
        c.PorcentajePrima = dto.PorcentajePrima;
        c.TasaAnual = dto.TasaAnual;
        c.PlazoMinimoMeses = dto.PlazoMinimoMeses;
        c.PlazoMaximoMeses = dto.PlazoMaximoMeses;
        c.PlazosDisponibles = string.Join(',', plazos);
        c.TextoLegal = dto.TextoLegal?.Trim();
        c.ActualizadoEn = DateTimeOffset.UtcNow;
        c.ActualizadoPorId = usuarioId;

        // Este cambio se audita con especial cuidado: la tasa tiene
        // implicaciones legales y hay que poder demostrar cual estaba
        // vigente en cada momento.
        _db.Registrar("ConfiguracionFinanciamiento", c.Id,
            AccionAuditoria.Editar, usuarioId,
            antes: antes,
            despues: new
            {
                c.Activo,
                c.TasaAnual,
                c.PorcentajePrima,
                c.PlazosDisponibles
            });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<SimulacionDto?>> SimularAsync(
        decimal precio, CancellationToken ct = default)
    {
        if (precio <= 0)
            return Respuesta<SimulacionDto?>.Invalido("El precio no es válido.");

        var c = await _db.ConfiguracionFinanciamiento
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);

        // Null y no un error: que el financiamiento este apagado es un
        // estado normal del sistema, no un fallo. El frontend
        // simplemente no muestra la seccion.
        if (c is null || !c.EstaOperativo)
            return Respuesta<SimulacionDto?>.Ok(null);

        var planes = CalculadoraFinanciamiento.CalcularTodos(precio, c).ToList();

        if (planes.Count == 0)
            return Respuesta<SimulacionDto?>.Ok(null);

        var simulacion = new SimulacionDto(
            PrecioVehiculo: precio,
            Prima: planes[0].Prima,
            Opciones: planes
                .Select(p => new CuotaDto(
                    p.PlazoMeses,
                    p.CuotaMensual,
                    p.TotalIntereses,
                    p.TotalAPagar,
                    p.TasaAnual))
                .ToList(),
            TextoLegal: c.TextoLegal);

        return Respuesta<SimulacionDto?>.Ok(simulacion);
    }

    /// <summary>
    /// "12, 24 , 36" → [12, 24, 36], sin repetidos y ordenados.
    /// </summary>
    private static List<short> ParsearPlazos(string texto) =>
        texto.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => short.TryParse(p.Trim(), out var v) ? v : (short)0)
            .Where(p => p > 0)
            .Distinct()
            .OrderBy(p => p)
            .ToList();
}

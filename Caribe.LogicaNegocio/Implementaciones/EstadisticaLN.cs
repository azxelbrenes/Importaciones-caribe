using Caribe.AccesoDatos.Contexto;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Dtos.Estadisticas;
using Caribe.LogicaNegocio.Interfaces;
using Caribe.Utilitarios;
using Microsoft.EntityFrameworkCore;

namespace Caribe.LogicaNegocio.Implementaciones;

public class EstadisticaLN : IEstadisticaLN
{
    private readonly CaribeContext _db;

    public EstadisticaLN(CaribeContext db) => _db = db;

    public async Task<Respuesta<ResumenDto>> ResumenAsync(CancellationToken ct = default)
    {
        var ahora = DateTimeOffset.UtcNow;

        var inicioMes = new DateTimeOffset(
            ahora.Year, ahora.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // ── Inventario ──
        // Un solo recorrido de la tabla en vez de tres consultas
        // separadas: con pocos cientos de vehiculos da igual, pero es
        // gratis hacerlo bien desde el principio.
        var porEstado = await _db.Vehiculos
            .AsNoTracking()
            .GroupBy(v => v.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync(ct);

        var publicados = porEstado
            .Where(x => x.Estado == EstadoVehiculo.Disponible)
            .Sum(x => x.Cantidad);

        var enTrato = porEstado
            .Where(x => x.Estado == EstadoVehiculo.EnTrato)
            .Sum(x => x.Cantidad);

        var enTransito = porEstado
            .Where(x => x.Estado == EstadoVehiculo.EnTransito)
            .Sum(x => x.Cantidad);

        // ── Ventas del mes ──
        var vendidos = await _db.Vehiculos
            .AsNoTracking()
            .Where(v => v.VendidoEn >= inicioMes)
            .Select(v => new
            {
                v.PrecioPublicado,
                v.Honorario,
                v.PublicadoEn,
                v.VendidoEn
            })
            .ToListAsync(ct);

        // El margen sale del honorario, no de la resta con el costo:
        // el honorario ES la ganancia, y usar la resta daria el mismo
        // numero pero dependeria de que los costos esten completos.
        var ingresos = vendidos.Sum(v => v.PrecioPublicado);
        var margen = vendidos.Sum(v => v.Honorario);

        var conFechas = vendidos
            .Where(v => v.PublicadoEn.HasValue && v.VendidoEn.HasValue)
            .ToList();

        var diasVenta = conFechas.Count == 0
            ? 0
            : conFechas.Average(v =>
                (v.VendidoEn!.Value - v.PublicadoEn!.Value).TotalDays);

        // ── Solicitudes ──
        var nuevas = await _db.Solicitudes
            .CountAsync(s => s.Estado == EstadoSolicitud.Nueva
                          && s.ArchivadaEn == null, ct);

        var solicitudesMes = await _db.Solicitudes
            .CountAsync(s => s.CreadoEn >= inicioMes, ct);

        // Tiempo de respuesta: solo sobre las que ya se atendieron.
        var tiempos = await _db.Solicitudes
            .AsNoTracking()
            .Where(s => s.CreadoEn >= inicioMes && s.AtendidaEn != null)
            .Select(s => new { s.CreadoEn, s.AtendidaEn })
            .ToListAsync(ct);

        var minutosRespuesta = tiempos.Count == 0
            ? 0
            : tiempos.Average(t =>
                (t.AtendidaEn!.Value - t.CreadoEn).TotalMinutes);

        // ── Visitas ──
        // Se cuentan sobre los vehiculos publicados este mes. No es un
        // contador de visitas del sitio: es interes acumulado sobre el
        // inventario, que es lo que le sirve al dueno.
        var visitas = await _db.Vehiculos
            .AsNoTracking()
            .Where(v => v.PublicadoEn >= inicioMes)
            .SumAsync(v => (int?)v.Visitas, ct) ?? 0;

        var dto = new ResumenDto(
            VehiculosPublicados: publicados,
            VehiculosEnTrato: enTrato,
            VehiculosEnTransito: enTransito,
            VendidosMes: vendidos.Count,
            SolicitudesNuevas: nuevas,
            SolicitudesMes: solicitudesMes,
            IngresosMes: ingresos,
            MargenMes: margen,
            VisitasMes: visitas,
            DiasPromedioVenta: Math.Round(diasVenta, 1),
            MinutosPromedioRespuesta: Math.Round(minutosRespuesta, 0));

        return Respuesta<ResumenDto>.Ok(dto);
    }

    public async Task<Respuesta<IEnumerable<VentaMesDto>>> VentasPorMesAsync(
        int meses = 6, CancellationToken ct = default)
    {
        if (meses is < 1 or > 36) meses = 6;

        var desde = DateTimeOffset.UtcNow
            .AddMonths(-(meses - 1))
            .Date;

        var inicio = new DateTimeOffset(
            desde.Year, desde.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var ventas = await _db.Vehiculos
            .AsNoTracking()
            .Where(v => v.VendidoEn >= inicio)
            .GroupBy(v => new { v.VendidoEn!.Value.Year, v.VendidoEn!.Value.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Cantidad = g.Count(),
                Ingresos = g.Sum(v => v.PrecioPublicado),
                Margen = g.Sum(v => v.Honorario)
            })
            .ToListAsync(ct);

        // Se rellenan los meses sin ventas con ceros. Sin esto, el
        // grafico del panel saltaria de marzo a junio y daria la
        // impresion de que faltan datos en vez de que no hubo ventas.
        var lista = new List<VentaMesDto>();

        for (var i = 0; i < meses; i++)
        {
            var fecha = inicio.AddMonths(i);

            var v = ventas.FirstOrDefault(x =>
                x.Year == fecha.Year && x.Month == fecha.Month);

            lista.Add(new VentaMesDto(
                (short)fecha.Year,
                (short)fecha.Month,
                v?.Cantidad ?? 0,
                v?.Ingresos ?? 0m,
                v?.Margen ?? 0m));
        }

        return Respuesta<IEnumerable<VentaMesDto>>.Ok(lista);
    }

    public async Task<Respuesta<IEnumerable<EtapaPipelineDto>>> PipelineAsync(
        CancellationToken ct = default)
    {
        // Las vivas, sin contar las archivadas.
        var vivas = await _db.Solicitudes
            .AsNoTracking()
            .Where(s => s.ArchivadaEn == null)
            .GroupBy(s => s.Estado)
            .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
            .ToListAsync(ct);

        // Y las que ya se purgaron. Sumarlas es lo que evita que la
        // conversion suba artificialmente: con 100 solicitudes y 20
        // cerradas la conversion real es 20%, pero si se borraron 50
        // descartadas y no se contaran, el panel mostraria 40%.
        var historicas = await _db.EstadisticasHistoricas
            .AsNoTracking()
            .GroupBy(h => 1)
            .Select(g => new
            {
                Nuevas = g.Sum(h => h.Nuevas),
                Contactadas = g.Sum(h => h.Contactadas),
                EnProceso = g.Sum(h => h.EnProceso),
                Cerradas = g.Sum(h => h.Cerradas),
                Descartadas = g.Sum(h => h.Descartadas)
            })
            .FirstOrDefaultAsync(ct);

        var lista = new List<EtapaPipelineDto>();

        // Archivada no aparece en el embudo: no es una etapa del
        // proceso de venta, es un estado de almacenamiento.
        EstadoSolicitud[] etapas =
        [
            EstadoSolicitud.Nueva,
            EstadoSolicitud.Contactada,
            EstadoSolicitud.EnProceso,
            EstadoSolicitud.Cerrada,
            EstadoSolicitud.Descartada
        ];

        foreach (var etapa in etapas)
        {
            var cantidad = vivas
                .Where(v => v.Estado == etapa)
                .Sum(v => v.Cantidad);

            cantidad += etapa switch
            {
                EstadoSolicitud.Nueva      => historicas?.Nuevas ?? 0,
                EstadoSolicitud.Contactada => historicas?.Contactadas ?? 0,
                EstadoSolicitud.EnProceso  => historicas?.EnProceso ?? 0,
                EstadoSolicitud.Cerrada    => historicas?.Cerradas ?? 0,
                EstadoSolicitud.Descartada => historicas?.Descartadas ?? 0,
                _ => 0
            };

            lista.Add(new EtapaPipelineDto(
                (short)etapa, Etiquetas.De(etapa), cantidad));
        }

        return Respuesta<IEnumerable<EtapaPipelineDto>>.Ok(lista);
    }

    public async Task<Respuesta<IEnumerable<MarcaSolicitadaDto>>> MarcasMasSolicitadasAsync(
        int top = 8, CancellationToken ct = default)
    {
        if (top is < 1 or > 50) top = 8;

        // Sobre el texto libre de las solicitudes, no sobre el catalogo:
        // lo interesante es lo que la gente PIDE, incluidas marcas que
        // el negocio todavia no trae. Eso le dice al dueno que buscar.
        var lista = await _db.Solicitudes
            .AsNoTracking()
            .Where(s => s.MarcaTexto != null && s.MarcaTexto != "")
            .GroupBy(s => s.MarcaTexto!.ToLower())
            .Select(g => new MarcaSolicitadaDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Cantidad)
            .Take(top)
            .ToListAsync(ct);

        return Respuesta<IEnumerable<MarcaSolicitadaDto>>.Ok(lista);
    }
}

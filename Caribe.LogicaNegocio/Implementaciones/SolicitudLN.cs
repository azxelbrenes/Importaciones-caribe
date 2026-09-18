using Caribe.AccesoDatos.Contexto;
using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Dtos.Solicitudes;
using Caribe.LogicaNegocio.Interfaces;
using Caribe.Utilitarios;
using Microsoft.EntityFrameworkCore;

namespace Caribe.LogicaNegocio.Implementaciones;

public class SolicitudLN : ISolicitudLN
{
    private readonly CaribeContext _db;

    public SolicitudLN(CaribeContext db) => _db = db;

    // ══════════════════ PUBLICO ══════════════════

    public async Task<Respuesta<int>> CrearAsync(
        CrearSolicitudDto dto, string? ip, CancellationToken ct = default)
    {
        // Ley 8968: sin consentimiento explicito no se pueden tratar
        // los datos. No es una casilla decorativa.
        if (!dto.Consentimiento)
            return Respuesta<int>.Invalido(
                "Debe aceptar el uso de sus datos para ser contactado.");

        var whatsapp = SoloDigitos(dto.Whatsapp);

        if (whatsapp.Length is < 8 or > 15)
            return Respuesta<int>.Invalido(
                "El número de WhatsApp debe tener entre 8 y 15 dígitos.");

        var nombre = dto.Nombre.Trim();

        if (nombre.Length < 3)
            return Respuesta<int>.Invalido("Escriba su nombre completo.");

        if (dto.PresupuestoMin.HasValue && dto.PresupuestoMax.HasValue
            && dto.PresupuestoMin > dto.PresupuestoMax)
            return Respuesta<int>.Invalido(
                "El presupuesto máximo debe ser mayor al mínimo.");

        // Anti duplicado suave: el mismo numero pidiendo lo mismo en
        // menos de cinco minutos casi siempre es un doble clic o un
        // reenvio del formulario, no dos consultas distintas.
        var hace5min = DateTimeOffset.UtcNow.AddMinutes(-5);

        var duplicada = await _db.Solicitudes.AnyAsync(s =>
            s.Whatsapp == whatsapp
            && s.CreadoEn > hace5min
            && s.ModeloTexto == dto.ModeloTexto, ct);

        if (duplicada)
            return Respuesta<int>.Conflicto(
                "Ya recibimos su solicitud. Le escribimos en breve.");

        var s = new Solicitud
        {
            Nombre = nombre,
            Whatsapp = whatsapp,
            MarcaTexto = dto.MarcaTexto?.Trim(),
            ModeloTexto = dto.ModeloTexto?.Trim(),
            AnioDesde = dto.AnioDesde,
            PresupuestoMin = dto.PresupuestoMin,
            PresupuestoMax = dto.PresupuestoMax,
            Transmision = dto.Transmision.HasValue
                ? (Transmision)dto.Transmision.Value : null,
            Combustible = dto.Combustible.HasValue
                ? (Combustible)dto.Combustible.Value : null,
            Detalles = dto.Detalles?.Trim(),
            FormaPago = (FormaPago)dto.FormaPago,
            PlazoMesesInteres = dto.PlazoMesesInteres,
            Origen = (OrigenSolicitud)dto.Origen,
            VehiculoId = dto.VehiculoId,
            Estado = EstadoSolicitud.Nueva,
            Consentimiento = true,
            IpOrigen = ip,
            CreadoEn = DateTimeOffset.UtcNow
        };

        _db.Solicitudes.Add(s);
        await _db.SaveChangesAsync(ct);

        // No se audita: la auditoria registra acciones del personal.
        // Una solicitud publica ya queda registrada por si misma.
        return Respuesta<int>.Ok(s.Id);
    }

    // ══════════════════ PANEL ══════════════════

    public async Task<Respuesta<Pagina<SolicitudDto>>> ListarAsync(
        FiltroSolicitudDto filtro, CancellationToken ct = default)
    {
        var consulta = _db.Solicitudes.AsNoTracking();

        // Las archivadas se ocultan por defecto: con los meses son la
        // mayoria y no se trabajan a diario.
        if (!filtro.IncluirArchivadas)
            consulta = consulta.Where(s => s.ArchivadaEn == null);

        if (filtro.Estado.HasValue)
            consulta = consulta.Where(s => s.Estado == (EstadoSolicitud)filtro.Estado.Value);

        if (!string.IsNullOrWhiteSpace(filtro.AsignadaAId))
            consulta = consulta.Where(s => s.AsignadaAId == filtro.AsignadaAId);

        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var texto = filtro.Busqueda.Trim().ToLower();

            consulta = consulta.Where(s =>
                s.Nombre.ToLower().Contains(texto) ||
                s.Whatsapp.Contains(texto) ||
                (s.MarcaTexto != null && s.MarcaTexto.ToLower().Contains(texto)) ||
                (s.ModeloTexto != null && s.ModeloTexto.ToLower().Contains(texto)));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            // Las nuevas primero, despues las mas recientes. Es el
            // orden de trabajo, no el cronologico puro.
            .OrderBy(s => s.Estado)
            .ThenByDescending(s => s.CreadoEn)
            .Skip(filtro.Saltar)
            .Take(filtro.PorPagina)
            .Select(s => new SolicitudDto(
                s.Id,
                s.Nombre,
                s.Whatsapp,
                s.MarcaTexto,
                s.ModeloTexto,
                s.AnioDesde,
                s.PresupuestoMin,
                s.PresupuestoMax,
                (short)s.FormaPago,
                Etiquetas.De(s.FormaPago),
                s.PlazoMesesInteres,
                (short)s.Origen,
                Etiquetas.De(s.Origen),
                (short)s.Estado,
                s.AsignadaAId,
                s.Vehiculo != null ? s.Vehiculo.Slug : null,
                s.CreadoEn,
                s.AtendidaEn,
                s.Notas.Count()))
            .ToListAsync(ct);

        return Respuesta<Pagina<SolicitudDto>>.Ok(
            Pagina<SolicitudDto>.Crear(items, filtro.Pagina, filtro.PorPagina, total));
    }

    public async Task<Respuesta<SolicitudDetalleDto>> BuscarPorIdAsync(
        int id, CancellationToken ct = default)
    {
        var s = await _db.Solicitudes
            .AsNoTracking()
            .Include(x => x.Vehiculo)
            .Include(x => x.Notas)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (s is null)
            return Respuesta<SolicitudDetalleDto>.NoEncontrado("La solicitud no existe.");

        var detalle = new SolicitudDetalleDto(
            s.Id,
            s.Nombre,
            s.Whatsapp,
            s.MarcaTexto,
            s.ModeloTexto,
            s.AnioDesde,
            s.PresupuestoMin,
            s.PresupuestoMax,
            s.Transmision.HasValue ? (short)s.Transmision.Value : null,
            s.Combustible.HasValue ? (short)s.Combustible.Value : null,
            s.Detalles,
            (short)s.FormaPago,
            Etiquetas.De(s.FormaPago),
            s.PlazoMesesInteres,
            (short)s.Origen,
            Etiquetas.De(s.Origen),
            (short)s.Estado,
            s.AsignadaAId,
            s.Vehiculo?.Slug,
            s.CreadoEn,
            s.AtendidaEn,
            s.Notas
                .OrderByDescending(n => n.CreadoEn)
                .Select(n => new NotaDto(n.Id, n.UsuarioId, n.Nota, n.CreadoEn))
                .ToList());

        return Respuesta<SolicitudDetalleDto>.Ok(detalle);
    }

    public async Task<Respuesta<bool>> ActualizarAsync(
        ActualizarSolicitudDto dto, string usuarioId, CancellationToken ct = default)
    {
        var s = await _db.Solicitudes.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

        if (s is null)
            return Respuesta<bool>.NoEncontrado("La solicitud no existe.");

        var nuevo = (EstadoSolicitud)dto.Estado;

        if (!Enum.IsDefined(nuevo))
            return Respuesta<bool>.Invalido("El estado indicado no es válido.");

        var anterior = s.Estado;

        s.Estado = nuevo;
        s.AsignadaAId = dto.AsignadaAId;

        // La primera vez que sale de Nueva queda registrado cuando se
        // atendio. Ese dato alimenta el tiempo de respuesta, que es la
        // metrica que mas correlaciona con cerrar la venta.
        if (anterior == EstadoSolicitud.Nueva
            && nuevo != EstadoSolicitud.Nueva
            && s.AtendidaEn is null)
            s.AtendidaEn = DateTimeOffset.UtcNow;

        _db.Registrar("Solicitud", s.Id, AccionAuditoria.CambioEstado, usuarioId,
            antes: new { Estado = Etiquetas.De(anterior) },
            despues: new { Estado = Etiquetas.De(nuevo) });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<int>> AgregarNotaAsync(
        CrearNotaDto dto, string usuarioId, CancellationToken ct = default)
    {
        var existe = await _db.Solicitudes.AnyAsync(s => s.Id == dto.SolicitudId, ct);

        if (!existe)
            return Respuesta<int>.NoEncontrado("La solicitud no existe.");

        var texto = dto.Nota.Trim();

        if (texto.Length < 2)
            return Respuesta<int>.Invalido("La nota está vacía.");

        var nota = new SolicitudNota
        {
            SolicitudId = dto.SolicitudId,
            UsuarioId = usuarioId,
            Nota = texto,
            CreadoEn = DateTimeOffset.UtcNow
        };

        _db.SolicitudNotas.Add(nota);
        await _db.SaveChangesAsync(ct);

        return Respuesta<int>.Ok(nota.Id);
    }

    // ══════════════════ LIMPIEZA ══════════════════

    public async Task<Respuesta<bool>> ArchivarAsync(
        int id, string usuarioId, CancellationToken ct = default)
    {
        var s = await _db.Solicitudes.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (s is null)
            return Respuesta<bool>.NoEncontrado("La solicitud no existe.");

        if (s.ArchivadaEn is not null)
            return Respuesta<bool>.Ok(true);

        // Archivar no borra: las solicitudes son historial comercial y
        // alimentan las estadisticas de conversion.
        s.ArchivadaEn = DateTimeOffset.UtcNow;
        s.Estado = EstadoSolicitud.Archivada;

        _db.Registrar("Solicitud", s.Id, AccionAuditoria.Editar, usuarioId,
            despues: new { Accion = "Archivada" });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<int>> ArchivarCerradasAsync(
        int mesesAntiguedad, string usuarioId, CancellationToken ct = default)
    {
        if (mesesAntiguedad < 1)
            return Respuesta<int>.Invalido("La antigüedad mínima es de un mes.");

        var corte = DateTimeOffset.UtcNow.AddMonths(-mesesAntiguedad);
        var ahora = DateTimeOffset.UtcNow;

        // Solo cerradas y descartadas: una En proceso de hace seis
        // meses sigue siendo trabajo pendiente, no basura.
        var archivadas = await _db.Solicitudes
            .Where(s => s.ArchivadaEn == null
                     && s.CreadoEn < corte
                     && (s.Estado == EstadoSolicitud.Cerrada
                      || s.Estado == EstadoSolicitud.Descartada))
            .ExecuteUpdateAsync(set => set
                .SetProperty(s => s.ArchivadaEn, ahora)
                .SetProperty(s => s.Estado, EstadoSolicitud.Archivada), ct);

        if (archivadas > 0)
        {
            _db.Registrar("Solicitud", 0, AccionAuditoria.Editar, usuarioId,
                despues: new { Accion = "Archivado en lote", Cantidad = archivadas });

            await _db.SaveChangesAsync(ct);
        }

        return Respuesta<int>.Ok(archivadas);
    }

    public async Task<Respuesta<bool>> EliminarAsync(
        int id, string usuarioId, CancellationToken ct = default)
    {
        var s = await _db.Solicitudes.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (s is null)
            return Respuesta<bool>.NoEncontrado("La solicitud no existe.");

        // Solo se borran las descartadas: spam, bots y duplicados
        // obvios. Una solicitud real cerrada es el registro de una
        // venta y no se toca.
        if (s.Estado is not (EstadoSolicitud.Descartada or EstadoSolicitud.Archivada))
            return Respuesta<bool>.Conflicto(
                "Solo se pueden eliminar solicitudes descartadas o archivadas. " +
                "Descártela primero si es spam.");

        // Antes de borrar se acumula el conteo del mes. Sin esto, la
        // tasa de conversion subiria artificialmente: con 100
        // solicitudes y 20 cerradas la conversion real es 20%, pero
        // borrando 50 descartadas el panel mostraria 40%.
        await AcumularEnHistoricoAsync(s, ct);

        _db.Registrar("Solicitud", s.Id, AccionAuditoria.Eliminar, usuarioId,
            antes: new { s.Nombre, s.Whatsapp, Estado = Etiquetas.De(s.Estado) });

        _db.Solicitudes.Remove(s);
        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    // ══════════════════ PRIVADOS ══════════════════

    /// <summary>
    /// Suma la solicitud al conteo historico de su mes antes de
    /// borrarla, para que las estadisticas no se distorsionen.
    /// </summary>
    private async Task AcumularEnHistoricoAsync(Solicitud s, CancellationToken ct)
    {
        var anio = (short)s.CreadoEn.Year;
        var mes = (short)s.CreadoEn.Month;

        var historico = await _db.EstadisticasHistoricas
            .FirstOrDefaultAsync(h => h.Anio == anio && h.Mes == mes, ct);

        if (historico is null)
        {
            historico = new EstadisticaHistorica
            {
                Anio = anio,
                Mes = mes,
                ActualizadoEn = DateTimeOffset.UtcNow
            };

            _db.EstadisticasHistoricas.Add(historico);
        }

        switch (s.Estado)
        {
            case EstadoSolicitud.Nueva:      historico.Nuevas++; break;
            case EstadoSolicitud.Contactada: historico.Contactadas++; break;
            case EstadoSolicitud.EnProceso:  historico.EnProceso++; break;
            case EstadoSolicitud.Cerrada:    historico.Cerradas++; break;
            case EstadoSolicitud.Descartada: historico.Descartadas++; break;
            case EstadoSolicitud.Archivada:  historico.Descartadas++; break;
        }

        historico.Purgadas++;
        historico.ActualizadoEn = DateTimeOffset.UtcNow;
    }

    private static string SoloDigitos(string texto) =>
        new(texto.Where(char.IsDigit).ToArray());
}

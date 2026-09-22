using Caribe.AccesoDatos.Contexto;
using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Comunes;
using Caribe.LogicaNegocio.Dtos.Vehiculos;
using Caribe.LogicaNegocio.Interfaces;
using Caribe.Utilitarios;
using Microsoft.EntityFrameworkCore;

namespace Caribe.LogicaNegocio.Implementaciones;

public class VehiculoLN : IVehiculoLN
{
    private readonly CaribeContext _db;

    public VehiculoLN(CaribeContext db) => _db = db;

    // ══════════════════ PUBLICO ══════════════════

    public async Task<Respuesta<Pagina<VehiculoDto>>> ListarPublicoAsync(
        FiltroVehiculoDto filtro, CancellationToken ct = default)
    {
        // AsNoTracking porque nada de esto se va a modificar: EF no
        // necesita guardar copias para detectar cambios.
        var consulta = _db.Vehiculos
            .AsNoTracking()
            .Where(v => v.Estado == EstadoVehiculo.Disponible
                     || v.Estado == EstadoVehiculo.EnTrato)
            .AplicarFiltros(filtro);

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .Ordenar(filtro.OrdenarPor)
            .Skip(filtro.Saltar)
            .Take(filtro.PorPagina)
            // La proyeccion trae SOLO estas columnas. Sin ella, EF
            // pediria la tabla completa —costos incluidos— y despues
            // los descartaria en memoria.
            .Select(v => new VehiculoDto(
                v.Slug,
                v.Marca.Nombre,
                v.Modelo.Nombre,
                v.Anio,
                v.Kilometraje,
                v.Color,
                v.PrecioPublicado,
                (short)v.Estado,
                v.Destacado,
                v.AceptaFinanciamiento,
                v.Fotos
                    .Where(f => f.EsPortada)
                    .Select(f => f.UrlThumb)
                    .FirstOrDefault()))
            .ToListAsync(ct);

        return Respuesta<Pagina<VehiculoDto>>.Ok(
            Pagina<VehiculoDto>.Crear(items, filtro.Pagina, filtro.PorPagina, total));
    }

    public async Task<Respuesta<VehiculoDetalleDto>> BuscarPorSlugAsync(
        string slug, CancellationToken ct = default)
    {
        var v = await _db.Vehiculos
            .AsNoTracking()
            .Include(x => x.Marca)
            .Include(x => x.Modelo)
            .Include(x => x.Fotos)
            .FirstOrDefaultAsync(x => x.Slug == slug, ct);

        if (v is null || !v.EsVisibleAlPublico)
            return Respuesta<VehiculoDetalleDto>.NoEncontrado(
                "Este vehículo ya no está disponible.");

        // El contador se incrementa sin cargar la entidad ni disparar
        // SaveChanges: es un UPDATE directo. Con muchas visitas
        // simultaneas, cargar y guardar produciria conflictos.
        await _db.Vehiculos
            .Where(x => x.Id == v.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(
                x => x.Visitas, x => x.Visitas + 1), ct);

        // El financiamiento aparece solo si el vehiculo lo acepta Y la
        // configuracion esta activa. No se publican tasas ni cuotas:
        // la prima y los plazos, y el interes lo da el dueno por
        // WhatsApp.
        FinanciamientoVehiculoDto? financiamiento = null;

        if (v.AceptaFinanciamiento)
        {
            var config = await _db.ConfiguracionFinanciamiento
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);

            if (config is not null && config.Activo)
            {
                var plazos = config.Plazos().ToList();

                if (plazos.Count > 0)
                {
                    financiamiento = new FinanciamientoVehiculoDto(
                        config.PorcentajePrima,
                        Math.Round(v.PrecioPublicado * config.PorcentajePrima / 100m, 2,
                            MidpointRounding.AwayFromZero),
                        plazos);
                }
            }
        }

        var detalle = new VehiculoDetalleDto(
            v.Id,
            v.Slug,
            v.Marca.Nombre,
            v.Modelo.Nombre,
            v.Anio,
            v.Kilometraje,
            Etiquetas.De(v.Transmision),
            Etiquetas.De(v.Combustible),
            Etiquetas.De(v.Traccion),
            v.Color,
            v.Descripcion,
            v.PrecioPublicado,
            (short)v.Estado,
            // El honorario va sumado dentro de Tramites: mostrarlo
            // aparte le diria a la competencia cuanto gana el negocio.
            new DesglosePrecioDto(
                v.CostoVehiculo,
                v.CostoFlete,
                v.CostoImpuestos,
                v.CostoTramites + v.Honorario,
                v.PrecioPublicado,
                v.VigenciaDias),
            v.Fotos
                .OrderByDescending(f => f.EsPortada)
                .ThenBy(f => f.Orden)
                .Select(f => new FotoDto(f.Url, f.UrlThumb, f.Orden, f.EsPortada))
                .ToList(),
            financiamiento);

        return Respuesta<VehiculoDetalleDto>.Ok(detalle);
    }

    public async Task<Respuesta<VehiculoDto?>> DestacadoAsync(
        CancellationToken ct = default)
    {
        // Destacado primero; si no hay ninguno, el mas reciente. La
        // portada del sitio nunca queda vacia teniendo publicados.
        var v = await _db.Vehiculos
            .AsNoTracking()
            .Where(x => x.Estado == EstadoVehiculo.Disponible
                     || x.Estado == EstadoVehiculo.EnTrato)
            .OrderByDescending(x => x.Destacado)
            .ThenByDescending(x => x.PublicadoEn ?? x.CreadoEn)
            .Select(x => new VehiculoDto(
                x.Slug,
                x.Marca.Nombre,
                x.Modelo.Nombre,
                x.Anio,
                x.Kilometraje,
                x.Color,
                x.PrecioPublicado,
                (short)x.Estado,
                x.Destacado,
                x.AceptaFinanciamiento,
                x.Fotos
                    .Where(f => f.EsPortada)
                    .Select(f => f.UrlThumb)
                    .FirstOrDefault()))
            .FirstOrDefaultAsync(ct);

        return Respuesta<VehiculoDto?>.Ok(v);
    }

    // ══════════════════ PANEL ══════════════════

    public async Task<Respuesta<Pagina<VehiculoAdminDto>>> ListarAdminAsync(
        FiltroVehiculoDto filtro, CancellationToken ct = default)
    {
        var consulta = _db.Vehiculos
            .AsNoTracking()
            .AplicarFiltros(filtro);

        if (filtro.Estado.HasValue)
            consulta = consulta.Where(v => v.Estado == (EstadoVehiculo)filtro.Estado.Value);

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .Ordenar(filtro.OrdenarPor)
            .Skip(filtro.Saltar)
            .Take(filtro.PorPagina)
            .Select(v => new VehiculoAdminDto(
                v.Id,
                v.Slug,
                v.Marca.Nombre,
                v.Modelo.Nombre,
                v.Anio,
                v.Kilometraje,
                v.Color,
                v.CostoVehiculo + v.CostoFlete + v.CostoImpuestos + v.CostoTramites,
                v.Honorario,
                v.PrecioPublicado,
                v.PrecioPublicado - (v.CostoVehiculo + v.CostoFlete
                                   + v.CostoImpuestos + v.CostoTramites),
                (short)v.Estado,
                v.Destacado,
                v.AceptaFinanciamiento,
                v.Visitas,
                // Se cuenta en la base, no trayendo las fotos. Sin
                // esto el panel no puede avisar cuales no se pueden
                // publicar.
                v.Fotos.Count(),
                v.CreadoEn,
                v.PublicadoEn))
            .ToListAsync(ct);

        return Respuesta<Pagina<VehiculoAdminDto>>.Ok(
            Pagina<VehiculoAdminDto>.Crear(items, filtro.Pagina, filtro.PorPagina, total));
    }

    public async Task<Respuesta<VehiculoDetalleAdminDto>> BuscarPorIdAsync(
        int id, CancellationToken ct = default)
    {
        var v = await _db.Vehiculos
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new VehiculoDetalleAdminDto(
                x.Id, x.Slug, x.MarcaId, x.ModeloId, x.Anio, x.Kilometraje,
                (short)x.Transmision, (short)x.Combustible, (short)x.Traccion,
                x.Color, x.Descripcion,
                x.CostoVehiculo, x.CostoFlete, x.CostoImpuestos, x.CostoTramites,
                x.Honorario, x.PrecioPublicado, x.VigenciaDias,
                (short)x.Estado, x.Destacado, x.AceptaFinanciamiento))
            .FirstOrDefaultAsync(ct);

        return v is null
            ? Respuesta<VehiculoDetalleAdminDto>.NoEncontrado("El vehículo no existe.")
            : Respuesta<VehiculoDetalleAdminDto>.Ok(v);
    }

    public async Task<Respuesta<int>> CrearAsync(
        CrearVehiculoDto dto, string usuarioId, CancellationToken ct = default)
    {
        var modelo = await _db.Modelos
            .AsNoTracking()
            .Include(m => m.Marca)
            .FirstOrDefaultAsync(m => m.Id == dto.ModeloId, ct);

        if (modelo is null)
            return Respuesta<int>.Invalido("El modelo seleccionado no existe.");

        // El modelo debe pertenecer a la marca enviada: sin esta
        // comprobacion se podria crear un "Toyota Civic".
        if (modelo.MarcaId != dto.MarcaId)
            return Respuesta<int>.Invalido(
                "El modelo no corresponde a la marca seleccionada.");

        var ahora = DateTimeOffset.UtcNow;

        var v = new Vehiculo
        {
            MarcaId = dto.MarcaId,
            ModeloId = dto.ModeloId,
            Anio = dto.Anio,
            Kilometraje = dto.Kilometraje,
            Transmision = (Transmision)dto.Transmision,
            Combustible = (Combustible)dto.Combustible,
            Traccion = (Traccion)dto.Traccion,
            Color = dto.Color?.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            CostoVehiculo = dto.CostoVehiculo,
            CostoFlete = dto.CostoFlete,
            CostoImpuestos = dto.CostoImpuestos,
            CostoTramites = dto.CostoTramites,
            Honorario = dto.Honorario,
            VigenciaDias = dto.VigenciaDias,
            Destacado = dto.Destacado,
            AceptaFinanciamiento = dto.AceptaFinanciamiento,
            Estado = EstadoVehiculo.Borrador,
            CreadoEn = ahora,
            ActualizadoEn = ahora,
            CreadoPorId = usuarioId
        };

        // El precio SIEMPRE lo calcula el servidor. Nunca llega del
        // cliente: si viniera del navegador, cualquiera podria
        // publicar un vehiculo a un dolar.
        v.PrecioPublicado = v.CostoTotal + v.Honorario;

        v.Slug = await GenerarSlugUnicoAsync(
            modelo.Marca.Nombre, modelo.Nombre, dto.Anio, dto.Color, ct);

        _db.Vehiculos.Add(v);

        _db.Registrar("Vehiculo", 0, AccionAuditoria.Crear, usuarioId,
            despues: new { v.Slug, v.PrecioPublicado, v.Honorario });

        await _db.SaveChangesAsync(ct);

        return Respuesta<int>.Ok(v.Id);
    }

    public async Task<Respuesta<bool>> ActualizarAsync(
        ActualizarVehiculoDto dto, string usuarioId, CancellationToken ct = default)
    {
        var v = await _db.Vehiculos.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

        if (v is null)
            return Respuesta<bool>.NoEncontrado("El vehículo no existe.");

        // Un vendido no se edita: cambiar su precio descuadraria los
        // ingresos de un mes ya cerrado.
        if (v.Estado == EstadoVehiculo.Vendido)
            return Respuesta<bool>.Conflicto(
                "No se puede editar un vehículo vendido. Archívelo si necesita corregirlo.");

        var modelo = await _db.Modelos
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.ModeloId, ct);

        if (modelo is null || modelo.MarcaId != dto.MarcaId)
            return Respuesta<bool>.Invalido(
                "El modelo no corresponde a la marca seleccionada.");

        var antes = new { v.PrecioPublicado, v.Honorario, Costo = v.CostoTotal };

        v.MarcaId = dto.MarcaId;
        v.ModeloId = dto.ModeloId;
        v.Anio = dto.Anio;
        v.Kilometraje = dto.Kilometraje;
        v.Transmision = (Transmision)dto.Transmision;
        v.Combustible = (Combustible)dto.Combustible;
        v.Traccion = (Traccion)dto.Traccion;
        v.Color = dto.Color?.Trim();
        v.Descripcion = dto.Descripcion?.Trim();
        v.CostoVehiculo = dto.CostoVehiculo;
        v.CostoFlete = dto.CostoFlete;
        v.CostoImpuestos = dto.CostoImpuestos;
        v.CostoTramites = dto.CostoTramites;
        v.Honorario = dto.Honorario;
        v.VigenciaDias = dto.VigenciaDias;
        v.Destacado = dto.Destacado;
        v.AceptaFinanciamiento = dto.AceptaFinanciamiento;
        v.ActualizadoEn = DateTimeOffset.UtcNow;

        v.PrecioPublicado = v.CostoTotal + v.Honorario;

        // El slug NO se regenera al editar: cambiarlo romperia los
        // enlaces que los clientes ya compartieron por WhatsApp.
        _db.Registrar("Vehiculo", v.Id, AccionAuditoria.Editar, usuarioId,
            antes: antes,
            despues: new { v.PrecioPublicado, v.Honorario, Costo = v.CostoTotal });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> CambiarEstadoAsync(
        CambiarEstadoDto dto, string usuarioId, CancellationToken ct = default)
    {
        var v = await _db.Vehiculos.FirstOrDefaultAsync(x => x.Id == dto.Id, ct);

        if (v is null)
            return Respuesta<bool>.NoEncontrado("El vehículo no existe.");

        var nuevo = (EstadoVehiculo)dto.NuevoEstado;

        if (!TransicionEstado.EsValida(v.Estado, nuevo))
            return Respuesta<bool>.Conflicto(
                $"No se puede pasar de {Etiquetas.De(v.Estado)} a {Etiquetas.De(nuevo)}.");

        // Un vehiculo publicado sin fotos muestra una tarjeta vacia en
        // el catalogo, y eso dana la credibilidad del sitio.
        if (nuevo is EstadoVehiculo.Disponible or EstadoVehiculo.EnTrato)
        {
            var tieneFotos = await _db.VehiculoFotos
                .AnyAsync(f => f.VehiculoId == v.Id, ct);

            if (!tieneFotos)
                return Respuesta<bool>.Conflicto(
                    "Agregue al menos una fotografía antes de publicar.");
        }

        var anterior = v.Estado;
        v.Estado = nuevo;
        v.ActualizadoEn = DateTimeOffset.UtcNow;

        // La primera publicacion fija la fecha; las siguientes no la
        // mueven, para que el orden del catalogo sea estable.
        if (nuevo == EstadoVehiculo.Disponible && v.PublicadoEn is null)
            v.PublicadoEn = DateTimeOffset.UtcNow;

        if (nuevo == EstadoVehiculo.Vendido)
        {
            v.VendidoEn = DateTimeOffset.UtcNow;

            // Un vendido no puede seguir destacado en la portada.
            v.Destacado = false;
        }

        _db.Registrar("Vehiculo", v.Id, AccionAuditoria.CambioEstado, usuarioId,
            antes: new { Estado = Etiquetas.De(anterior) },
            despues: new { Estado = Etiquetas.De(nuevo) });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> EliminarAsync(
        int id, string usuarioId, CancellationToken ct = default)
    {
        var v = await _db.Vehiculos.FirstOrDefaultAsync(x => x.Id == id, ct);

        if (v is null)
            return Respuesta<bool>.NoEncontrado("El vehículo no existe.");

        // Un vendido nunca se borra: es el registro de una operacion
        // real y sostiene los reportes de ingresos.
        if (v.Estado == EstadoVehiculo.Vendido)
            return Respuesta<bool>.Conflicto(
                "Un vehículo vendido no se elimina. Archívelo para sacarlo del catálogo.");

        _db.Registrar("Vehiculo", v.Id, AccionAuditoria.Eliminar, usuarioId,
            antes: new { v.Slug, v.PrecioPublicado });

        // Las fotos se borran en cascada por la configuracion del
        // contexto. Los archivos del almacenamiento los limpia FotoLN.
        _db.Vehiculos.Remove(v);

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    // ══════════════════ PRIVADOS ══════════════════

    /// <summary>
    /// Genera el slug y le agrega un sufijo si ya existe. Dos Tacoma
    /// 2023 blancos son posibles, y las URL no pueden repetirse.
    /// </summary>
    private async Task<string> GenerarSlugUnicoAsync(
        string marca, string modelo, short anio, string? color, CancellationToken ct)
    {
        var baseSlug = Slug.Generar(marca, modelo, anio.ToString(), color);

        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = $"vehiculo-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";

        var candidato = baseSlug;
        var numero = 1;

        while (await _db.Vehiculos.AnyAsync(v => v.Slug == candidato, ct))
        {
            numero++;
            candidato = Slug.ConSufijo(baseSlug, numero);
        }

        return candidato;
    }
}

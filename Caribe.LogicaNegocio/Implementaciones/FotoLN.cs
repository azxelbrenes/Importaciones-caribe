using Caribe.AccesoDatos.Contexto;
using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Almacenamiento;
using Caribe.LogicaNegocio.Dtos.Vehiculos;
using Caribe.LogicaNegocio.Interfaces;
using Caribe.Utilitarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Caribe.LogicaNegocio.Implementaciones;

public class FotoLN : IFotoLN
{
    private readonly CaribeContext _db;
    private readonly IAlmacenamiento _almacen;
    private readonly AlmacenamientoOpciones _op;
    private readonly ILogger<FotoLN> _logger;

    public FotoLN(
        CaribeContext db,
        IAlmacenamiento almacen,
        IOptions<AlmacenamientoOpciones> op,
        ILogger<FotoLN> logger)
        => (_db, _almacen, _op, _logger) = (db, almacen, op.Value, logger);

    public async Task<Respuesta<FotoSubidaDto>> SubirAsync(
        int vehiculoId, Stream contenido, string nombreOriginal,
        string usuarioId, CancellationToken ct = default)
    {
        var existe = await _db.Vehiculos.AnyAsync(v => v.Id == vehiculoId, ct);

        if (!existe)
            return Respuesta<FotoSubidaDto>.NoEncontrado("El vehículo no existe.");

        if (contenido.Length == 0)
            return Respuesta<FotoSubidaDto>.Invalido("El archivo está vacío.");

        var maxBytes = _op.MaxMegabytesPorArchivo * 1024L * 1024L;

        if (contenido.Length > maxBytes)
            return Respuesta<FotoSubidaDto>.Invalido(
                $"El archivo supera los {_op.MaxMegabytesPorArchivo} MB permitidos.");

        // Se valida por la firma binaria, no por la extension: el
        // nombre lo controla el cliente.
        if (!ProcesadorImagen.EsImagenValida(contenido))
            return Respuesta<FotoSubidaDto>.Invalido(
                "El archivo no es una imagen válida. Use JPG, PNG, WebP o HEIC.");

        var cantidad = await _db.VehiculoFotos
            .CountAsync(f => f.VehiculoId == vehiculoId, ct);

        if (cantidad >= _op.MaxFotosPorVehiculo)
            return Respuesta<FotoSubidaDto>.Conflicto(
                $"El vehículo ya tiene el máximo de {_op.MaxFotosPorVehiculo} fotografías.");

        MemoryStream principal, thumb;

        try
        {
            (principal, thumb) = ProcesadorImagen.Procesar(contenido);
        }
        catch (Exception ex)
        {
            // Firma valida pero el procesamiento fallo: es error del
            // archivo, no del sistema, asi que no debe subir al
            // middleware como 500.
            _logger.LogWarning(ex,
                "No se pudo procesar {Archivo} del vehiculo {Id}",
                nombreOriginal, vehiculoId);

            // Si es HEIC, el motivo mas probable es que el servidor no
            // tenga el decodificador. Ahi el mensaje tiene que decirle
            // a la persona QUE HACER, no solo que fallo: cambiar el
            // ajuste del iPhone resuelve el problema para siempre.
            if (ProcesadorImagen.EsHeic(contenido))
                return Respuesta<FotoSubidaDto>.Invalido(
                    "No pudimos convertir esta foto del iPhone. En el teléfono, entrá a " +
                    "Ajustes → Cámara → Formatos y elegí \"Más compatible\": las fotos " +
                    "nuevas se guardan en un formato que siempre funciona.");

            return Respuesta<FotoSubidaDto>.Invalido(
                "No se pudo procesar la imagen. Puede estar dañada.");
        }

        // Nombre unico: evita colisiones y permite cachear por un ano,
        // porque una URL nunca va a apuntar a otro contenido.
        var id = Guid.NewGuid().ToString("N")[..16];
        var rutaPrincipal = $"vehiculos/{vehiculoId}/{id}.webp";
        var rutaThumb     = $"vehiculos/{vehiculoId}/{id}_thumb.webp";

        string url, urlThumb;

        try
        {
            await using (principal)
            await using (thumb)
            {
                url      = await _almacen.GuardarAsync(principal, rutaPrincipal, "image/webp", ct);
                urlThumb = await _almacen.GuardarAsync(thumb, rutaThumb, "image/webp", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Fallo al guardar la imagen del vehiculo {Id} en el almacenamiento",
                vehiculoId);

            return Respuesta<FotoSubidaDto>.Conflicto(
                "No se pudo guardar la fotografía. Intente de nuevo.");
        }

        var foto = new VehiculoFoto
        {
            VehiculoId = vehiculoId,
            Url = url,
            UrlThumb = urlThumb,
            Orden = (short)cantidad,

            // La primera foto es la portada automaticamente: asi nunca
            // queda un vehiculo publicado sin imagen en el catalogo.
            EsPortada = cantidad == 0,
            CreadoEn = DateTimeOffset.UtcNow
        };

        _db.VehiculoFotos.Add(foto);

        _db.Registrar("VehiculoFoto", vehiculoId, AccionAuditoria.Crear, usuarioId,
            despues: new { foto.Url, foto.EsPortada });

        await _db.SaveChangesAsync(ct);

        return Respuesta<FotoSubidaDto>.Ok(new FotoSubidaDto(
            foto.Id, foto.Url, foto.UrlThumb, foto.Orden, foto.EsPortada));
    }

    public async Task<Respuesta<IEnumerable<FotoSubidaDto>>> ListarAsync(
        int vehiculoId, CancellationToken ct = default)
    {
        var lista = await _db.VehiculoFotos
            .AsNoTracking()
            .Where(f => f.VehiculoId == vehiculoId)
            .OrderByDescending(f => f.EsPortada)
            .ThenBy(f => f.Orden)
            .Select(f => new FotoSubidaDto(
                f.Id, f.Url, f.UrlThumb, f.Orden, f.EsPortada))
            .ToListAsync(ct);

        return Respuesta<IEnumerable<FotoSubidaDto>>.Ok(lista);
    }

    public async Task<Respuesta<bool>> EliminarAsync(
        int fotoId, string usuarioId, CancellationToken ct = default)
    {
        var foto = await _db.VehiculoFotos.FirstOrDefaultAsync(f => f.Id == fotoId, ct);

        if (foto is null)
            return Respuesta<bool>.NoEncontrado("La fotografía no existe.");

        var vehiculo = await _db.Vehiculos
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == foto.VehiculoId, ct);

        // Un vehiculo publicado sin fotos mostraria una tarjeta vacia
        // en el catalogo. Misma regla que impide publicarlo sin
        // imagenes, aplicada al borrado.
        if (vehiculo is not null && vehiculo.EsVisibleAlPublico)
        {
            var restantes = await _db.VehiculoFotos
                .CountAsync(f => f.VehiculoId == foto.VehiculoId, ct);

            if (restantes <= 1)
                return Respuesta<bool>.Conflicto(
                    "No se puede quedar sin fotografías un vehículo publicado. " +
                    "Pásalo a borrador o agrega otra imagen primero.");
        }

        var eraPortada = foto.EsPortada;
        var vehId = foto.VehiculoId;

        await BorrarDelAlmacenAsync(foto, ct);

        _db.VehiculoFotos.Remove(foto);

        _db.Registrar("VehiculoFoto", vehId, AccionAuditoria.Eliminar, usuarioId,
            antes: new { foto.Url });

        await _db.SaveChangesAsync(ct);

        // Si se borro la portada, la siguiente toma su lugar. Sin esto
        // el vehiculo quedaria con fotos pero sin imagen en el catalogo.
        if (eraPortada)
        {
            var siguiente = await _db.VehiculoFotos
                .Where(f => f.VehiculoId == vehId)
                .OrderBy(f => f.Orden)
                .FirstOrDefaultAsync(ct);

            if (siguiente is not null)
            {
                siguiente.EsPortada = true;
                await _db.SaveChangesAsync(ct);
            }
        }

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> MarcarPortadaAsync(
        int fotoId, string usuarioId, CancellationToken ct = default)
    {
        var foto = await _db.VehiculoFotos.FirstOrDefaultAsync(f => f.Id == fotoId, ct);

        if (foto is null)
            return Respuesta<bool>.NoEncontrado("La fotografía no existe.");

        if (foto.EsPortada)
            return Respuesta<bool>.Ok(true);

        // Se quita la portada anterior ANTES de poner la nueva: hay un
        // indice unico parcial que impide dos portadas en el mismo
        // vehiculo, y el orden inverso lo violaria.
        await _db.VehiculoFotos
            .Where(f => f.VehiculoId == foto.VehiculoId && f.EsPortada)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.EsPortada, false), ct);

        foto.EsPortada = true;

        _db.Registrar("VehiculoFoto", foto.VehiculoId, AccionAuditoria.Editar, usuarioId,
            despues: new { Portada = foto.Url });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> ReordenarAsync(
        ReordenarFotosDto dto, string usuarioId, CancellationToken ct = default)
    {
        var fotos = await _db.VehiculoFotos
            .Where(f => f.VehiculoId == dto.VehiculoId)
            .ToListAsync(ct);

        if (fotos.Count == 0)
            return Respuesta<bool>.NoEncontrado("El vehículo no tiene fotografías.");

        // Todos los ids enviados deben pertenecer a este vehiculo: sin
        // esta comprobacion se podrian reordenar fotos de otro registro
        // mandando ids ajenos.
        var idsValidos = fotos.Select(f => f.Id).ToHashSet();

        if (!dto.IdsEnOrden.All(idsValidos.Contains))
            return Respuesta<bool>.Invalido(
                "La lista contiene fotografías que no pertenecen a este vehículo.");

        for (var i = 0; i < dto.IdsEnOrden.Count; i++)
        {
            var f = fotos.First(x => x.Id == dto.IdsEnOrden[i]);
            f.Orden = (short)i;
        }

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    /// <summary>
    /// Borra del almacenamiento sin dejar caer la operacion si falla.
    ///
    /// Un archivo huerfano ocupa unos KB; una excepcion aqui dejaria
    /// el registro en la base apuntando a algo que ya no se puede
    /// borrar, que es peor.
    /// </summary>
    private async Task BorrarDelAlmacenAsync(VehiculoFoto foto, CancellationToken ct)
    {
        try
        {
            await _almacen.EliminarAsync(_almacen.RutaDesdeUrl(foto.Url), ct);
            await _almacen.EliminarAsync(_almacen.RutaDesdeUrl(foto.UrlThumb), ct);
        }
        catch (Exception ex)
        {
            // Se registra para poder limpiarlo despues, pero no se
            // propaga: el registro en la base si tiene que irse.
            _logger.LogWarning(ex,
                "No se pudo borrar del almacenamiento la foto {Id}: {Url}",
                foto.Id, foto.Url);
        }
    }
}

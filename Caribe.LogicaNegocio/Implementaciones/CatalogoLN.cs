using Caribe.AccesoDatos.Contexto;
using Caribe.Dominio.Entidades;
using Caribe.Dominio.Enums;
using Caribe.LogicaNegocio.Dtos.Catalogos;
using Caribe.LogicaNegocio.Interfaces;
using Caribe.Utilitarios;
using Microsoft.EntityFrameworkCore;

namespace Caribe.LogicaNegocio.Implementaciones;

public class CatalogoLN : ICatalogoLN
{
    private readonly CaribeContext _db;

    public CatalogoLN(CaribeContext db) => _db = db;

    // ══════════════════ LECTURA ══════════════════

    public async Task<Respuesta<IEnumerable<MarcaDto>>> ListarMarcasAsync(
        bool soloActivas = true, CancellationToken ct = default)
    {
        var consulta = _db.Marcas.AsNoTracking();

        if (soloActivas)
            consulta = consulta.Where(m => m.Activa);

        var lista = await consulta
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaDto(
                m.Id,
                m.Nombre,
                m.Activa,
                m.Modelos.Count(mo => mo.Activo),
                // Cuantos vehiculos la usan. Con esto el panel sabe si
                // ofrecer eliminar o solo desactivar, sin tener que
                // preguntarlo en otra llamada.
                _db.Vehiculos.Count(v => v.MarcaId == m.Id)))
            .ToListAsync(ct);

        return Respuesta<IEnumerable<MarcaDto>>.Ok(lista);
    }

    public async Task<Respuesta<IEnumerable<ModeloDto>>> ListarModelosAsync(
        int? marcaId, CancellationToken ct = default)
    {
        var consulta = _db.Modelos.AsNoTracking();

        if (marcaId is > 0)
            consulta = consulta.Where(m => m.MarcaId == marcaId);

        var lista = await consulta
            .OrderBy(m => m.Marca.Nombre)
            .ThenBy(m => m.Nombre)
            .Select(m => new ModeloDto(
                m.Id, m.MarcaId, m.Marca.Nombre, m.Nombre, m.Activo,
                _db.Vehiculos.Count(v => v.ModeloId == m.Id)))
            .ToListAsync(ct);

        return Respuesta<IEnumerable<ModeloDto>>.Ok(lista);
    }

    // ══════════════════ CREAR ══════════════════

    public async Task<Respuesta<int>> CrearMarcaAsync(
        CrearMarcaDto dto, string usuarioId, CancellationToken ct = default)
    {
        var nombre = Normalizar(dto.Nombre);

        if (nombre.Length < 2)
            return Respuesta<int>.Invalido("El nombre de la marca es muy corto.");

        // La comparacion normaliza LOS DOS LADOS.
        //
        // Antes solo se limpiaba lo que entraba, y si en la base habia
        // un registro con espacios de sobra —de antes de esta regla—
        // no coincidia y se creaba un duplicado. Limpiar la entrada no
        // arregla lo que ya esta guardado mal.
        var existente = await BuscarMarcaPorNombreAsync(nombre, ct);

        if (existente is not null)
        {
            // Si existe pero esta desactivada, se reactiva en vez de
            // rechazar: quien escribe "Ford" quiere que Ford este
            // disponible, y esa es la forma menos confusa de darselo.
            if (!existente.Activa)
            {
                existente.Activa = true;

                _db.Registrar("Marca", existente.Id, AccionAuditoria.Editar, usuarioId,
                    despues: new { Accion = "Reactivada", existente.Nombre });

                await _db.SaveChangesAsync(ct);
                return Respuesta<int>.Ok(existente.Id);
            }

            return Respuesta<int>.Conflicto($"La marca \"{existente.Nombre}\" ya existe.");
        }

        var marca = new Marca { Nombre = nombre, Activa = true };
        _db.Marcas.Add(marca);

        _db.Registrar("Marca", 0, AccionAuditoria.Crear, usuarioId,
            despues: new { marca.Nombre });

        await _db.SaveChangesAsync(ct);

        return Respuesta<int>.Ok(marca.Id);
    }

    public async Task<Respuesta<int>> CrearModeloAsync(
        CrearModeloDto dto, string usuarioId, CancellationToken ct = default)
    {
        var nombre = Normalizar(dto.Nombre);

        if (nombre.Length < 1)
            return Respuesta<int>.Invalido("El nombre del modelo es obligatorio.");

        var marca = await _db.Marcas
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == dto.MarcaId, ct);

        if (marca is null)
            return Respuesta<int>.Invalido("La marca seleccionada no existe.");

        var existente = await BuscarModeloPorNombreAsync(dto.MarcaId, nombre, ct);

        if (existente is not null)
        {
            if (!existente.Activo)
            {
                existente.Activo = true;

                _db.Registrar("Modelo", existente.Id, AccionAuditoria.Editar, usuarioId,
                    despues: new { Accion = "Reactivado", existente.Nombre });

                await _db.SaveChangesAsync(ct);
                return Respuesta<int>.Ok(existente.Id);
            }

            return Respuesta<int>.Conflicto(
                $"{marca.Nombre} ya tiene un modelo llamado \"{existente.Nombre}\".");
        }

        var modelo = new Modelo
        {
            MarcaId = dto.MarcaId,
            Nombre = nombre,
            Activo = true
        };

        _db.Modelos.Add(modelo);

        _db.Registrar("Modelo", 0, AccionAuditoria.Crear, usuarioId,
            despues: new { Marca = marca.Nombre, modelo.Nombre });

        await _db.SaveChangesAsync(ct);

        return Respuesta<int>.Ok(modelo.Id);
    }

    // ══════════════════ DESACTIVAR Y ELIMINAR ══════════════════

    /// <summary>
    /// Desactiva o reactiva una marca.
    ///
    /// Al desactivarla, sus modelos tambien se desactivan: dejarlos
    /// activos permitiria seleccionar un modelo de una marca que ya
    /// no se ofrece.
    /// </summary>
    public async Task<Respuesta<bool>> ActivarMarcaAsync(
        int id, bool activa, string usuarioId, CancellationToken ct = default)
    {
        var marca = await _db.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (marca is null)
            return Respuesta<bool>.NoEncontrado("La marca no existe.");

        marca.Activa = activa;

        await _db.Modelos
            .Where(m => m.MarcaId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Activo, activa), ct);

        _db.Registrar("Marca", id, AccionAuditoria.Editar, usuarioId,
            despues: new { marca.Nombre, Activa = activa });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> ActivarModeloAsync(
        int id, bool activo, string usuarioId, CancellationToken ct = default)
    {
        var modelo = await _db.Modelos.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (modelo is null)
            return Respuesta<bool>.NoEncontrado("El modelo no existe.");

        modelo.Activo = activo;

        _db.Registrar("Modelo", id, AccionAuditoria.Editar, usuarioId,
            despues: new { modelo.Nombre, Activo = activo });

        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    /// <summary>
    /// Elimina una marca, pero SOLO si ningun vehiculo la usa.
    ///
    /// Con vehiculos asociados, borrarla dejaria registros apuntando a
    /// una marca inexistente y rompería el catalogo. La base lo impide
    /// con Restrict, pero se comprueba antes para poder dar un mensaje
    /// util en vez de un error de clave foranea.
    /// </summary>
    public async Task<Respuesta<bool>> EliminarMarcaAsync(
        int id, string usuarioId, CancellationToken ct = default)
    {
        var marca = await _db.Marcas.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (marca is null)
            return Respuesta<bool>.NoEncontrado("La marca no existe.");

        var enUso = await _db.Vehiculos.CountAsync(v => v.MarcaId == id, ct);

        if (enUso > 0)
            return Respuesta<bool>.Conflicto(
                $"No se puede eliminar: hay {enUso} " +
                $"{(enUso == 1 ? "vehículo que usa" : "vehículos que usan")} " +
                "esta marca. Desactivala para que no aparezca en el catálogo.");

        // Los modelos se borran primero: la relacion es Restrict, asi
        // que la base rechazaria borrar la marca con modelos vivos.
        var modelos = await _db.Modelos.CountAsync(m => m.MarcaId == id, ct);

        if (modelos > 0)
            await _db.Modelos.Where(m => m.MarcaId == id).ExecuteDeleteAsync(ct);

        _db.Registrar("Marca", id, AccionAuditoria.Eliminar, usuarioId,
            antes: new { marca.Nombre, ModelosEliminados = modelos });

        _db.Marcas.Remove(marca);
        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    public async Task<Respuesta<bool>> EliminarModeloAsync(
        int id, string usuarioId, CancellationToken ct = default)
    {
        var modelo = await _db.Modelos.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (modelo is null)
            return Respuesta<bool>.NoEncontrado("El modelo no existe.");

        var enUso = await _db.Vehiculos.CountAsync(v => v.ModeloId == id, ct);

        if (enUso > 0)
            return Respuesta<bool>.Conflicto(
                $"No se puede eliminar: hay {enUso} " +
                $"{(enUso == 1 ? "vehículo que usa" : "vehículos que usan")} " +
                "este modelo. Desactivalo para que no aparezca en el catálogo.");

        _db.Registrar("Modelo", id, AccionAuditoria.Eliminar, usuarioId,
            antes: new { modelo.Nombre });

        _db.Modelos.Remove(modelo);
        await _db.SaveChangesAsync(ct);

        return Respuesta<bool>.Ok(true);
    }

    /// <summary>
    /// Limpia los nombres con espacios de sobra que quedaron de antes
    /// de la normalizacion, y fusiona los duplicados que se crearon
    /// por esa causa.
    ///
    /// Se ejecuta una sola vez desde el panel. Los vehiculos de los
    /// registros duplicados se reapuntan al que se conserva antes de
    /// borrarlos: ninguno queda huerfano.
    /// </summary>
    public async Task<Respuesta<LimpiezaDto>> LimpiarDuplicadosAsync(
        string usuarioId, CancellationToken ct = default)
    {
        var marcasFusionadas = 0;
        var modelosFusionados = 0;
        var nombresCorregidos = 0;

        // ── Marcas ──
        var marcas = await _db.Marcas.ToListAsync(ct);

        var gruposMarca = marcas
            .GroupBy(m => Normalizar(m.Nombre).ToLowerInvariant())
            .Where(g => g.Count() > 1);

        foreach (var grupo in gruposMarca)
        {
            // Se conserva la mas antigua: es la que mas probablemente
            // tenga vehiculos asociados.
            var conservar = grupo.OrderBy(m => m.Id).First();
            var duplicadas = grupo.Where(m => m.Id != conservar.Id).ToList();

            foreach (var dup in duplicadas)
            {
                await _db.Vehiculos
                    .Where(v => v.MarcaId == dup.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        v => v.MarcaId, conservar.Id), ct);

                await _db.Modelos
                    .Where(m => m.MarcaId == dup.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        m => m.MarcaId, conservar.Id), ct);

                _db.Marcas.Remove(dup);
                marcasFusionadas++;
            }

            conservar.Nombre = Normalizar(conservar.Nombre);
        }

        // Los que no tenian duplicado pero si espacios de sobra.
        foreach (var m in marcas.Where(x => _db.Entry(x).State != EntityState.Deleted))
        {
            var limpio = Normalizar(m.Nombre);

            if (m.Nombre != limpio)
            {
                m.Nombre = limpio;
                nombresCorregidos++;
            }
        }

        await _db.SaveChangesAsync(ct);

        // ── Modelos ──
        // Se hace despues de guardar las marcas porque algunos modelos
        // acaban de cambiar de MarcaId, y agrupar antes daria grupos
        // incorrectos.
        var modelos = await _db.Modelos.ToListAsync(ct);

        var gruposModelo = modelos
            .GroupBy(m => new { m.MarcaId, Nombre = Normalizar(m.Nombre).ToLowerInvariant() })
            .Where(g => g.Count() > 1);

        foreach (var grupo in gruposModelo)
        {
            var conservar = grupo.OrderBy(m => m.Id).First();
            var duplicados = grupo.Where(m => m.Id != conservar.Id).ToList();

            foreach (var dup in duplicados)
            {
                await _db.Vehiculos
                    .Where(v => v.ModeloId == dup.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        v => v.ModeloId, conservar.Id), ct);

                _db.Modelos.Remove(dup);
                modelosFusionados++;
            }

            conservar.Nombre = Normalizar(conservar.Nombre);
        }

        foreach (var m in modelos.Where(x => _db.Entry(x).State != EntityState.Deleted))
        {
            var limpio = Normalizar(m.Nombre);

            if (m.Nombre != limpio)
            {
                m.Nombre = limpio;
                nombresCorregidos++;
            }
        }

        _db.Registrar("Catalogo", 0, AccionAuditoria.Editar, usuarioId,
            despues: new { marcasFusionadas, modelosFusionados, nombresCorregidos });

        await _db.SaveChangesAsync(ct);

        return Respuesta<LimpiezaDto>.Ok(
            new LimpiezaDto(marcasFusionadas, modelosFusionados, nombresCorregidos));
    }

    // ══════════════════ OPCIONES ══════════════════

    public Respuesta<IEnumerable<OpcionDto>> ListarOpciones(string tipo)
    {
        IEnumerable<OpcionDto> opciones = tipo?.ToLowerInvariant() switch
        {
            "transmision" => Enum.GetValues<Transmision>()
                .Select(v => new OpcionDto((short)v, Etiquetas.De(v))),

            "combustible" => Enum.GetValues<Combustible>()
                .Select(v => new OpcionDto((short)v, Etiquetas.De(v))),

            "traccion" => Enum.GetValues<Traccion>()
                .Select(v => new OpcionDto((short)v, Etiquetas.De(v))),

            "estado" => Enum.GetValues<EstadoVehiculo>()
                .Select(v => new OpcionDto((short)v, Etiquetas.De(v))),

            "estadosolicitud" => Enum.GetValues<EstadoSolicitud>()
                .Select(v => new OpcionDto((short)v, Etiquetas.De(v))),

            "formapago" => Enum.GetValues<FormaPago>()
                .Select(v => new OpcionDto((short)v, Etiquetas.De(v))),

            _ => []
        };

        var lista = opciones.ToList();

        return lista.Count == 0
            ? Respuesta<IEnumerable<OpcionDto>>.NoEncontrado(
                $"No existe el tipo de opción \"{tipo}\".")
            : Respuesta<IEnumerable<OpcionDto>>.Ok(lista);
    }

    // ══════════════════ PRIVADOS ══════════════════

    /// <summary>
    /// Busca ignorando mayusculas Y espacios de sobra a ambos lados.
    ///
    /// Se trae la lista y se compara en memoria porque Trim() dentro
    /// de una consulta de EF no se traduce de forma confiable a SQL.
    /// Con unos cientos de marcas el costo es imperceptible.
    /// </summary>
    private async Task<Marca?> BuscarMarcaPorNombreAsync(
        string nombre, CancellationToken ct)
    {
        var buscado = nombre.ToLowerInvariant();

        var candidatas = await _db.Marcas.ToListAsync(ct);

        return candidatas.FirstOrDefault(m =>
            Normalizar(m.Nombre).Equals(buscado, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Modelo?> BuscarModeloPorNombreAsync(
        int marcaId, string nombre, CancellationToken ct)
    {
        var buscado = nombre.ToLowerInvariant();

        var candidatos = await _db.Modelos
            .Where(m => m.MarcaId == marcaId)
            .ToListAsync(ct);

        return candidatos.FirstOrDefault(m =>
            Normalizar(m.Nombre).Equals(buscado, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Quita espacios de los extremos y colapsa los del medio:
    /// "  Land   Rover  " se vuelve "Land Rover".
    ///
    /// Sin esto, dos marcas que se ven iguales pero tienen distinta
    /// cantidad de espacios pasan como distintas.
    /// </summary>
    private static string Normalizar(string texto) =>
        string.Join(' ', texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

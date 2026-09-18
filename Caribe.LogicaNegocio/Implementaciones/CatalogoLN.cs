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

    public async Task<Respuesta<IEnumerable<MarcaDto>>> ListarMarcasAsync(
        bool soloActivas = true, CancellationToken ct = default)
    {
        var consulta = _db.Marcas.AsNoTracking();

        // El sitio publico solo debe ver las activas; el panel necesita
        // ver todo para poder reactivar lo desactivado.
        if (soloActivas)
            consulta = consulta.Where(m => m.Activa);

        var lista = await consulta
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaDto(
                m.Id,
                m.Nombre,
                m.Activa,
                // Se cuenta en la base. Traer los modelos para contarlos
                // en memoria pediria cientos de filas que nadie mira.
                m.Modelos.Count(mo => mo.Activo)))
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
                m.Id, m.MarcaId, m.Marca.Nombre, m.Nombre, m.Activo))
            .ToListAsync(ct);

        return Respuesta<IEnumerable<ModeloDto>>.Ok(lista);
    }

    public async Task<Respuesta<int>> CrearMarcaAsync(
        CrearMarcaDto dto, string usuarioId, CancellationToken ct = default)
    {
        var nombre = Normalizar(dto.Nombre);

        if (nombre.Length < 2)
            return Respuesta<int>.Invalido("El nombre de la marca es muy corto.");

        // La comparacion es sin distinguir mayusculas: "toyota" y
        // "Toyota" son la misma marca, y tenerlas duplicadas partiria
        // el catalogo en dos.
        var existente = await _db.Marcas
            .FirstOrDefaultAsync(m => m.Nombre.ToLower() == nombre.ToLower(), ct);

        if (existente is not null)
        {
            // Si existe pero esta desactivada, se reactiva en vez de
            // rechazar: el usuario quiere esa marca disponible, y esa
            // es la forma menos confusa de dársela.
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

        // El nombre se repite entre marcas: puede haber un Civic de
        // Honda y otro modelo igual en otra marca. Lo unico es el par.
        var existente = await _db.Modelos
            .FirstOrDefaultAsync(m => m.MarcaId == dto.MarcaId
                                   && m.Nombre.ToLower() == nombre.ToLower(), ct);

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

    /// <summary>
    /// Opciones de los desplegables, sacadas de los enums.
    ///
    /// Angular las consume de aqui para que los textos vivan en un
    /// solo lugar: si el backend dice "En tránsito" y el frontend
    /// "En transito", el cliente pregunta si son estados distintos.
    /// </summary>
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

    /// <summary>
    /// Quita espacios sobrantes y los del medio: "  Land   Rover  "
    /// se vuelve "Land Rover". Sin esto, dos marcas que se ven iguales
    /// pero tienen distinta cantidad de espacios pasarian como
    /// distintas.
    /// </summary>
    private static string Normalizar(string texto) =>
        string.Join(' ', texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

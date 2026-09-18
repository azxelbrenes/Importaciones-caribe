namespace Caribe.Utilitarios;

/// <summary>
/// Pagina de resultados.
///
/// Devuelve el total y las banderas de navegacion calculadas para que
/// el frontend no tenga que hacer aritmetica: si cada pantalla calcula
/// "hay siguiente" por su cuenta, tarde o temprano una lo hace mal.
/// </summary>
public class Pagina<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

    public int NumeroPagina { get; init; }
    public int PorPagina { get; init; }
    public int TotalRegistros { get; init; }

    public int TotalPaginas => PorPagina > 0
        ? (int)Math.Ceiling(TotalRegistros / (double)PorPagina)
        : 0;

    public bool HayAnterior => NumeroPagina > 1;
    public bool HaySiguiente => NumeroPagina < TotalPaginas;

    public static Pagina<T> Crear(
        IReadOnlyList<T> items, int pagina, int porPagina, int total) => new()
    {
        Items = items,
        NumeroPagina = pagina,
        PorPagina = porPagina,
        TotalRegistros = total
    };

    public static Pagina<T> Vacia(int porPagina = 12) => new()
    {
        Items = [],
        NumeroPagina = 1,
        PorPagina = porPagina,
        TotalRegistros = 0
    };
}

/// <summary>
/// Parametros de paginacion comunes a todos los listados.
///
/// Los limites no son decorativos: sin un maximo, alguien podria pedir
/// porPagina=100000 y forzar al servidor a materializar toda la tabla.
/// </summary>
public abstract class FiltroPaginado
{
    private int _pagina = 1;
    private int _porPagina = 12;

    public int Pagina
    {
        get => _pagina;
        set => _pagina = value < 1 ? 1 : value;
    }

    public int PorPagina
    {
        get => _porPagina;
        set => _porPagina = value switch
        {
            < 1   => 12,
            > 100 => 100,
            _     => value
        };
    }

    public int Saltar => (Pagina - 1) * PorPagina;
}

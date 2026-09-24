using ImageMagick;

namespace Caribe.LogicaNegocio.Almacenamiento;

/// <summary>
/// Convierte a WebP y genera dos tamanos.
///
/// Una foto de celular pesa 5 MB. Con 500 vehiculos y 8 fotos cada
/// uno serian 20 GB; procesadas quedan en ~200 KB, menos de 1 GB en
/// total. En un sitio que se ve desde el celular con datos moviles,
/// esa es la diferencia entre que cargue y que la gente se vaya.
///
/// Se usa Magick.NET (Apache 2.0) y no ImageSharp: este ultimo paso
/// a exigir licencia comercial en su version 4, y entregarle a un
/// cliente software con una biblioteca mal licenciada es un problema
/// que aparece despues, cuando ya nadie lo esta mirando.
/// </summary>
public static class ProcesadorImagen
{
    private const uint AnchoPrincipal = 1200;
    private const uint AltoPrincipal  = 900;
    private const uint AnchoThumb     = 400;
    private const uint AltoThumb      = 300;
    private const uint Calidad        = 82;

    /// <summary>
    /// Firmas reales de archivo. La extension y el content-type los
    /// controla el cliente, asi que no se les cree: un ejecutable
    /// renombrado a .jpg pasaria cualquier validacion basada en el
    /// nombre.
    /// </summary>
    private static readonly byte[][] FirmasValidas =
    [
        [0xFF, 0xD8, 0xFF],                    // JPEG
        [0x89, 0x50, 0x4E, 0x47],              // PNG
        [0x52, 0x49, 0x46, 0x46],              // RIFF (WebP)
        [0x42, 0x4D]                           // BMP
    ];

    /// <summary>
    /// Tipos de archivo HEIC y HEIF, el formato con el que el iPhone
    /// guarda las fotos desde iOS 11.
    ///
    /// No tienen una firma al inicio como los demas: llevan la caja
    /// "ftyp" en el byte 4 y el tipo concreto en el 8.
    /// </summary>
    private static readonly string[] TiposHeic =
        ["heic", "heix", "hevc", "hevx", "mif1", "msf1", "heim", "heis"];

    /// <summary>
    /// Si el archivo es HEIC o HEIF.
    ///
    /// Se comprueba aparte para poder dar un mensaje util cuando el
    /// servidor no logra convertirlo: decirle a alguien "la imagen
    /// esta danada" cuando en realidad es el formato de su telefono
    /// lo deja sin saber que hacer.
    /// </summary>
    public static bool EsHeic(Stream s)
    {
        if (s.Length < 12) return false;

        s.Position = 0;
        var cabecera = new byte[12];
        var leidos = s.Read(cabecera, 0, 12);
        s.Position = 0;

        if (leidos < 12) return false;

        // Bytes 4 a 7: "ftyp"
        if (cabecera[4] != 0x66 || cabecera[5] != 0x74 ||
            cabecera[6] != 0x79 || cabecera[7] != 0x70) return false;

        var tipo = System.Text.Encoding.ASCII.GetString(cabecera, 8, 4).ToLowerInvariant();

        return TiposHeic.Contains(tipo);
    }

    public static bool EsImagenValida(Stream s)
    {
        if (s.Length < 8) return false;

        // El iPhone guarda en HEIC. Sin esto, cada foto tomada desde
        // un iPhone se rechaza antes siquiera de intentar procesarla.
        if (EsHeic(s)) return true;

        s.Position = 0;
        var cabecera = new byte[8];
        var leidos = s.Read(cabecera, 0, 8);
        s.Position = 0;

        if (leidos < 8) return false;

        foreach (var firma in FirmasValidas)
        {
            var coincide = true;

            for (var i = 0; i < firma.Length; i++)
            {
                if (cabecera[i] != firma[i]) { coincide = false; break; }
            }

            if (coincide) return true;
        }

        return false;
    }

    public static (MemoryStream principal, MemoryStream thumb) Procesar(Stream original)
    {
        original.Position = 0;

        // MagickImage lee HEIC igual que JPEG o PNG, y la salida
        // siempre es WebP: el formato de entrada deja de importar
        // apenas se procesa.
        using var imagen = new MagickImage(original);

        // Quita los metadatos EXIF, que incluyen la ubicacion GPS de
        // donde se tomo la foto. Si el dueno fotografia un vehiculo en
        // su casa y esa imagen llega al catalogo con las coordenadas
        // adentro, acaba de publicar su direccion.
        imagen.Strip();

        // Corrige la orientacion antes de recortar: las fotos de
        // celular vienen giradas con una etiqueta EXIF que al quitarla
        // dejaria la imagen de lado.
        imagen.AutoOrient();

        var principal = Redimensionar(imagen, AnchoPrincipal, AltoPrincipal);
        var thumb     = Redimensionar(imagen, AnchoThumb, AltoThumb);

        return (principal, thumb);
    }

    private static MemoryStream Redimensionar(MagickImage origen, uint ancho, uint alto)
    {
        using var copia = (MagickImage)origen.Clone();

        // Recorte al centro: todas las tarjetas del catalogo quedan
        // con la misma proporcion. Sin esto, una foto vertical
        // descuadra toda la rejilla.
        copia.Resize(new MagickGeometry(ancho, alto) { FillArea = true });
        copia.Crop(ancho, alto, Gravity.Center);
        copia.ResetPage();

        copia.Format = MagickFormat.WebP;
        copia.Quality = Calidad;

        var ms = new MemoryStream();
        copia.Write(ms);
        ms.Position = 0;

        return ms;
    }
}

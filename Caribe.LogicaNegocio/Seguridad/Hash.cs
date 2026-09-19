using System.Security.Cryptography;
using System.Text;

namespace Caribe.LogicaNegocio.Seguridad;

/// <summary>
/// Genera y compara tokens opacos: invitaciones, refresh tokens y
/// enlaces de recuperacion.
///
/// El valor en claro se entrega una sola vez —en un correo o en una
/// cookie—; en la base vive solo el hash. Una copia de la base no
/// permite usar ninguno de esos tokens.
///
/// No sirve para contrasenas: esas las maneja Identity con PBKDF2,
/// que es lento a proposito para resistir fuerza bruta. SHA-256 es
/// rapido, que es lo correcto para tokens aleatorios de 256 bits
/// —no hay nada que adivinar— pero seria un error para contrasenas
/// elegidas por personas.
/// </summary>
public static class Hash
{
    /// <summary>Token aleatorio de 256 bits, apto para URL.</summary>
    public static string GenerarToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static string Calcular(string valor) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)))
            .ToLowerInvariant();

    /// <summary>
    /// Comparacion en tiempo constante.
    ///
    /// Una comparacion normal se detiene en el primer caracter que
    /// difiere, y eso filtra informacion: midiendo cuanto tarda, un
    /// atacante puede ir adivinando el token caracter por caracter.
    /// </summary>
    public static bool Coincide(string valorEnClaro, string hashGuardado)
    {
        var calculado = Encoding.UTF8.GetBytes(Calcular(valorEnClaro));
        var guardado = Encoding.UTF8.GetBytes(hashGuardado);

        return CryptographicOperations.FixedTimeEquals(calculado, guardado);
    }

    /// <summary>
    /// Contrasena temporal legible, para dictar por telefono o pasar
    /// por WhatsApp.
    ///
    /// Sin O, 0, l, 1 ni I: son los caracteres que se confunden al
    /// leerlos, y una contrasena que hay que repetir tres veces es
    /// una mala contrasena aunque sea segura.
    /// </summary>
    public static string GenerarPasswordTemporal()
    {
        const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minusculas = "abcdefghijkmnpqrstuvwxyz";
        const string numeros = "23456789";

        var bytes = RandomNumberGenerator.GetBytes(14);
        var sb = new StringBuilder(14);

        // Se garantiza mayuscula y numeros: son las reglas que exige
        // Identity, y una contrasena generada que no las cumpla seria
        // rechazada al aplicarla.
        sb.Append(mayusculas[bytes[0] % mayusculas.Length]);

        for (var i = 1; i < 12; i++)
            sb.Append(minusculas[bytes[i] % minusculas.Length]);

        sb.Append(numeros[bytes[12] % numeros.Length]);
        sb.Append(numeros[bytes[13] % numeros.Length]);

        return sb.ToString();
    }
}

using Caribe.LogicaNegocio.Dtos.Usuarios;
using FluentValidation;

namespace Caribe.LogicaNegocio.Validadores;

/// <summary>
/// Reglas de contrasena, en un solo lugar.
///
/// Se reutilizan en los tres puntos donde alguien elige una: aceptar
/// invitacion, cambiarla y restablecerla con enlace. Sin esto, es
/// facil que una de las tres quede con reglas distintas.
/// </summary>
public static class ReglasPassword
{
    public static IRuleBuilderOptions<T, string> Password<T>(
        this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(10).WithMessage("Debe tener al menos 10 caracteres.")
            .MaximumLength(128)
            .Must(p => p.Any(char.IsLetter))
            .WithMessage("Debe incluir al menos una letra.")
            .Must(p => p.Any(char.IsDigit))
            .WithMessage("Debe incluir al menos un número.");
}

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Indique su correo.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");

        // Aqui NO se validan las reglas de contrasena: al entrar solo
        // importa si coincide. Exigir diez caracteres en el login le
        // diria a un atacante cual es la regla sin necesidad de tener
        // una cuenta.
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Indique su contraseña.");

        // Seis digitos si viene de la aplicacion, o un codigo de
        // respaldo si la persona perdio el telefono. Se acepta
        // cualquiera de los dos largos y el backend decide cual es.
        RuleFor(x => x.CodigoDobleFactor)
            .Must(c => c is null || FormatoCodigo.Valido(c))
            .WithMessage("El código no tiene un formato válido.");
    }
}

/// <summary>
/// Seis digitos, o un codigo de respaldo de entre 8 y 20 caracteres.
/// </summary>
file static class FormatoCodigo
{
    public static bool Valido(string c)
    {
        var limpio = c.Replace(" ", "").Replace("-", "");
        return limpio.Length == 6 || (limpio.Length >= 8 && limpio.Length <= 20);
    }
}

public class CambiarPasswordValidator : AbstractValidator<CambiarPasswordDto>
{
    public CambiarPasswordValidator()
    {
        RuleFor(x => x.PasswordActual)
            .NotEmpty().WithMessage("Indique su contraseña actual.");

        RuleFor(x => x.PasswordNueva).Password();

        RuleFor(x => x)
            .Must(x => x.PasswordActual != x.PasswordNueva)
            .WithMessage("La contraseña nueva debe ser distinta de la actual.")
            .WithName("PasswordNueva");
    }
}

public class RestablecerConTokenValidator : AbstractValidator<RestablecerConTokenDto>
{
    public RestablecerConTokenValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.PasswordNueva).Password();

        RuleFor(x => x.ConfirmarPassword)
            .Equal(x => x.PasswordNueva)
            .WithMessage("Las contraseñas no coinciden.");
    }
}

public class AceptarInvitacionValidator : AbstractValidator<AceptarInvitacionDto>
{
    public AceptarInvitacionValidator()
    {
        RuleFor(x => x.Token).NotEmpty();

        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("Escriba su nombre.")
            .MinimumLength(3).WithMessage("Escriba su nombre completo.")
            .MaximumLength(120);

        RuleFor(x => x.Password).Password();

        RuleFor(x => x.ConfirmarPassword)
            .Equal(x => x.Password)
            .WithMessage("Las contraseñas no coinciden.");
    }
}

public class CrearInvitacionValidator : AbstractValidator<CrearInvitacionDto>
{
    public CrearInvitacionValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Indique el correo.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(256);

        RuleFor(x => x.Rol)
            .InclusiveBetween((short)1, (short)2)
            .WithMessage("Seleccione Administrador u Operador.");
    }
}

public class SolicitarRecuperacionValidator : AbstractValidator<SolicitarRecuperacionDto>
{
    public SolicitarRecuperacionValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Indique su correo.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.");
    }
}

using System.Text.RegularExpressions;
using Caribe.LogicaNegocio.Dtos.Solicitudes;
using FluentValidation;

namespace Caribe.LogicaNegocio.Validadores;

public partial class CrearSolicitudValidator : AbstractValidator<CrearSolicitudDto>
{
    public CrearSolicitudValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Escriba su nombre.")
            .MinimumLength(3).WithMessage("Escriba su nombre completo.")
            .MaximumLength(120);

        RuleFor(x => x.Whatsapp)
            .NotEmpty().WithMessage("Indique su número de WhatsApp.")
            .Must(TieneDigitosValidos)
            .WithMessage("El número debe tener entre 8 y 15 dígitos.");

        RuleFor(x => x.MarcaTexto).MaximumLength(60);
        RuleFor(x => x.ModeloTexto).MaximumLength(80);
        RuleFor(x => x.Detalles).MaximumLength(1000);

        RuleFor(x => x.AnioDesde)
            .InclusiveBetween((short)1980, (short)(DateTime.UtcNow.Year + 1))
            .When(x => x.AnioDesde.HasValue);

        RuleFor(x => x.PresupuestoMin)
            .GreaterThan(0).When(x => x.PresupuestoMin.HasValue);

        RuleFor(x => x.PresupuestoMax)
            .GreaterThan(0).When(x => x.PresupuestoMax.HasValue);

        RuleFor(x => x)
            .Must(x => !x.PresupuestoMin.HasValue
                    || !x.PresupuestoMax.HasValue
                    || x.PresupuestoMin <= x.PresupuestoMax)
            .WithMessage("El presupuesto máximo debe ser mayor al mínimo.")
            .WithName("Presupuesto");

        // Ley 8968: sin consentimiento explicito no se pueden tratar
        // los datos. Se valida aqui ademas de en el servicio porque
        // es una obligacion legal, no una regla de negocio mas.
        RuleFor(x => x.Consentimiento)
            .Equal(true)
            .WithMessage("Debe aceptar el uso de sus datos para ser contactado.");

        RuleFor(x => x.Origen).InclusiveBetween((short)0, (short)3);
        RuleFor(x => x.FormaPago).InclusiveBetween((short)0, (short)2);

        RuleFor(x => x.PlazoMesesInteres)
            .InclusiveBetween((short)1, (short)120)
            .When(x => x.PlazoMesesInteres.HasValue);
    }

    private static bool TieneDigitosValidos(string? numero)
    {
        if (string.IsNullOrWhiteSpace(numero)) return false;

        var digitos = SoloDigitos().Replace(numero, "");
        return digitos.Length is >= 8 and <= 15;
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex SoloDigitos();
}

public class ActualizarSolicitudValidator : AbstractValidator<ActualizarSolicitudDto>
{
    public ActualizarSolicitudValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Estado).InclusiveBetween((short)0, (short)5);
    }
}

public class CrearNotaValidator : AbstractValidator<CrearNotaDto>
{
    public CrearNotaValidator()
    {
        RuleFor(x => x.SolicitudId).GreaterThan(0);

        RuleFor(x => x.Nota)
            .NotEmpty().WithMessage("La nota está vacía.")
            .MinimumLength(2)
            .MaximumLength(2000);
    }
}

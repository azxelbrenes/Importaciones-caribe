using Caribe.LogicaNegocio.Dtos.Catalogos;
using Caribe.LogicaNegocio.Dtos.Financiamiento;
using FluentValidation;

namespace Caribe.LogicaNegocio.Validadores;

public class CrearMarcaValidator : AbstractValidator<CrearMarcaDto>
{
    public CrearMarcaValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Escriba el nombre de la marca.")
            .MinimumLength(2)
            .MaximumLength(60);
    }
}

public class CrearModeloValidator : AbstractValidator<CrearModeloDto>
{
    public CrearModeloValidator()
    {
        RuleFor(x => x.MarcaId)
            .GreaterThan(0).WithMessage("Seleccione una marca.");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Escriba el nombre del modelo.")
            .MaximumLength(80);
    }
}

public class ActualizarFinanciamientoValidator
    : AbstractValidator<ActualizarFinanciamientoDto>
{
    public ActualizarFinanciamientoValidator()
    {
        RuleFor(x => x.PorcentajePrima)
            .InclusiveBetween(0, 100)
            .WithMessage("La prima debe estar entre 0 y 100 por ciento.");

        RuleFor(x => x.PlazoMinimoMeses)
            .InclusiveBetween((short)1, (short)120);

        RuleFor(x => x.PlazoMaximoMeses)
            .InclusiveBetween((short)1, (short)120)
            .GreaterThanOrEqualTo(x => x.PlazoMinimoMeses)
            .WithMessage("El plazo máximo no puede ser menor al mínimo.");

        RuleFor(x => x.PlazosDisponibles)
            .NotEmpty().WithMessage("Indique los plazos, separados por coma.")
            .MaximumLength(60);
    }
}

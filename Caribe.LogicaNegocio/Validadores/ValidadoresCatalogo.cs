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

        // El limite de 60 es un freno grueso contra un error de tecleo,
        // no el tope legal: ese lo publica el Banco Central cada
        // semestre bajo la Ley 9859 y cambia dos veces al ano.
        RuleFor(x => x.TasaAnual)
            .InclusiveBetween(0, 60)
            .WithMessage("La tasa anual debe estar entre 0 y 60 por ciento.");

        RuleFor(x => x.PlazoMinimoMeses)
            .InclusiveBetween((short)1, (short)120);

        RuleFor(x => x.PlazoMaximoMeses)
            .InclusiveBetween((short)1, (short)120)
            .GreaterThanOrEqualTo(x => x.PlazoMinimoMeses)
            .WithMessage("El plazo máximo no puede ser menor al mínimo.");

        RuleFor(x => x.PlazosDisponibles)
            .NotEmpty().WithMessage("Indique los plazos, separados por coma.")
            .MaximumLength(60);

        RuleFor(x => x.TextoLegal).MaximumLength(2000);

        // Estas dos reglas son las que impiden publicar cuotas sin el
        // respaldo legal. No son una recomendacion en un documento que
        // se olvida: bloquean el guardado.
        RuleFor(x => x.TasaAnual)
            .GreaterThan(0)
            .When(x => x.Activo)
            .WithMessage("Indique la tasa anual antes de activar el financiamiento.");

        RuleFor(x => x.TextoLegal)
            .NotEmpty()
            .When(x => x.Activo)
            .WithMessage("Agregue el texto legal antes de activar el financiamiento.");
    }
}

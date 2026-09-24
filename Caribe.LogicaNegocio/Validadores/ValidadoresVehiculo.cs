using Caribe.LogicaNegocio.Dtos.Vehiculos;
using FluentValidation;

namespace Caribe.LogicaNegocio.Validadores;

public class CrearVehiculoValidator : AbstractValidator<CrearVehiculoDto>
{
    public CrearVehiculoValidator()
    {
        RuleFor(x => x.MarcaId)
            .GreaterThan(0).WithMessage("Seleccione una marca.");

        RuleFor(x => x.ModeloId)
            .GreaterThan(0).WithMessage("Seleccione un modelo.");

        // El limite superior es el ano siguiente: los modelos nuevos
        // salen antes de que termine el ano calendario.
        RuleFor(x => x.Anio)
            .InclusiveBetween((short)1980, (short)(DateTime.UtcNow.Year + 1))
            .WithMessage($"El año debe estar entre 1980 y {DateTime.UtcNow.Year + 1}.");

        RuleFor(x => x.Kilometraje)
            .InclusiveBetween(0, 2_000_000)
            .WithMessage("El kilometraje no es válido.");

        RuleFor(x => x.CostoVehiculo)
            .GreaterThan(0).WithMessage("Indique el costo del vehículo.")
            .LessThan(10_000_000).WithMessage("El costo parece incorrecto.");

        RuleFor(x => x.CostoFlete).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CostoImpuestos).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CostoTramites).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Honorario)
            .GreaterThan(0).WithMessage("Indique su honorario.");

        RuleFor(x => x.VigenciaDias)
            .InclusiveBetween((short)1, (short)90)
            .WithMessage("La vigencia debe estar entre 1 y 90 días.");

        // Tiempo de importacion: opcional, pero si viene tiene que
        // tener sentido. Un ano es mas que cualquier importacion real.
        RuleFor(x => x.SemanasImportacionMin)
            .InclusiveBetween((short)1, (short)52)
            .When(x => x.SemanasImportacionMin.HasValue)
            .WithMessage("El tiempo mínimo de importación debe estar entre 1 y 52 semanas.");

        RuleFor(x => x.SemanasImportacionMax)
            .InclusiveBetween((short)1, (short)52)
            .When(x => x.SemanasImportacionMax.HasValue)
            .WithMessage("El tiempo máximo de importación debe estar entre 1 y 52 semanas.");

        RuleFor(x => x.SemanasImportacionMin)
            .NotNull()
            .When(x => x.SemanasImportacionMax.HasValue)
            .WithMessage("Indique el tiempo mínimo de importación si pone un máximo.");

        RuleFor(x => x.SemanasImportacionMax)
            .GreaterThanOrEqualTo(x => x.SemanasImportacionMin)
            .When(x => x.SemanasImportacionMin.HasValue && x.SemanasImportacionMax.HasValue)
            .WithMessage("El tiempo máximo de importación no puede ser menor al mínimo.");

        RuleFor(x => x.Color).MaximumLength(40);
        RuleFor(x => x.Descripcion).MaximumLength(4000);

        // Los enums se validan por rango porque un short fuera de
        // rango se convertiria sin error y quedaria un valor invalido
        // guardado en la base.
        RuleFor(x => x.Transmision).InclusiveBetween((short)0, (short)1);
        RuleFor(x => x.Combustible).InclusiveBetween((short)0, (short)3);
        RuleFor(x => x.Traccion).InclusiveBetween((short)0, (short)2);
    }
}

public class ActualizarVehiculoValidator : AbstractValidator<ActualizarVehiculoDto>
{
    public ActualizarVehiculoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        // Se reutilizan las reglas del crear en vez de repetirlas: si
        // manana cambia el limite de un costo, cambia en un solo lugar.
        Include(new CrearVehiculoValidator());
    }
}

public class CambiarEstadoValidator : AbstractValidator<CambiarEstadoDto>
{
    public CambiarEstadoValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.NuevoEstado)
            .InclusiveBetween((short)0, (short)5)
            .WithMessage("El estado indicado no existe.");
    }
}

public class ReordenarFotosValidator : AbstractValidator<ReordenarFotosDto>
{
    public ReordenarFotosValidator()
    {
        RuleFor(x => x.VehiculoId).GreaterThan(0);

        RuleFor(x => x.IdsEnOrden)
            .NotEmpty().WithMessage("La lista de fotografías está vacía.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("La lista tiene fotografías repetidas.");
    }
}

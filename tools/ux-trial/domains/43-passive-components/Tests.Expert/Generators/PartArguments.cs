using System;

using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

/// <summary>
///     The eight arguments of <see cref="Part" />, drawn so that they satisfy every rule that ties them together.
///     A test about one refusal changes the single argument it is about, with <c>with</c>, and leaves the seven
///     others arbitrary but valid.
/// </summary>
public sealed record PartArguments(string                 PartNumber,
                                   ComponentKind          Kind,
                                   double                 NominalValue,
                                   Tolerance              Tolerance,
                                   double                 Rating,
                                   TemperatureCoefficient TemperatureCoefficient,
                                   Package                Package,
                                   decimal                UnitCost) {

    public Part Build() {
        return new Part(PartNumber, Kind, NominalValue, Tolerance, Rating, TemperatureCoefficient, Package, UnitCost);
    }

}

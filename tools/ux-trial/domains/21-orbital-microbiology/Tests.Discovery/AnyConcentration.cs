// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Concentration --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Concentration" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyConcentration : IAny<Concentration> {

    private readonly IAny<double> _cellsPerMillilitre;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyConcentration()
        : this(cellsPerMillilitre: AnyValidCellsPerMillilitre()) { }

    private static AnyDouble AnyValidCellsPerMillilitre() {
        return Any.Double().Between(0, Concentration.MaxCellsPerMillilitre);
    }

    private AnyConcentration(IAny<double> cellsPerMillilitre) {
        _cellsPerMillilitre = cellsPerMillilitre;
    }

    /// <summary>Pins <c>cellsPerMillilitre</c> to a fixed value.</summary>
    public AnyConcentration WithCellsPerMillilitre(double value) {
        return new AnyConcentration(new FixedValue<double>(value));
    }

    /// <summary>Draws <c>cellsPerMillilitre</c> from <c>AnyValidCellsPerMillilitre()</c>, refined by <paramref name="configure" />.</summary>
    public AnyConcentration WithCellsPerMillilitre(Func<AnyDouble, AnyDouble> configure) {
        return new AnyConcentration(configure(AnyValidCellsPerMillilitre()));
    }

    /// <summary>Produces one arbitrary <see cref="Concentration" />.</summary>
    public Concentration Generate() {
        return new Concentration(_cellsPerMillilitre.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}

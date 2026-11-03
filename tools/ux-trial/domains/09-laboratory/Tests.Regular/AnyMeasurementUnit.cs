// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeasurementUnit --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MeasurementUnit" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasurementUnit : IAny<MeasurementUnit> {

    private IAny<string> _symbol;

    private AnyString _symbolRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasurementUnit()
        : this(symbol: AnyValidSymbol()) { }

    private static AnyString AnyValidSymbol() {
        return Any.String().NotBlank().WithMaxLength(MeasurementUnit.MaxLength);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMeasurementUnit(AnyString symbol) {
        _symbol       = symbol;
        _symbolRecipe = symbol;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMeasurementUnit(AnyMeasurementUnit from) {
        _symbol       = from._symbol;
        _symbolRecipe = from._symbolRecipe;
    }

    /// <summary>Pins <c>symbol</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMeasurementUnit WithSymbol(string value) {
        AnyMeasurementUnit copy = new AnyMeasurementUnit(this);

        copy._symbol = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>symbol</c> is drawn from — <c>AnyValidSymbol()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMeasurementUnit WithSymbol(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMeasurementUnit copy = new AnyMeasurementUnit(this);

        copy._symbolRecipe = configure(_symbolRecipe);
        copy._symbol       = copy._symbolRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MeasurementUnit" />.</summary>
    public MeasurementUnit Generate() {
        return new MeasurementUnit(_symbol.Generate());
    }

}

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PointSetting --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="PointSetting" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPointSetting : IAny<PointSetting> {

    private IAny<string>        _pointName;
    private IAny<PointPosition> _required;

    private AnyString              _pointNameRecipe;
    private AnyEnum<PointPosition> _requiredRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPointSetting()
        : this(pointName: AnyValidPointName(),
               required:  AnyValidRequired()) { }

    private static AnyString AnyValidPointName() {
        return Any.String().NotBlank();
    }

    private static AnyEnum<PointPosition> AnyValidRequired() {
        return Any.Enum<PointPosition>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPointSetting(AnyString              pointName,
                            AnyEnum<PointPosition> required) {
        _pointName       = pointName;
        _pointNameRecipe = pointName;
        _required        = required;
        _requiredRecipe  = required;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPointSetting(AnyPointSetting from) {
        _pointName       = from._pointName;
        _pointNameRecipe = from._pointNameRecipe;
        _required        = from._required;
        _requiredRecipe  = from._requiredRecipe;
    }

    /// <summary>Pins <c>pointName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPointSetting WithPointName(string value) {
        AnyPointSetting copy = new AnyPointSetting(this);

        copy._pointName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>pointName</c> is drawn from — <c>AnyValidPointName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPointSetting WithPointName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPointSetting copy = new AnyPointSetting(this);

        copy._pointNameRecipe = configure(_pointNameRecipe);
        copy._pointName       = copy._pointNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>required</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPointSetting WithRequired(PointPosition value) {
        AnyPointSetting copy = new AnyPointSetting(this);

        copy._required = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>required</c> is drawn from — <c>AnyValidRequired()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;PointPosition&gt;, AnyEnum&lt;PointPosition&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPointSetting WithRequired(Func<AnyEnum<PointPosition>, AnyEnum<PointPosition>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPointSetting copy = new AnyPointSetting(this);

        copy._requiredRecipe = configure(_requiredRecipe);
        copy._required       = copy._requiredRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PointSetting" />.</summary>
    public PointSetting Generate() {
        return new PointSetting(_pointName.Generate(),
                                _required.Generate());
    }

}

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CellCoordinate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CellCoordinate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCellCoordinate : IAny<CellCoordinate> {

    private IAny<int> _i;
    private IAny<int> _j;
    private IAny<int> _k;

    private AnyInt32 _iRecipe;
    private AnyInt32 _jRecipe;
    private AnyInt32 _kRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCellCoordinate()
        : this(i: AnyValidI(),
               j: AnyValidJ(),
               k: AnyValidK()) { }

    private static AnyInt32 AnyValidI() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyInt32 AnyValidJ() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyInt32 AnyValidK() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCellCoordinate(AnyInt32 i,
                              AnyInt32 j,
                              AnyInt32 k) {
        _i       = i;
        _iRecipe = i;
        _j       = j;
        _jRecipe = j;
        _k       = k;
        _kRecipe = k;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCellCoordinate(AnyCellCoordinate from) {
        _i       = from._i;
        _iRecipe = from._iRecipe;
        _j       = from._j;
        _jRecipe = from._jRecipe;
        _k       = from._k;
        _kRecipe = from._kRecipe;
    }

    /// <summary>Pins <c>i</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCellCoordinate WithI(int value) {
        AnyCellCoordinate copy = new AnyCellCoordinate(this);

        copy._i = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>i</c> is drawn from — <c>AnyValidI()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCellCoordinate WithI(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCellCoordinate copy = new AnyCellCoordinate(this);

        copy._iRecipe = configure(_iRecipe);
        copy._i       = copy._iRecipe;

        return copy;
    }

    /// <summary>Pins <c>j</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCellCoordinate WithJ(int value) {
        AnyCellCoordinate copy = new AnyCellCoordinate(this);

        copy._j = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>j</c> is drawn from — <c>AnyValidJ()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCellCoordinate WithJ(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCellCoordinate copy = new AnyCellCoordinate(this);

        copy._jRecipe = configure(_jRecipe);
        copy._j       = copy._jRecipe;

        return copy;
    }

    /// <summary>Pins <c>k</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCellCoordinate WithK(int value) {
        AnyCellCoordinate copy = new AnyCellCoordinate(this);

        copy._k = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>k</c> is drawn from — <c>AnyValidK()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCellCoordinate WithK(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCellCoordinate copy = new AnyCellCoordinate(this);

        copy._kRecipe = configure(_kRecipe);
        copy._k       = copy._kRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CellCoordinate" />.</summary>
    public CellCoordinate Generate() {
        return new CellCoordinate(_i.Generate(),
                                  _j.Generate(),
                                  _k.Generate());
    }

}

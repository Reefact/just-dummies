// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SiteChronology --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SiteChronology" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySiteChronology : IAny<SiteChronology> {

    private IAny<string>           _siteName;
    private IAny<CalibrationCurve> _curve;

    private AnyString           _siteNameRecipe;
    private AnyCalibrationCurve _curveRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySiteChronology()
        : this(siteName: AnyValidSiteName(),
               curve:    AnyValidCurve()) { }

    private static AnyString AnyValidSiteName() {
        return Any.String().NotBlank();
    }

    private static AnyCalibrationCurve AnyValidCurve() {
        return new AnyCalibrationCurve();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySiteChronology(AnyString           siteName,
                              AnyCalibrationCurve curve) {
        _siteName       = siteName;
        _siteNameRecipe = siteName;
        _curve          = curve;
        _curveRecipe    = curve;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySiteChronology(AnySiteChronology from) {
        _siteName       = from._siteName;
        _siteNameRecipe = from._siteNameRecipe;
        _curve          = from._curve;
        _curveRecipe    = from._curveRecipe;
    }

    /// <summary>Pins <c>siteName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySiteChronology WithSiteName(string value) {
        AnySiteChronology copy = new AnySiteChronology(this);

        copy._siteName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>siteName</c> is drawn from — <c>AnyValidSiteName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySiteChronology WithSiteName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySiteChronology copy = new AnySiteChronology(this);

        copy._siteNameRecipe = configure(_siteNameRecipe);
        copy._siteName       = copy._siteNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>curve</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySiteChronology WithCurve(CalibrationCurve value) {
        AnySiteChronology copy = new AnySiteChronology(this);

        copy._curve = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>curve</c> is drawn from — <c>AnyValidCurve()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCalibrationCurve, AnyCalibrationCurve&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySiteChronology WithCurve(Func<AnyCalibrationCurve, AnyCalibrationCurve> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySiteChronology copy = new AnySiteChronology(this);

        copy._curveRecipe = configure(_curveRecipe);
        copy._curve       = copy._curveRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="SiteChronology" />.</summary>
    public SiteChronology Generate() {
        return new SiteChronology(_siteName.Generate(),
                                  _curve.Generate());
    }

}

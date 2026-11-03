// Scaffolded by `any` (JustDummies), then completed by hand: the three cross terms are not drawn on their own.
// A cross term must give a correlation strictly inside (-1, 1) and keep the matrix positive definite, so it is
// drawn as a correlation in [-MaximumCorrelation, MaximumCorrelation] and scaled by the two standard deviations.
// With every |correlation| <= 0.3 the correlation matrix is diagonally dominant, hence positive definite.

using System;

using JustDummies;

namespace SatelliteConjunction.Domain;

/// <summary>A generator of arbitrary <see cref="PositionCovariance" /> values.</summary>
public sealed partial class AnyPositionCovariance : IAny<PositionCovariance> {

    private IAny<double> _xx;
    private IAny<double> _yy;
    private IAny<double> _zz;
    private IAny<double> _correlationXy;
    private IAny<double> _correlationXz;
    private IAny<double> _correlationYz;

    private AnyDouble _xxRecipe;
    private AnyDouble _yyRecipe;
    private AnyDouble _zzRecipe;
    private AnyDouble _correlationXyRecipe;
    private AnyDouble _correlationXzRecipe;
    private AnyDouble _correlationYzRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPositionCovariance()
        : this(xx:            AnyValidVariance(),
               yy:            AnyValidVariance(),
               zz:            AnyValidVariance(),
               correlationXy: AnyValidCorrelation(),
               correlationXz: AnyValidCorrelation(),
               correlationYz: AnyValidCorrelation()) { }

    private static AnyDouble AnyValidVariance() {
        return Any.Double().Between(1e-4, PositionCovariance.MaximumSigmaKm * PositionCovariance.MaximumSigmaKm);
    }

    private static AnyDouble AnyValidCorrelation() {
        return Any.Double().Between(-MaximumCorrelation, MaximumCorrelation);
    }

    /// <summary>The widest correlation that keeps any draw positive definite.</summary>
    public const double MaximumCorrelation = 0.3;

    private AnyPositionCovariance(AnyDouble xx,
                                  AnyDouble yy,
                                  AnyDouble zz,
                                  AnyDouble correlationXy,
                                  AnyDouble correlationXz,
                                  AnyDouble correlationYz) {
        _xx                  = xx;
        _xxRecipe            = xx;
        _yy                  = yy;
        _yyRecipe            = yy;
        _zz                  = zz;
        _zzRecipe            = zz;
        _correlationXy       = correlationXy;
        _correlationXyRecipe = correlationXy;
        _correlationXz       = correlationXz;
        _correlationXzRecipe = correlationXz;
        _correlationYz       = correlationYz;
        _correlationYzRecipe = correlationYz;
    }

    private AnyPositionCovariance(AnyPositionCovariance from) {
        _xx                  = from._xx;
        _xxRecipe            = from._xxRecipe;
        _yy                  = from._yy;
        _yyRecipe            = from._yyRecipe;
        _zz                  = from._zz;
        _zzRecipe            = from._zzRecipe;
        _correlationXy       = from._correlationXy;
        _correlationXyRecipe = from._correlationXyRecipe;
        _correlationXz       = from._correlationXz;
        _correlationXzRecipe = from._correlationXzRecipe;
        _correlationYz       = from._correlationYz;
        _correlationYzRecipe = from._correlationYzRecipe;
    }

    /// <summary>Pins <c>xx</c>.</summary>
    public AnyPositionCovariance WithXx(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xx = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>xx</c> is drawn from.</summary>
    public AnyPositionCovariance WithXx(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._xxRecipe = configure(_xxRecipe);
        copy._xx       = copy._xxRecipe;

        return copy;
    }

    /// <summary>Pins <c>yy</c>.</summary>
    public AnyPositionCovariance WithYy(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._yy = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>yy</c> is drawn from.</summary>
    public AnyPositionCovariance WithYy(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._yyRecipe = configure(_yyRecipe);
        copy._yy       = copy._yyRecipe;

        return copy;
    }

    /// <summary>Pins <c>zz</c>.</summary>
    public AnyPositionCovariance WithZz(double value) {
        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._zz = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>zz</c> is drawn from.</summary>
    public AnyPositionCovariance WithZz(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._zzRecipe = configure(_zzRecipe);
        copy._zz       = copy._zzRecipe;

        return copy;
    }

    /// <summary>Pins the three correlations (xy, xz, yz) to one value, for example 0 for an uncorrelated matrix.</summary>
    public AnyPositionCovariance WithCorrelation(double value) {
        if (Math.Abs(value) > MaximumCorrelation) {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Beyond this a draw is no longer guaranteed positive definite.");
        }

        AnyPositionCovariance copy = new AnyPositionCovariance(this);

        copy._correlationXy = Any.Fixed(value);
        copy._correlationXz = Any.Fixed(value);
        copy._correlationYz = Any.Fixed(value);

        return copy;
    }

    /// <summary>Gives the three variances the same value: an isotropic uncertainty.</summary>
    public AnyPositionCovariance WithVariance(double value) {
        return WithXx(value).WithYy(value).WithZz(value);
    }

    /// <summary>Produces one arbitrary <see cref="PositionCovariance" />.</summary>
    public PositionCovariance Generate() {
        double xx = _xx.Generate();
        double yy = _yy.Generate();
        double zz = _zz.Generate();

        return new PositionCovariance(xx,
                                      yy,
                                      zz,
                                      _correlationXy.Generate() * Math.Sqrt(xx * yy),
                                      _correlationXz.Generate() * Math.Sqrt(xx * zz),
                                      _correlationYz.Generate() * Math.Sqrt(yy * zz));
    }

}

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate EnergyPerformanceDiagnosis --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
//
// Hand-edited: the declared rating must equal the rating the two figures give, a cross-field rule the
// scaffolded shape (one independent generator per parameter) cannot hold. The rating is drawn first and
// the figures are drawn inside its bands in Generate(); the scaffolded WithPrimaryEnergy… and
// WithEmissions… overloads were removed, because a figure pinned on its own breaks the rule.

using System;

using JustDummies;

namespace RealEstate.Domain;

/// <summary>
///     A generator of arbitrary <see cref="EnergyPerformanceDiagnosis" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEnergyPerformanceDiagnosis : IAny<EnergyPerformanceDiagnosis> {

    // Upper bounds (exclusive) of each band, A to F; G is open-ended up to a plausible ceiling.
    private static readonly int[] EnergyBands    = [70, 110, 180, 250, 330, 420, 1_000];
    private static readonly int[] EmissionsBands = [6, 11, 30, 50, 70, 100, 300];

    private readonly IAny<EnergyRating> _declaredRating;
    private readonly IAny<DateOnly>     _issuedOn;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEnergyPerformanceDiagnosis()
        : this(declaredRating: AnyValidDeclaredRating(),
               issuedOn:       AnyValidIssuedOn()) { }

    private static AnyEnum<EnergyRating> AnyValidDeclaredRating() {
        return Any.Enum<EnergyRating>();
    }

    private static AnyDateOnly AnyValidIssuedOn() {
        return Any.DateOnly()
                  .AfterOrEqualTo(EnergyPerformanceDiagnosis.ReformDate)
                  .BeforeOrEqualTo(EnergyPerformanceDiagnosis.LatestIssueDate);
    }

    private AnyEnergyPerformanceDiagnosis(IAny<EnergyRating> declaredRating,
                                          IAny<DateOnly>     issuedOn) {
        _declaredRating = declaredRating;
        _issuedOn       = issuedOn;
    }

    /// <summary>Pins <c>declaredRating</c> to a fixed value.</summary>
    public AnyEnergyPerformanceDiagnosis WithDeclaredRating(EnergyRating value) {
        return new AnyEnergyPerformanceDiagnosis(new FixedValue<EnergyRating>(value), _issuedOn);
    }

    /// <summary>Draws <c>declaredRating</c> from <c>AnyValidDeclaredRating()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEnergyPerformanceDiagnosis WithDeclaredRating(Func<AnyEnum<EnergyRating>, AnyEnum<EnergyRating>> configure) {
        return new AnyEnergyPerformanceDiagnosis(configure(AnyValidDeclaredRating()), _issuedOn);
    }

    /// <summary>Pins <c>issuedOn</c> to a fixed value.</summary>
    public AnyEnergyPerformanceDiagnosis WithIssuedOn(DateOnly value) {
        return new AnyEnergyPerformanceDiagnosis(_declaredRating, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>issuedOn</c> from <c>AnyValidIssuedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEnergyPerformanceDiagnosis WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyEnergyPerformanceDiagnosis(_declaredRating, configure(AnyValidIssuedOn()));
    }

    /// <summary>Produces one arbitrary <see cref="EnergyPerformanceDiagnosis" />.</summary>
    public EnergyPerformanceDiagnosis Generate() {
        EnergyRating rating = _declaredRating.Generate();
        int          band   = (int)rating;

        // The rating is the worse of the two bands: one figure sits in the rating's band, the other
        // anywhere at or below it.
        bool energyDecides = Any.Boolean().Generate();
        int  energy        = energyDecides ? InBand(EnergyBands, band) : AtOrBelow(EnergyBands, band);
        int  emissions     = energyDecides ? AtOrBelow(EmissionsBands, band) : InBand(EmissionsBands, band);

        return new EnergyPerformanceDiagnosis(energy, emissions, rating, _issuedOn.Generate());
    }

    private static int InBand(int[] bands, int band) {
        int floor = band == 0 ? 0 : bands[band - 1];
        return Any.Int32().GreaterThanOrEqualTo(floor).LessThan(bands[band]).Generate();
    }

    private static int AtOrBelow(int[] bands, int band) {
        return Any.Int32().GreaterThanOrEqualTo(0).LessThan(bands[band]).Generate();
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

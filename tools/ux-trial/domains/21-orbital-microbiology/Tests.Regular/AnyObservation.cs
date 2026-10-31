// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Observation --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Observation" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyObservation : IAny<Observation> {

    private readonly IAny<CultureId>         _cultureId;
    private readonly IAny<Species>           _species;
    private readonly IAny<double>            _elapsedHours;
    private readonly IAny<Concentration>     _measured;
    private readonly IAny<double>            _relativeUncertainty;
    private readonly IAny<ObservationMethod> _method;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyObservation()
        : this(cultureId:           AnyValidCultureId(),
               species:             AnyValidSpecies(),
               elapsedHours:        AnyValidElapsedHours(),
               measured:            AnyValidMeasured(),
               relativeUncertainty: AnyValidRelativeUncertainty(),
               method:              AnyValidMethod()) { }

    private static AnyCultureId AnyValidCultureId() {
        return new AnyCultureId();
    }

    private static AnySpecies AnyValidSpecies() {
        return new AnySpecies();
    }

    private static AnyDouble AnyValidElapsedHours() {
        return Any.Double().GreaterThanOrEqualTo(0);
    }

    private static AnyConcentration AnyValidMeasured() {
        return new AnyConcentration();
    }

    private static AnyDouble AnyValidRelativeUncertainty() {
        // Optical density cannot claim less than 10 %, so the base starts there and every method accepts it.
        return Any.Double().Between(Observation.MinIndirectRelativeUncertainty, 1);
    }

    private static AnyEnum<ObservationMethod> AnyValidMethod() {
        return Any.Enum<ObservationMethod>();
    }

    private AnyObservation(IAny<CultureId>         cultureId,
                           IAny<Species>           species,
                           IAny<double>            elapsedHours,
                           IAny<Concentration>     measured,
                           IAny<double>            relativeUncertainty,
                           IAny<ObservationMethod> method) {
        _cultureId           = cultureId;
        _species             = species;
        _elapsedHours        = elapsedHours;
        _measured            = measured;
        _relativeUncertainty = relativeUncertainty;
        _method              = method;
    }

    /// <summary>Pins <c>cultureId</c> to a fixed value.</summary>
    public AnyObservation WithCultureId(CultureId value) {
        return new AnyObservation(new FixedValue<CultureId>(value), _species, _elapsedHours, _measured, _relativeUncertainty, _method);
    }

    /// <summary>Draws <c>cultureId</c> from <c>AnyValidCultureId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyObservation WithCultureId(Func<AnyCultureId, AnyCultureId> configure) {
        return new AnyObservation(configure(AnyValidCultureId()), _species, _elapsedHours, _measured, _relativeUncertainty, _method);
    }

    /// <summary>Pins <c>species</c> to a fixed value.</summary>
    public AnyObservation WithSpecies(Species value) {
        return new AnyObservation(_cultureId, new FixedValue<Species>(value), _elapsedHours, _measured, _relativeUncertainty, _method);
    }

    /// <summary>Draws <c>species</c> from <c>AnyValidSpecies()</c>, refined by <paramref name="configure" />.</summary>
    public AnyObservation WithSpecies(Func<AnySpecies, AnySpecies> configure) {
        return new AnyObservation(_cultureId, configure(AnyValidSpecies()), _elapsedHours, _measured, _relativeUncertainty, _method);
    }

    /// <summary>Pins <c>elapsedHours</c> to a fixed value.</summary>
    public AnyObservation WithElapsedHours(double value) {
        return new AnyObservation(_cultureId, _species, new FixedValue<double>(value), _measured, _relativeUncertainty, _method);
    }

    /// <summary>Draws <c>elapsedHours</c> from <c>AnyValidElapsedHours()</c>, refined by <paramref name="configure" />.</summary>
    public AnyObservation WithElapsedHours(Func<AnyDouble, AnyDouble> configure) {
        return new AnyObservation(_cultureId, _species, configure(AnyValidElapsedHours()), _measured, _relativeUncertainty, _method);
    }

    /// <summary>Pins <c>measured</c> to a fixed value.</summary>
    public AnyObservation WithMeasured(Concentration value) {
        return new AnyObservation(_cultureId, _species, _elapsedHours, new FixedValue<Concentration>(value), _relativeUncertainty, _method);
    }

    /// <summary>Draws <c>measured</c> from <c>AnyValidMeasured()</c>, refined by <paramref name="configure" />.</summary>
    public AnyObservation WithMeasured(Func<AnyConcentration, AnyConcentration> configure) {
        return new AnyObservation(_cultureId, _species, _elapsedHours, configure(AnyValidMeasured()), _relativeUncertainty, _method);
    }

    /// <summary>Pins <c>relativeUncertainty</c> to a fixed value.</summary>
    public AnyObservation WithRelativeUncertainty(double value) {
        return new AnyObservation(_cultureId, _species, _elapsedHours, _measured, new FixedValue<double>(value), _method);
    }

    /// <summary>Draws <c>relativeUncertainty</c> from <c>AnyValidRelativeUncertainty()</c>, refined by <paramref name="configure" />.</summary>
    public AnyObservation WithRelativeUncertainty(Func<AnyDouble, AnyDouble> configure) {
        return new AnyObservation(_cultureId, _species, _elapsedHours, _measured, configure(AnyValidRelativeUncertainty()), _method);
    }

    /// <summary>Pins <c>method</c> to a fixed value.</summary>
    public AnyObservation WithMethod(ObservationMethod value) {
        return new AnyObservation(_cultureId, _species, _elapsedHours, _measured, _relativeUncertainty, new FixedValue<ObservationMethod>(value));
    }

    /// <summary>Draws <c>method</c> from <c>AnyValidMethod()</c>, refined by <paramref name="configure" />.</summary>
    public AnyObservation WithMethod(Func<AnyEnum<ObservationMethod>, AnyEnum<ObservationMethod>> configure) {
        return new AnyObservation(_cultureId, _species, _elapsedHours, _measured, _relativeUncertainty, configure(AnyValidMethod()));
    }

    /// <summary>Produces one arbitrary <see cref="Observation" />.</summary>
    public Observation Generate() {
        return new Observation(_cultureId.Generate(),
                               _species.Generate(),
                               _elapsedHours.Generate(),
                               _measured.Generate(),
                               _relativeUncertainty.Generate(),
                               _method.Generate());
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

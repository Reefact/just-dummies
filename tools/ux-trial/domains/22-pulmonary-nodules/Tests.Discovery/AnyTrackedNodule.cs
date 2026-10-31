// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TrackedNodule --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TrackedNodule" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTrackedNodule : IAny<TrackedNodule> {

    private readonly IAny<Guid>              _id;
    private readonly IAny<string>            _patientId;
    private readonly IAny<NoduleObservation> _baseline;
    private readonly IAny<double>            _matchToleranceMm;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTrackedNodule()
        : this(id:               AnyValidId(),
               patientId:        AnyValidPatientId(),
               baseline:         AnyValidBaseline(),
               matchToleranceMm: AnyValidMatchToleranceMm()) { }

    private static AnyGuid AnyValidId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyString AnyValidPatientId() {
        return Any.String().NotBlank().WithMaxLength(64);
    }

    private static AnyNoduleObservation AnyValidBaseline() {
        return new AnyNoduleObservation();
    }

    private static AnyDouble AnyValidMatchToleranceMm() {
        return Any.Double().Between(TrackedNodule.MinMatchToleranceMm, TrackedNodule.MaxMatchToleranceMm);
    }

    private AnyTrackedNodule(IAny<Guid>              id,
                             IAny<string>            patientId,
                             IAny<NoduleObservation> baseline,
                             IAny<double>            matchToleranceMm) {
        _id               = id;
        _patientId        = patientId;
        _baseline         = baseline;
        _matchToleranceMm = matchToleranceMm;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyTrackedNodule WithId(Guid value) {
        return new AnyTrackedNodule(new FixedValue<Guid>(value), _patientId, _baseline, _matchToleranceMm);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrackedNodule WithId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyTrackedNodule(configure(AnyValidId()), _patientId, _baseline, _matchToleranceMm);
    }

    /// <summary>Pins <c>patientId</c> to a fixed value.</summary>
    public AnyTrackedNodule WithPatientId(string value) {
        return new AnyTrackedNodule(_id, new FixedValue<string>(value), _baseline, _matchToleranceMm);
    }

    /// <summary>Draws <c>patientId</c> from <c>AnyValidPatientId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrackedNodule WithPatientId(Func<AnyString, AnyString> configure) {
        return new AnyTrackedNodule(_id, configure(AnyValidPatientId()), _baseline, _matchToleranceMm);
    }

    /// <summary>Pins <c>baseline</c> to a fixed value.</summary>
    public AnyTrackedNodule WithBaseline(NoduleObservation value) {
        return new AnyTrackedNodule(_id, _patientId, new FixedValue<NoduleObservation>(value), _matchToleranceMm);
    }

    /// <summary>Draws <c>baseline</c> from <c>AnyValidBaseline()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrackedNodule WithBaseline(Func<AnyNoduleObservation, AnyNoduleObservation> configure) {
        return new AnyTrackedNodule(_id, _patientId, configure(AnyValidBaseline()), _matchToleranceMm);
    }

    /// <summary>Pins <c>matchToleranceMm</c> to a fixed value.</summary>
    public AnyTrackedNodule WithMatchToleranceMm(double value) {
        return new AnyTrackedNodule(_id, _patientId, _baseline, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>matchToleranceMm</c> from <c>AnyValidMatchToleranceMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTrackedNodule WithMatchToleranceMm(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTrackedNodule(_id, _patientId, _baseline, configure(AnyValidMatchToleranceMm()));
    }

    /// <summary>Produces one arbitrary <see cref="TrackedNodule" />.</summary>
    public TrackedNodule Generate() {
        return new TrackedNodule(_id.Generate(),
                                 _patientId.Generate(),
                                 _baseline.Generate(),
                                 _matchToleranceMm.Generate());
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

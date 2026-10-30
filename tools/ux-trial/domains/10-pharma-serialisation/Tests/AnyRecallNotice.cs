// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RecallNotice --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Serialisation.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RecallNotice" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRecallNotice : IAny<RecallNotice> {

    private readonly IAny<BatchNumber> _batch;
    private readonly IAny<DateOnly>    _issuedOn;
    private readonly IAny<DateOnly>    _effectiveFrom;
    private readonly IAny<RecallClass> _recallClass;
    private readonly IAny<string>      _reason;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRecallNotice()
        : this(batch:         AnyValidBatch(),
               issuedOn:      AnyValidIssuedOn(),
               effectiveFrom: AnyValidEffectiveFrom(),
               recallClass:   AnyValidRecallClass(),
               reason:        AnyValidReason()) { }

    private static AnyBatchNumber AnyValidBatch() {
        return new AnyBatchNumber();
    }

    private static AnyDateOnly AnyValidIssuedOn() {
        // Cross-field guard effectiveFrom >= issuedOn: issue is drawn before the earliest effective date below.
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31));
    }

    private static AnyDateOnly AnyValidEffectiveFrom() {
        // Cross-field guard effectiveFrom >= issuedOn: effect is drawn after the latest issue date above.
        return Any.DateOnly().Between(new DateOnly(2025, 1, 1), new DateOnly(2026, 12, 31));
    }

    private static AnyEnum<RecallClass> AnyValidRecallClass() {
        return Any.Enum<RecallClass>();
    }

    private static AnyString AnyValidReason() {
        return Any.String().NonEmpty().WithMaxLength(500);
    }

    private AnyRecallNotice(IAny<BatchNumber> batch,
                            IAny<DateOnly>    issuedOn,
                            IAny<DateOnly>    effectiveFrom,
                            IAny<RecallClass> recallClass,
                            IAny<string>      reason) {
        _batch         = batch;
        _issuedOn      = issuedOn;
        _effectiveFrom = effectiveFrom;
        _recallClass   = recallClass;
        _reason        = reason;
    }

    /// <summary>Pins <c>batch</c> to a fixed value.</summary>
    public AnyRecallNotice WithBatch(BatchNumber value) {
        return new AnyRecallNotice(new FixedValue<BatchNumber>(value), _issuedOn, _effectiveFrom, _recallClass, _reason);
    }

    /// <summary>Draws <c>batch</c> from <c>AnyValidBatch()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRecallNotice WithBatch(Func<AnyBatchNumber, AnyBatchNumber> configure) {
        return new AnyRecallNotice(configure(AnyValidBatch()), _issuedOn, _effectiveFrom, _recallClass, _reason);
    }

    /// <summary>Pins <c>issuedOn</c> to a fixed value.</summary>
    public AnyRecallNotice WithIssuedOn(DateOnly value) {
        return new AnyRecallNotice(_batch, new FixedValue<DateOnly>(value), _effectiveFrom, _recallClass, _reason);
    }

    /// <summary>Draws <c>issuedOn</c> from <c>AnyValidIssuedOn()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRecallNotice WithIssuedOn(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyRecallNotice(_batch, configure(AnyValidIssuedOn()), _effectiveFrom, _recallClass, _reason);
    }

    /// <summary>Pins <c>effectiveFrom</c> to a fixed value.</summary>
    public AnyRecallNotice WithEffectiveFrom(DateOnly value) {
        return new AnyRecallNotice(_batch, _issuedOn, new FixedValue<DateOnly>(value), _recallClass, _reason);
    }

    /// <summary>Draws <c>effectiveFrom</c> from <c>AnyValidEffectiveFrom()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRecallNotice WithEffectiveFrom(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyRecallNotice(_batch, _issuedOn, configure(AnyValidEffectiveFrom()), _recallClass, _reason);
    }

    /// <summary>Pins <c>recallClass</c> to a fixed value.</summary>
    public AnyRecallNotice WithRecallClass(RecallClass value) {
        return new AnyRecallNotice(_batch, _issuedOn, _effectiveFrom, new FixedValue<RecallClass>(value), _reason);
    }

    /// <summary>Draws <c>recallClass</c> from <c>AnyValidRecallClass()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRecallNotice WithRecallClass(Func<AnyEnum<RecallClass>, AnyEnum<RecallClass>> configure) {
        return new AnyRecallNotice(_batch, _issuedOn, _effectiveFrom, configure(AnyValidRecallClass()), _reason);
    }

    /// <summary>Pins <c>reason</c> to a fixed value.</summary>
    public AnyRecallNotice WithReason(string value) {
        return new AnyRecallNotice(_batch, _issuedOn, _effectiveFrom, _recallClass, new FixedValue<string>(value));
    }

    /// <summary>Draws <c>reason</c> from <c>AnyValidReason()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRecallNotice WithReason(Func<AnyString, AnyString> configure) {
        return new AnyRecallNotice(_batch, _issuedOn, _effectiveFrom, _recallClass, configure(AnyValidReason()));
    }

    /// <summary>Produces one arbitrary <see cref="RecallNotice" />.</summary>
    public RecallNotice Generate() {
        return new RecallNotice(_batch.Generate(),
                                _issuedOn.Generate(),
                                _effectiveFrom.Generate(),
                                _recallClass.Generate(),
                                _reason.Generate());
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

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CreditSerial --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CreditSerial" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCreditSerial : IAny<CreditSerial> {

    private readonly IAny<string> _registry;
    private readonly IAny<int>    _projectId;
    private readonly IAny<int>    _vintage;
    private readonly IAny<long>   _unitNumber;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCreditSerial()
        : this(registry:   AnyValidRegistry(),
               projectId:  AnyValidProjectId(),
               vintage:    AnyValidVintage(),
               unitNumber: AnyValidUnitNumber()) { }

    private static AnyPattern AnyValidRegistry() {
        // A registry code is 2 to 5 upper-case ASCII letters.
        return Any.StringMatching("[A-Z]{2,5}");
    }

    private static AnyInt32 AnyValidProjectId() {
        return Any.Int32().Positive().LessThanOrEqualTo(999999);
    }

    private static AnyInt32 AnyValidVintage() {
        return Any.Int32().Between(1990, 2100);
    }

    private static AnyInt64 AnyValidUnitNumber() {
        return Any.Int64().Positive().LessThanOrEqualTo(999999999999);
    }

    private AnyCreditSerial(IAny<string> registry,
                            IAny<int>    projectId,
                            IAny<int>    vintage,
                            IAny<long>   unitNumber) {
        _registry   = registry;
        _projectId  = projectId;
        _vintage    = vintage;
        _unitNumber = unitNumber;
    }

    /// <summary>Pins <c>registry</c> to a fixed value.</summary>
    public AnyCreditSerial WithRegistry(string value) {
        return new AnyCreditSerial(new FixedValue<string>(value), _projectId, _vintage, _unitNumber);
    }

    /// <summary>Draws <c>registry</c> from <c>AnyValidRegistry()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditSerial WithRegistry(Func<AnyPattern, AnyPattern> configure) {
        return new AnyCreditSerial(configure(AnyValidRegistry()), _projectId, _vintage, _unitNumber);
    }

    /// <summary>Pins <c>projectId</c> to a fixed value.</summary>
    public AnyCreditSerial WithProjectId(int value) {
        return new AnyCreditSerial(_registry, new FixedValue<int>(value), _vintage, _unitNumber);
    }

    /// <summary>Draws <c>projectId</c> from <c>AnyValidProjectId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditSerial WithProjectId(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCreditSerial(_registry, configure(AnyValidProjectId()), _vintage, _unitNumber);
    }

    /// <summary>Pins <c>vintage</c> to a fixed value.</summary>
    public AnyCreditSerial WithVintage(int value) {
        return new AnyCreditSerial(_registry, _projectId, new FixedValue<int>(value), _unitNumber);
    }

    /// <summary>Draws <c>vintage</c> from <c>AnyValidVintage()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditSerial WithVintage(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCreditSerial(_registry, _projectId, configure(AnyValidVintage()), _unitNumber);
    }

    /// <summary>Pins <c>unitNumber</c> to a fixed value.</summary>
    public AnyCreditSerial WithUnitNumber(long value) {
        return new AnyCreditSerial(_registry, _projectId, _vintage, new FixedValue<long>(value));
    }

    /// <summary>Draws <c>unitNumber</c> from <c>AnyValidUnitNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCreditSerial WithUnitNumber(Func<AnyInt64, AnyInt64> configure) {
        return new AnyCreditSerial(_registry, _projectId, _vintage, configure(AnyValidUnitNumber()));
    }

    /// <summary>Produces one arbitrary <see cref="CreditSerial" />.</summary>
    public CreditSerial Generate() {
        return new CreditSerial(_registry.Generate(),
                                _projectId.Generate(),
                                _vintage.Generate(),
                                _unitNumber.Generate());
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

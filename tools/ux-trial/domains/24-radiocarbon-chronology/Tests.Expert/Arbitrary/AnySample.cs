// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Sample --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace RadiocarbonChronology.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Sample" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySample : IAny<Sample> {

    private readonly IAny<string>         _labCode;
    private readonly IAny<SampleMaterial> _material;
    private readonly IAny<RadiocarbonAge> _measurement;
    private readonly IAny<int>            _reservoirOffset;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySample()
        : this(labCode:         AnyValidLabCode(),
               material:        AnyValidMaterial(),
               measurement:     AnyValidMeasurement(),
               reservoirOffset: AnyValidReservoirOffset()) { }

    private static AnyPattern AnyValidLabCode() {
        // A laboratory code: two to five letters, a dash, three to six digits (OxA-12345).
        return Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}");
    }

    private static AnyEnum<SampleMaterial> AnyValidMaterial() {
        return Any.Enum<SampleMaterial>();
    }

    private static AnyRadiocarbonAge AnyValidMeasurement() {
        return new AnyRadiocarbonAge();
    }

    private static AnyInt32 AnyValidReservoirOffset() {
        // Only a marine shell may carry an offset, and never more than its measurement: zero is the one
        // offset valid for every material and every measurement. A marine test pins its own offset.
        return Any.Int32().Zero();
    }

    private AnySample(IAny<string>         labCode,
                      IAny<SampleMaterial> material,
                      IAny<RadiocarbonAge> measurement,
                      IAny<int>            reservoirOffset) {
        _labCode         = labCode;
        _material        = material;
        _measurement     = measurement;
        _reservoirOffset = reservoirOffset;
    }

    /// <summary>Pins <c>labCode</c> to a fixed value.</summary>
    public AnySample WithLabCode(string value) {
        return new AnySample(new FixedValue<string>(value), _material, _measurement, _reservoirOffset);
    }

    /// <summary>Draws <c>labCode</c> from <c>AnyValidLabCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnySample WithLabCode(Func<AnyPattern, AnyPattern> configure) {
        return new AnySample(configure(AnyValidLabCode()), _material, _measurement, _reservoirOffset);
    }

    /// <summary>Pins <c>material</c> to a fixed value.</summary>
    public AnySample WithMaterial(SampleMaterial value) {
        return new AnySample(_labCode, new FixedValue<SampleMaterial>(value), _measurement, _reservoirOffset);
    }

    /// <summary>Draws <c>material</c> from <c>AnyValidMaterial()</c>, refined by <paramref name="configure" />.</summary>
    public AnySample WithMaterial(Func<AnyEnum<SampleMaterial>, AnyEnum<SampleMaterial>> configure) {
        return new AnySample(_labCode, configure(AnyValidMaterial()), _measurement, _reservoirOffset);
    }

    /// <summary>Pins <c>measurement</c> to a fixed value.</summary>
    public AnySample WithMeasurement(RadiocarbonAge value) {
        return new AnySample(_labCode, _material, new FixedValue<RadiocarbonAge>(value), _reservoirOffset);
    }

    /// <summary>Draws <c>measurement</c> from <c>AnyValidMeasurement()</c>, refined by <paramref name="configure" />.</summary>
    public AnySample WithMeasurement(Func<AnyRadiocarbonAge, AnyRadiocarbonAge> configure) {
        return new AnySample(_labCode, _material, configure(AnyValidMeasurement()), _reservoirOffset);
    }

    /// <summary>Pins <c>reservoirOffset</c> to a fixed value.</summary>
    public AnySample WithReservoirOffset(int value) {
        return new AnySample(_labCode, _material, _measurement, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>reservoirOffset</c> from <c>AnyValidReservoirOffset()</c>, refined by <paramref name="configure" />.</summary>
    public AnySample WithReservoirOffset(Func<AnyInt32, AnyInt32> configure) {
        return new AnySample(_labCode, _material, _measurement, configure(AnyValidReservoirOffset()));
    }

    /// <summary>Produces one arbitrary <see cref="Sample" />.</summary>
    public Sample Generate() {
        return new Sample(_labCode.Generate(),
                          _material.Generate(),
                          _measurement.Generate(),
                          _reservoirOffset.Generate());
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

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Sample --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Sample" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySample : IAny<Sample> {

    private IAny<string>         _labCode;
    private IAny<SampleMaterial> _material;
    private IAny<RadiocarbonAge> _measurement;
    private IAny<int>            _reservoirOffset;

    private AnyPattern               _labCodeRecipe;
    private AnyEnum<SampleMaterial> _materialRecipe;
    private AnyRadiocarbonAge       _measurementRecipe;
    private AnyInt32                _reservoirOffsetRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySample()
        : this(labCode:         AnyValidLabCode(),
               material:        AnyValidMaterial(),
               measurement:     AnyValidMeasurement(),
               reservoirOffset: AnyValidReservoirOffset()) { }

    private static AnyPattern AnyValidLabCode() {
        return Any.StringMatching("[A-Z][A-Za-z]{1,4}-[0-9]{3,6}");
    }

    private static AnyEnum<SampleMaterial> AnyValidMaterial() {
        return Any.Enum<SampleMaterial>();
    }

    private static AnyRadiocarbonAge AnyValidMeasurement() {
        return new AnyRadiocarbonAge();
    }

    private static AnyInt32 AnyValidReservoirOffset() {
        return Any.Int32().Between(Sample.MinimumReservoirOffset, Sample.MaximumReservoirOffset);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnySample(AnyPattern               labCode,
                      AnyEnum<SampleMaterial> material,
                      AnyRadiocarbonAge       measurement,
                      AnyInt32                reservoirOffset) {
        _labCode               = labCode;
        _labCodeRecipe         = labCode;
        _material              = material;
        _materialRecipe        = material;
        _measurement           = measurement;
        _measurementRecipe     = measurement;
        _reservoirOffset       = reservoirOffset;
        _reservoirOffsetRecipe = reservoirOffset;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnySample(AnySample from) {
        _labCode               = from._labCode;
        _labCodeRecipe         = from._labCodeRecipe;
        _material              = from._material;
        _materialRecipe        = from._materialRecipe;
        _measurement           = from._measurement;
        _measurementRecipe     = from._measurementRecipe;
        _reservoirOffset       = from._reservoirOffset;
        _reservoirOffsetRecipe = from._reservoirOffsetRecipe;
    }

    /// <summary>Pins <c>labCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySample WithLabCode(string value) {
        AnySample copy = new AnySample(this);

        copy._labCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>labCode</c> is drawn from — <c>AnyValidLabCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySample WithLabCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySample copy = new AnySample(this);

        copy._labCodeRecipe = configure(_labCodeRecipe);
        copy._labCode       = copy._labCodeRecipe;

        return copy;
    }

    /// <summary>Pins <c>material</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySample WithMaterial(SampleMaterial value) {
        AnySample copy = new AnySample(this);

        copy._material = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>material</c> is drawn from — <c>AnyValidMaterial()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;SampleMaterial&gt;, AnyEnum&lt;SampleMaterial&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySample WithMaterial(Func<AnyEnum<SampleMaterial>, AnyEnum<SampleMaterial>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySample copy = new AnySample(this);

        copy._materialRecipe = configure(_materialRecipe);
        copy._material       = copy._materialRecipe;

        return copy;
    }

    /// <summary>Pins <c>measurement</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySample WithMeasurement(RadiocarbonAge value) {
        AnySample copy = new AnySample(this);

        copy._measurement = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>measurement</c> is drawn from — <c>AnyValidMeasurement()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRadiocarbonAge, AnyRadiocarbonAge&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySample WithMeasurement(Func<AnyRadiocarbonAge, AnyRadiocarbonAge> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySample copy = new AnySample(this);

        copy._measurementRecipe = configure(_measurementRecipe);
        copy._measurement       = copy._measurementRecipe;

        return copy;
    }

    /// <summary>Pins <c>reservoirOffset</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnySample WithReservoirOffset(int value) {
        AnySample copy = new AnySample(this);

        copy._reservoirOffset = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reservoirOffset</c> is drawn from — <c>AnyValidReservoirOffset()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnySample WithReservoirOffset(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnySample copy = new AnySample(this);

        copy._reservoirOffsetRecipe = configure(_reservoirOffsetRecipe);
        copy._reservoirOffset       = copy._reservoirOffsetRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Sample" />.</summary>
    public Sample Generate() {
        SampleMaterial  material    = _material.Generate();
        RadiocarbonAge  measurement = _measurement.Generate();
        int             offset      = _reservoirOffset.Generate();

        // The constructor ties the offset to the material (only a marine shell carries one) and to the measurement
        // (the corrected age stays within 0..MaximumYearsBp): no per-parameter recipe can say so.
        if (material != SampleMaterial.MarineShell) {
            offset = 0;
        } else {
            offset = Math.Max(Math.Min(offset, measurement.YearsBp), measurement.YearsBp - RadiocarbonAge.MaximumYearsBp);
        }

        return new Sample(_labCode.Generate(),
                          material,
                          measurement,
                          offset);
    }

}

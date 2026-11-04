// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PortRange --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PortRange" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPortRange : IAny<PortRange> {


    private AnyUInt16 _lowRecipe;
    private AnyUInt16 _highRecipe;

    private ushort? _lowPin;
    private ushort? _highPin;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPortRange()
        : this(low:  AnyValidLow(),
               high: AnyValidHigh()) { }

    private static AnyUInt16 AnyValidLow() {
        return Any.UInt16();
    }

    private static AnyUInt16 AnyValidHigh() {
        return Any.UInt16();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPortRange(AnyUInt16 low,
                         AnyUInt16 high) {
        _lowRecipe  = low;
        _highRecipe = high;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPortRange(AnyPortRange from) {
        _lowRecipe  = from._lowRecipe;
        _lowPin     = from._lowPin;
        _highRecipe = from._highRecipe;
        _highPin    = from._highPin;
    }

    /// <summary>Draws <c>low</c> and <c>high</c>, <c>low</c> never after <c>high</c> as the constructor requires: together when neither is pinned, one bounded by the other's pin, and two pins as they are, for the constructor to judge.</summary>
    private (ushort Low, ushort High) LowAndHigh() {
        if (_lowPin is { } low) { return (low, _highPin ?? _highRecipe.GreaterThanOrEqualTo(low).Generate()); }
        if (_highPin is { } high) { return (_lowRecipe.LessThanOrEqualTo(high).Generate(), high); }

        return Any.Pair(_lowRecipe, _highRecipe).Having.Item1LessThanOrEqualToItem2().Generate();
    }

    /// <summary>Pins <c>low</c> to a fixed value, which bounds <c>high</c>; its recipe is kept for a later refinement.</summary>
    public AnyPortRange WithLow(ushort value) {
        AnyPortRange copy = new AnyPortRange(this);

        copy._lowPin     = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>low</c> is drawn from — <c>AnyValidLow()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>high</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt16, AnyUInt16&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPortRange WithLow(Func<AnyUInt16, AnyUInt16> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPortRange copy = new AnyPortRange(this);

        copy._lowRecipe  = configure(_lowRecipe);
        copy._lowPin     = null;

        return copy;
    }

    /// <summary>Pins <c>high</c> to a fixed value, which bounds <c>low</c>; its recipe is kept for a later refinement.</summary>
    public AnyPortRange WithHigh(ushort value) {
        AnyPortRange copy = new AnyPortRange(this);

        copy._highPin    = value;

        return copy;
    }

    /// <summary>Refines the recipe <c>high</c> is drawn from — <c>AnyValidHigh()</c>, then each earlier refinement — through <paramref name="configure" />, in order with <c>low</c>.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyUInt16, AnyUInt16&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPortRange WithHigh(Func<AnyUInt16, AnyUInt16> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPortRange copy = new AnyPortRange(this);

        copy._highRecipe = configure(_highRecipe);
        copy._highPin    = null;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PortRange" />.</summary>
    public PortRange Generate() {
        (ushort low, ushort high) = LowAndHigh();

        return new PortRange(low,
                             high);
    }

}

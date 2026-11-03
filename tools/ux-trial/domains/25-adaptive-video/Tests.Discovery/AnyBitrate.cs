// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Bitrate --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Bitrate" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyBitrate : IAny<Bitrate> {

    private IAny<int> _kbps;

    private AnyInt32 _kbpsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyBitrate()
        : this(kbps: AnyValidKbps()) { }

    private static AnyInt32 AnyValidKbps() {
        return Any.Int32().Between(Bitrate.MinKbps, Bitrate.MaxKbps);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyBitrate(AnyInt32 kbps) {
        _kbps       = kbps;
        _kbpsRecipe = kbps;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyBitrate(AnyBitrate from) {
        _kbps       = from._kbps;
        _kbpsRecipe = from._kbpsRecipe;
    }

    /// <summary>Pins <c>kbps</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyBitrate WithKbps(int value) {
        AnyBitrate copy = new AnyBitrate(this);

        copy._kbps = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kbps</c> is drawn from — <c>AnyValidKbps()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyBitrate WithKbps(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBitrate copy = new AnyBitrate(this);

        copy._kbpsRecipe = configure(_kbpsRecipe);
        copy._kbps       = copy._kbpsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Bitrate" />.</summary>
    public Bitrate Generate() {
        return new Bitrate(_kbps.Generate());
    }

}

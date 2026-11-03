// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Voice --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Voice" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVoice : IAny<Voice> {

    private IAny<int>                       _number;
    private IAny<IReadOnlyList<VoiceEntry>> _entries;

    private AnyInt32            _numberRecipe;
    private AnyList<VoiceEntry> _entriesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVoice()
        : this(number:  AnyValidNumber(),
               entries: AnyValidEntries()) { }

    private static AnyInt32 AnyValidNumber() {
        return Any.Int32().Positive().LessThanOrEqualTo(Voice.MaxVoices);
    }

    private static AnyList<VoiceEntry> AnyValidEntries() {
        // Written music divides time by powers of two: that keeps the exact sum of the entries notatable.
        AnyVoiceEntry notatableEntry = new AnyVoiceEntry().WithDuration(d => d.WithDenominator(den => den.OneOf(1, 2, 4, 8, 16, 32, 64)));

        return Any.ListOf(notatableEntry).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVoice(AnyInt32            number,
                     AnyList<VoiceEntry> entries) {
        _number        = number;
        _numberRecipe  = number;
        _entries       = entries;
        _entriesRecipe = entries;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVoice(AnyVoice from) {
        _number        = from._number;
        _numberRecipe  = from._numberRecipe;
        _entries       = from._entries;
        _entriesRecipe = from._entriesRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVoice WithNumber(int value) {
        AnyVoice copy = new AnyVoice(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoice WithNumber(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoice copy = new AnyVoice(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>entries</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVoice WithEntries(IReadOnlyList<VoiceEntry> value) {
        AnyVoice copy = new AnyVoice(this);

        copy._entries = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>entries</c> is drawn from — <c>AnyValidEntries()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;VoiceEntry&gt;, AnyList&lt;VoiceEntry&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVoice WithEntries(Func<AnyList<VoiceEntry>, AnyList<VoiceEntry>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVoice copy = new AnyVoice(this);

        copy._entriesRecipe = configure(_entriesRecipe);
        copy._entries       = copy._entriesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Voice" />.</summary>
    public Voice Generate() {
        return new Voice(_number.Generate(),
                         _entries.Generate());
    }

}

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Score --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace MusicScore.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Score" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyScore : IAny<Score> {

    private IAny<string>               _title;
    private IAny<Tempo>                _tempo;
    private IAny<IReadOnlyList<Staff>> _staves;

    private AnyString      _titleRecipe;
    private AnyTempo       _tempoRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyScore()
        : this(title:  AnyValidTitle(),
               tempo:  AnyValidTempo(),
               staves: AnyValidStaves()) { }

    private static AnyString AnyValidTitle() {
        return Any.String().Printable().NotBlank().WithLengthBetween(1, 60);
    }

    private static AnyTempo AnyValidTempo() {
        return new AnyTempo();
    }

    private static IAny<IReadOnlyList<Staff>> AnyValidStaves() {
        // The staves of a score are tied together (names, measure count, meters, repeat barlines):
        // see AnyScore.Custom.cs.
        return new CoherentStaves();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyScore(AnyString                     title,
                     AnyTempo                      tempo,
                     IAny<IReadOnlyList<Staff>> staves) {
        _title        = title;
        _titleRecipe  = title;
        _tempo        = tempo;
        _tempoRecipe  = tempo;
        _staves       = staves;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyScore(AnyScore from) {
        _title        = from._title;
        _titleRecipe  = from._titleRecipe;
        _tempo        = from._tempo;
        _tempoRecipe  = from._tempoRecipe;
        _staves       = from._staves;
    }

    /// <summary>Pins <c>title</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyScore WithTitle(string value) {
        AnyScore copy = new AnyScore(this);

        copy._title = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>title</c> is drawn from — <c>AnyValidTitle()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyScore WithTitle(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyScore copy = new AnyScore(this);

        copy._titleRecipe = configure(_titleRecipe);
        copy._title       = copy._titleRecipe;

        return copy;
    }

    /// <summary>Pins <c>tempo</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyScore WithTempo(Tempo value) {
        AnyScore copy = new AnyScore(this);

        copy._tempo = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tempo</c> is drawn from — <c>AnyValidTempo()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTempo, AnyTempo&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyScore WithTempo(Func<AnyTempo, AnyTempo> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyScore copy = new AnyScore(this);

        copy._tempoRecipe = configure(_tempoRecipe);
        copy._tempo       = copy._tempoRecipe;

        return copy;
    }

    /// <summary>Pins <c>staves</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyScore WithStaves(IReadOnlyList<Staff> value) {
        AnyScore copy = new AnyScore(this);

        copy._staves = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Score" />.</summary>
    public Score Generate() {
        return new Score(_title.Generate(),
                         _tempo.Generate(),
                         _staves.Generate());
    }

}

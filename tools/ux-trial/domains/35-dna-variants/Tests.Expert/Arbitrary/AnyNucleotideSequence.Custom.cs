using System;

using JustDummies;

namespace DnaVariants.Domain;

// Written by hand, never touched by `any generate --force`.
public sealed partial class AnyNucleotideSequence {

    /// <summary>
    ///     Draws a sequence of called bases only (no N), refined by <paramref name="configure" />: what a test needs when
    ///     a variant must be able to state any position in an allele, since an allele cannot carry N.
    /// </summary>
    /// <remarks>
    ///     A refinement through <c>WithBases(b => b.WithChars("ACGTacgt"))</c> is refused: the base already declared
    ///     <c>WithChars("ACGTNacgtn")</c>, a pool is declared once, and no member subtracts arbitrary characters.
    ///     So this one replaces the base pool with a subset of it, which still honours the constructor's alphabet.
    /// </remarks>
    public AnyNucleotideSequence OfCalledBases(Func<AnyString, AnyString> configure) {
        return new AnyNucleotideSequence(configure(Any.String().WithChars("ACGTacgt").NonEmpty()));
    }

}

#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

[TestSubject(typeof(AnyString))]
public sealed class AnyStringTests {

    private const int SampleCount = 200;

    #region Statics members declarations

    private static IEnumerable<string> Samples(IAny<string> generator) {
        for (int i = 0; i < SampleCount; i++) {
            yield return generator.Generate();
        }
    }

    #endregion

    [Fact(DisplayName = "An unconstrained String yields 0 to 1024 characters drawn from the whole of ASCII.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void UnconstrainedYieldsAsciiUpToTheSpread() {
        HashSet<char> seen = [];
        foreach (string value in Samples(Any.String())) {
            Check.That(value.Length).IsLessOrEqualThan(1024);
            Check.That(value.All(CharacterPools.IsAscii)).IsTrue();
            foreach (char character in value) { seen.Add(character); }
        }

        // The point of the default (ADR-0075): a control character is exactly what an unconstrained draw may hand
        // the code under test, so what the test survives, it has been shown to tolerate.
        Check.That(seen.Any(CharacterPools.IsAsciiNonPrintable)).IsTrue();
    }

    [Fact(DisplayName = "NonEmpty yields at least one character.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void NonEmptyHasAtLeastOneCharacter() {
        foreach (string value in Samples(Any.String().NonEmpty())) {
            Check.That(value.Length).IsStrictlyGreaterThan(0);
        }
    }

    [Fact(DisplayName = "NotBlank yields a value the guard behind IsNullOrWhiteSpace accepts.")]
    public void NotBlankIsNeverAllWhitespace() {
        foreach (string value in Samples(Any.String().NotBlank().WithMaxLength(4))) {
            Check.That(string.IsNullOrWhiteSpace(value)).IsFalse();
        }
    }

    /// <summary>
    ///     The whole reason the member exists rather than <c>NonEmpty()</c> standing in for it: a short ceiling
    ///     makes an all-whitespace draw ordinary, and the four line and page breaks the <c>Whitespaces</c> family
    ///     does not name are two thirds of it.
    /// </summary>
    [Fact(DisplayName = "NonEmpty alone leaves the all-whitespace draw NotBlank rejects reachable.")]
    public void NonEmptyDoesNotRejectWhitespace() {
        HashSet<char> seen = [];
        foreach (string value in Samples(Any.String().NonEmpty().WithMaxLength(4))) {
            foreach (char character in value) { seen.Add(character); }
        }

        Check.That(seen.Any(character => CharacterPools.IsBlank(character) && !CharacterPools.IsAsciiWhitespace(character))).IsTrue();
    }

    [Fact(DisplayName = "NotBlank leaves interior whitespace legal — only an entirely blank value is refused.")]
    public void NotBlankAdmitsInteriorWhitespace() {
        Check.That(Samples(Any.String().NotBlank().WithLengthBetween(3, 8))
                   .Any(value => value.Skip(1).Take(value.Length - 2).Any(char.IsWhiteSpace)))
             .IsTrue();
    }

    [Fact(DisplayName = "NotBlank leans on an anchored literal that already carries a non-blank character.")]
    public void NotBlankAcceptsANonBlankAnchor() {
        foreach (string value in Samples(Any.String().StartingWith("A").NotBlank().WithLength(1))) {
            Check.That(value).IsEqualTo("A");
        }
    }

    [Fact(DisplayName = "NotBlank rescues a value whose every anchor is blank.")]
    public void NotBlankRescuesABlankAnchor() {
        foreach (string value in Samples(Any.String().StartingWith(" ").NotBlank().WithMaxLength(3))) {
            Check.That(string.IsNullOrWhiteSpace(value)).IsFalse();
        }
    }

    [Fact(DisplayName = "NotBlank names both sides when the declared family leaves only whitespace to draw.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void NotBlankConflictsWithTheWhitespacesFamily() {
        Check.ThatCode(() => Any.String().Whitespaces().NotBlank().Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because Whitespaces() leaves only whitespace to draw.");
    }

    [Fact(DisplayName = "NotBlank names both sides when the anchors fill the declared length.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void NotBlankConflictsWithAFullyAnchoredLength() {
        Check.ThatCode(() => Any.String().StartingWith(" ").WithLength(1).NotBlank().Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because the declared shape leaves no room for one.");
    }

    [Fact(DisplayName = "NotBlank blames the exhausted length rather than a family it never draws from.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void NotBlankBlamesTheShapeBeforeTheFamily() {
        // Both sides are unsatisfiable at once here. Dropping Whitespaces() would leave the chain just as refused,
        // so naming it would send the caller to a constraint whose departure changes nothing.
        Check.ThatCode(() => Any.String().StartingWith(" ").WithLength(1).Whitespaces().NotBlank().Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because the declared shape leaves no room for one.");
    }

    [Fact(DisplayName = "A contradiction is answered for once, whichever order the same constraints were written in.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void NotBlankRefusesAnEmptyAlphabetInEveryOrder() {
        // The diagnosis belongs to the constraint set, so the sentence has to be the same sentence every time --
        // not one message when the family came first and another when it came second.
        Check.ThatCode(() => Any.String().Whitespaces().NotBlank().Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because Whitespaces() leaves only whitespace to draw.");
        Check.ThatCode(() => Any.String().NotBlank().Whitespaces().Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because Whitespaces() leaves only whitespace to draw.");
    }

    [Theory(DisplayName = "An anchor satisfies NotBlank wherever in the chain it was declared.")]
    [MemberData(nameof(AnchorRescuesNotBlankCases))]
    public void AnchorRescuesNotBlankWhateverTheOrder(string because, char anchored) {
        // The six shapes an exhaustive permutation sweep of the string surface found order-sensitive: a filler
        // alphabet holding no non-blank character, NotBlank(), and an anchor that carries one. Declared before the
        // other two the anchor was honoured; declared after, the chain was refused -- the same constraint set,
        // answered two ways. A specification is answered for once it is whole, so all of these draw.
        foreach (string value in Samples(AnchorRescuesNotBlankChains[because]())) {
            Check.WithCustomMessage(because).That(value).Contains(anchored.ToString());
            Check.WithCustomMessage(because).That(value.Any(character => !char.IsWhiteSpace(character))).IsTrue();
        }
    }

    // Keyed by the case name rather than carried through the theory data: a Func is not serializable, and one that is
    // not makes Test Explorer collapse every case into a single row (xUnit1044).
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    private static readonly Dictionary<string, Func<AnyString>> AnchorRescuesNotBlankChains = new() {
        ["Whitespaces + prefix"] = () => Any.String().Whitespaces().NotBlank().StartingWith("A"),
        ["Whitespaces + suffix"] = () => Any.String().Whitespaces().NotBlank().EndingWith("Z"),
        ["Whitespaces + fragment"] = () => Any.String().Whitespaces().NotBlank().Containing("x"),
        ["WithChars + prefix"] = () => Any.String().WithChars(" \t").NotBlank().StartingWith("A"),
        ["WithChars + suffix"] = () => Any.String().WithChars(" \t").NotBlank().EndingWith("Z"),
        ["WithChars + fragment"] = () => Any.String().WithChars(" \t").NotBlank().Containing("x")
    };

    public static TheoryData<string, char> AnchorRescuesNotBlankCases() {
        return new TheoryData<string, char> {
            { "Whitespaces + prefix", 'A' },
            { "Whitespaces + suffix", 'Z' },
            { "Whitespaces + fragment", 'x' },
            { "WithChars + prefix", 'A' },
            { "WithChars + suffix", 'Z' },
            { "WithChars + fragment", 'x' },
        };
    }

    [Fact(DisplayName = "NotBlank draws the same values whichever order its chain was written in.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void NotBlankIsIndifferentToDeclarationOrder() {
        // The three orders that reach a value must reach the SAME values, not merely all succeed: an anchor that
        // arrives late has to constrain the draw exactly as one that arrived early.
        foreach (Func<AnyString> chain in new Func<AnyString>[] {
                     () => Any.String().StartingWith("A").Whitespaces().NotBlank(),
                     () => Any.String().Whitespaces().StartingWith("A").NotBlank(),
                     () => Any.String().Whitespaces().NotBlank().StartingWith("A"),
                 }) {
            foreach (string value in Samples(chain())) {
                Check.That(value[0]).IsEqualTo('A');
                Check.That(value.Skip(1).All(char.IsWhiteSpace)).IsTrue();
            }
        }
    }

    [Fact(DisplayName = "WithLength yields exactly that many characters.")]
    public void WithLengthIsExact() {
        foreach (string value in Samples(Any.String().WithLength(10))) {
            Check.That(value.Length).IsEqualTo(10);
        }
    }

    [Fact(DisplayName = "WithLength(0) yields the empty string.")]
    public void WithLengthZeroIsEmpty() {
        Check.That(Any.String().WithLength(0).Generate()).IsEqualTo(string.Empty);
    }

    [Fact(DisplayName = "WithMinLength and WithMaxLength bound the length inclusively.")]
    [SuppressMessage(JustDummiesRule.JD031.Category, JustDummiesRule.JD031.Id, Justification = SuppressionJustification.JD031.SeparateBoundsAreTheSubject)]
    public void MinAndMaxLengthAreInclusiveBounds() {
        foreach (string value in Samples(Any.String().WithMinLength(3).WithMaxLength(5))) {
            Check.That(value.Length).IsGreaterOrEqualThan(3);
            Check.That(value.Length).IsLessOrEqualThan(5);
        }
    }

    [Fact(DisplayName = "WithLengthBetween bounds the length inclusively and reaches its bounds.")]
    public void WithLengthBetweenIsInclusive() {
        HashSet<int> lengths = [];
        foreach (string value in Samples(Any.String().WithLengthBetween(2, 4))) {
            lengths.Add(value.Length);
            Check.That(value.Length).IsGreaterOrEqualThan(2);
            Check.That(value.Length).IsLessOrEqualThan(4);
        }

        Check.That(lengths.Contains(2)).IsTrue();
        Check.That(lengths.Contains(4)).IsTrue();
    }

    [Fact(DisplayName = "StartingWith anchors the prefix.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void StartingWithAnchorsThePrefix() {
        foreach (string value in Samples(Any.String().StartingWith("ORD-"))) {
            Check.That(value).StartsWith("ORD-");
        }
    }

    [Fact(DisplayName = "EndingWith anchors the suffix.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void EndingWithAnchorsTheSuffix() {
        foreach (string value in Samples(Any.String().EndingWith("-FR"))) {
            Check.That(value).EndsWith("-FR");
        }
    }

    [Fact(DisplayName = "Containing embeds the value.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void ContainingEmbedsTheValue() {
        foreach (string value in Samples(Any.String().Containing("ABC"))) {
            Check.That(value).Contains("ABC");
        }
    }

    [Fact(DisplayName = "Prefix, contained value, suffix and exact length hold together.")]
    public void FragmentsAndExactLengthHoldTogether() {
        foreach (string value in Samples(Any.String().StartingWith("ORD-").Containing("X").EndingWith("-FR").WithLength(12))) {
            Check.That(value.Length).IsEqualTo(12);
            Check.That(value).StartsWith("ORD-");
            Check.That(value).Contains("X");
            Check.That(value).EndsWith("-FR");
        }
    }

    [Fact(DisplayName = "A fragment-only budget is generable: length equals the fragment sum.")]
    public void FragmentsExactlyFillingTheLengthAreGenerable() {
        Check.That(Any.String().StartingWith("AB").EndingWith("CD").WithLength(4).Generate()).IsEqualTo("ABCD");
    }

    [Fact(DisplayName = "Containing: an identical fragment redeclared is a no-op, on the shaped path and the pooled one alike (issue #181).")]
    [SuppressMessage(JustDummiesRule.JD035.Category, JustDummiesRule.JD035.Id, Justification = SuppressionJustification.JD035.RepetitionIsTheSubject)]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void IdenticalFragmentRedeclaredIsANoOp() {
        // Before the fix, two identical "ab" fragments cost 4 characters on the shaped path (RequiredLength summed
        // both) but only 1 on the pooled path (DeclaredConstraints groups by the same ConstraintCall) — a budget that
        // disagreed with itself depending on whether a value set was also in force.
        Check.That(Any.String().Containing("ab").Containing("ab").WithLength(2).Generate()).IsEqualTo("ab");

        foreach (string value in Samples(Any.String().Containing("ab").Containing("ab"))) {
            Check.That(value).Contains("ab");
        }

        Check.That(Any.String().OneOf("ab").Containing("ab").Containing("ab").Generate()).IsEqualTo("ab");
        Check.That(Any.String().Containing("ab").Containing("ab").OneOf("ab").Generate()).IsEqualTo("ab");
    }

    [Fact(DisplayName = "Containing: two DIFFERENT fragments keep the concatenated budget — issue #137's refusal is unaffected by #181's fix.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void DifferentFragmentsKeepTheConcatenatedBudget() {
        Check.ThatCode(() => Any.String().Containing("ab").Containing("ba").WithLength(3))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("\"ab\"", "\"ba\"");

        Check.That(Any.String().Containing("ab").Containing("ba").WithLength(4).Generate()).IsEqualTo("abba");
    }

    [Fact(DisplayName = "Containing: two fragments whose diagnostics render alike stay two requirements — CharacterPools.Escape is not injective (PR #197 review).")]
    public void FragmentsThatRenderAlikeAreNotFoldedTogether() {
        // "\n" (one newline character) and "\\n" (a literal backslash then 'n', two characters) both escape to the
        // same diagnostic text Containing("\n"), so comparing the rendered ConstraintCall instead of the fragment
        // itself would fold two DIFFERENT requirements into one and silently drop the second.
        string newline       = "\n";
        string backslashThenN = "\\n";
        Check.That(newline).Not.IsEqualTo(backslashThenN);

        Check.ThatCode(() => Any.String().Containing(newline).Containing(backslashThenN).WithLength(1))
             .Throws<ConflictingAnyConstraintException>();

        string value = Any.String().Containing(newline).Containing(backslashThenN).WithLength(3).Generate();
        Check.That(value).Contains(newline);
        Check.That(value).Contains(backslashThenN);
    }

    [Fact(DisplayName = "Alpha yields ASCII letters only.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void AlphaYieldsLettersOnly() {
        foreach (string value in Samples(Any.String().Alpha().NonEmpty())) {
            Check.That(value.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')).IsTrue();
        }
    }

    [Fact(DisplayName = "Numeric yields ASCII digits only.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void NumericYieldsDigitsOnly() {
        foreach (string value in Samples(Any.String().Numeric().NonEmpty())) {
            Check.That(value.All(character => character is >= '0' and <= '9')).IsTrue();
        }
    }

    [Fact(DisplayName = "AlphaNumeric yields ASCII letters and digits only.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void AlphaNumericYieldsLettersAndDigitsOnly() {
        foreach (string value in Samples(Any.String().AlphaNumeric().NonEmpty())) {
            Check.That(value.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')).IsTrue();
        }
    }

    [Fact(DisplayName = "Punctuation yields printable non-alphanumerics only.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void PunctuationYieldsPrintableNonAlphaNumericsOnly() {
        foreach (string value in Samples(Any.String().Punctuation().NonEmpty())) {
            Check.That(value.All(CharacterPools.IsAsciiPunctuation)).IsTrue();
        }
    }

    [Fact(DisplayName = "Printable yields printable ASCII only, and actually reaches punctuation.")]
    public void PrintableYieldsPrintableAsciiOnly() {
        HashSet<char> seen = [];
        foreach (string value in Samples(Any.String().Printable().WithLength(16))) {
            Check.That(value.All(CharacterPools.IsAsciiPrintable)).IsTrue();
            foreach (char character in value) { seen.Add(character); }
        }

        // The point of the family: a filler that stops at letters and digits is what sent a caller looking for a
        // pool of their own in the first place.
        Check.That(seen.Any(CharacterPools.IsAsciiPunctuation)).IsTrue();
    }

    [Fact(DisplayName = "Printable admits a punctuated fragment, which the narrower families refuse.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void PrintableAdmitsAPunctuatedFragment() {
        foreach (string value in Samples(Any.String().Printable().StartingWith("ORD-").NonEmpty())) {
            Check.That(value).StartsWith("ORD-");
        }
    }

    [Fact(DisplayName = "WithoutAlpha yields no letter; WithoutAlpha().WithoutNumeric() yields neither a letter nor a digit.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void WithoutAlphaAndWithoutNumericSubtractFromTheFiller() {
        foreach (string value in Samples(Any.String().WithoutAlpha().NonEmpty())) {
            Check.That(value.Any(char.IsLetter)).IsFalse();
        }

        foreach (string value in Samples(Any.String().WithoutAlpha().WithoutNumeric().NonEmpty())) {
            Check.That(value.Any(char.IsLetterOrDigit)).IsFalse();
        }
    }

    [Fact(DisplayName = "WithoutAlpha keeps a contained value holding a letter: a subtraction governs the filler alone.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void WithoutAlphaKeepsAContainedValueHoldingALetter() {
        foreach (string value in Samples(Any.String().WithoutAlpha().Containing("abc").WithLengthBetween(6, 12))) {
            Check.That(value).Contains("abc");
            // The filler carries no letter, so the first occurrence is the literal itself: removing it leaves
            // exactly what was drawn, and the subtraction answers for all of that.
            Check.That(value.Remove(value.IndexOf("abc", StringComparison.Ordinal), 3).Any(char.IsLetter)).IsFalse();
        }
    }

    [Fact(DisplayName = "InLowerCase yields no uppercase letter; digits stay allowed.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void LowerCaseForbidsUppercaseLetters() {
        foreach (string value in Samples(Any.String().InLowerCase().NonEmpty())) {
            Check.That(value.Any(character => character is >= 'A' and <= 'Z')).IsFalse();
        }
    }

    [Fact(DisplayName = "InUpperCase draws no lowercase letter, and keeps a lowercase literal as written.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void UpperCaseForbidsLowercaseLetters() {
        foreach (string value in Samples(Any.String().InUpperCase().StartingWith("ord-").WithLengthBetween(6, 12))) {
            Check.That(value).StartsWith("ord-");
            Check.That(value.Substring(4).Any(character => character is >= 'a' and <= 'z')).IsFalse();
        }
    }

    // A character constraint governs what the generator DRAWS; a literal fixed by StartingWith, EndingWith or
    // Containing is not drawn, and is therefore exempt (ADR-0079). The cases below are the formats that could not be
    // written before: a fixed separator in the prefix, and a body the family still governs on its own.

    [Fact(DisplayName = "AlphaNumeric with an anchored prefix draws the separator nowhere but the prefix.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void AlphaNumericWithAPrefixDrawsTheSeparatorNowhereElse() {
        foreach (string value in Samples(Any.String().AlphaNumeric().StartingWith("ORD-").WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.IndexOf('-', 4)).IsEqualTo(-1);
            Check.That(value.Substring(4).All(char.IsLetterOrDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "An order reference keeps its four rules as named calls: prefix, family, casing and length.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void AnOrderReferenceKeepsItsFourRulesNamed() {
        foreach (string value in Samples(Any.String().StartingWith("ORD-").AlphaNumeric().InUpperCase().WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Length).IsGreaterOrEqualThan(8);
            Check.That(value.Length).IsLessOrEqualThan(20);
            Check.That(value.Substring(4).All(character => char.IsUpper(character) || char.IsDigit(character))).IsTrue();
        }
    }

    [Fact(DisplayName = "A family and an anchored suffix compose the same way whichever is declared first.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void AFamilyAndASuffixAreOrderIndependent() {
        foreach (AnyString generator in new[] { Any.String().Alpha().EndingWith("-42"), Any.String().EndingWith("-42").Alpha() }) {
            foreach (string value in Samples(generator.WithLengthBetween(6, 12))) {
                Check.That(value).EndsWith("-42");
                Check.That(value.Substring(0, value.Length - 3).All(char.IsLetter)).IsTrue();
            }
        }
    }

    [Fact(DisplayName = "WithChars keeps a contained value its pool cannot draw, and still draws only from the pool.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void WithCharsKeepsAContainedValueOutsideItsPool() {
        foreach (string value in Samples(Any.String().WithChars("0123456789").Containing("-OK-").WithLengthBetween(8, 16))) {
            Check.That(value).Contains("-OK-");
            // The pool is digits only, so no filler can spell the literal: the first occurrence is the literal.
            Check.That(value.Remove(value.IndexOf("-OK-", StringComparison.Ordinal), 4).All(char.IsDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "A length the prefix fills exactly draws nothing, so the family governs nothing.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void APrefixFillingTheWholeLengthDrawsNothing() {
        Check.That(Any.String().Numeric().WithLength(4).StartingWith("ORD-").Generate()).IsEqualTo("ORD-");
    }

    [Fact(DisplayName = "A second WithLength conflicts: the exact length is declared once.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void SecondWithLengthConflicts() {
        Check.ThatCode(() => Any.String().WithLength(3).WithLength(5))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithLength(5)", "WithLength(3)");
    }

    [Fact(DisplayName = "A prefix longer than the exact length conflicts, naming both sides.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void PrefixLongerThanExactLengthConflicts() {
        Check.ThatCode(() => Any.String().WithLength(3).StartingWith("ORD-"))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("StartingWith(\"ORD-\")", "WithLength(3)", "4");
    }

    [Fact(DisplayName = "An exact length shorter than an already declared prefix conflicts, naming both sides.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void ExactLengthShorterThanPrefixConflicts() {
        Check.ThatCode(() => Any.String().StartingWith("ORD-").WithLength(3))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithLength(3)", "ORD-", "4");
    }

    [Fact(DisplayName = "A numeric string anchors a non-numeric prefix: the family governs the draw, not the literal.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void NumericAnchorsANonNumericPrefix() {
        foreach (string value in Samples(Any.String().Numeric().StartingWith("ORD-").WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Substring(4).All(char.IsDigit)).IsTrue();
        }
    }

    // A contained value is the one constraint the specification records per occurrence rather than in a named slot,
    // so it is also the one whose name a message can lose without any other assertion noticing.
    [Fact(DisplayName = "An allow-list that offers no value carrying the fragment names Containing as the culprit.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void AllowListRejectedByAFragmentNamesContaining() {
        Check.ThatCode(() => Any.String().Containing("ABC").OneOf("x", "y"))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Containing(\"ABC\")");
    }

    [Fact(DisplayName = "Declaring the charset after the prefix exempts it too: order does not matter.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void CharsetAfterAPrefixExemptsItToo() {
        foreach (string value in Samples(Any.String().StartingWith("ORD-").Numeric().WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Substring(4).All(char.IsDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "A minimum length above the maximum conflicts.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void MinAboveMaxConflicts() {
        Check.ThatCode(() => Any.String().WithMinLength(10).WithMaxLength(3))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithMaxLength(3)", "WithMinLength(10)");
    }

    [Fact(DisplayName = "An exact length above an already declared maximum conflicts.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void ExactAboveMaxConflicts() {
        Check.ThatCode(() => Any.String().WithMaxLength(3).WithLength(5)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "InLowerCase then InUpperCase conflicts: one casing per generator.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void LowerThenUpperCaseConflicts() {
        Check.ThatCode(() => Any.String().InLowerCase().InUpperCase())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("InUpperCase()", "InLowerCase()");
    }

    [Fact(DisplayName = "Alpha then Numeric conflicts: one character family per generator.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void AlphaThenNumericConflicts() {
        Check.ThatCode(() => Any.String().Alpha().Numeric()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "A lowercase string anchors an uppercase prefix, kept verbatim.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void LowerCaseAnchorsAnUppercasePrefix() {
        foreach (string value in Samples(Any.String().InLowerCase().StartingWith("ORD-").WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Substring(4).Any(character => character is >= 'A' and <= 'Z')).IsFalse();
        }
    }

    [Fact(DisplayName = "A second StartingWith conflicts: the prefix is declared once.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void SecondStartingWithConflicts() {
        Check.ThatCode(() => Any.String().StartingWith("A").StartingWith("B")).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Fragments exceeding the maximum length conflict.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void FragmentsExceedingMaxLengthConflict() {
        Check.ThatCode(() => Any.String().WithMaxLength(5).StartingWith("ORD-").EndingWith("-FR"))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("EndingWith(\"-FR\")", "7");
    }

    [Fact(DisplayName = "Length arguments are validated as arguments, not as conflicts.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void LengthArgumentsAreValidated() {
        Check.ThatCode(() => Any.String().WithLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithMinLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithMaxLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithLengthBetween(5, 3)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A produced length is refused above the ceiling; the bound just below it is accepted.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void ProducedLengthsAreCeilinged() {
        // ADR-0029. The two coordinates that matter are the ceiling itself and the first value past it: a guard
        // written with the wrong comparison passes every other length and fails exactly here.
        Check.ThatCode(() => Any.String().WithLength(1_000_001)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithMinLength(1_000_001)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithLengthBetween(1_000_001, 2_000_000)).Throws<ArgumentOutOfRangeException>();

        Check.ThatCode(() => Any.String().WithLength(1_000_000)).DoesNotThrow();
        Check.ThatCode(() => Any.String().WithMinLength(1_000_000)).DoesNotThrow();
    }

    [Fact(DisplayName = "An enormous length names the caller's parameter instead of leaking an internal one.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void AnEnormousLengthNamesTheCallersParameter() {
        // Regression: WithLength(int.MaxValue) used to surface an ArgumentOutOfRangeException from inside the draw,
        // naming System.Random's own 'maxValue' parameter after an arithmetic overflow — a message about internals
        // for a mistake the caller made in the Arrange.
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.String().WithLength(int.MaxValue));

        Check.That(error.ParamName).IsEqualTo("length");
    }

    [Fact(DisplayName = "A maximum steers the draw and is ceilinged like every other size.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void AMaximumSteersTheDraw() {
        // The bound the caller writes is the bound they get (ADR-0076) — and because it now steers, it is a size
        // the generator may have to produce, so the ceiling that used to exempt it applies.
        foreach (string value in Samples(Any.String().WithMaxLength(50))) {
            Check.That(value.Length).IsLessOrEqualThan(50);
        }

        Check.ThatCode(() => Any.String().WithMaxLength(int.MaxValue)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithMaxLength(4_000_000)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.String().WithMaxLength(1_000_000)).DoesNotThrow();
    }

    [Fact(DisplayName = "Fragment arguments are validated as arguments, not as conflicts.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void FragmentArgumentsAreValidated() {
        Check.ThatCode(() => Any.String().StartingWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().StartingWith("")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.String().EndingWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().Containing("")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "DifferentFrom never returns the excluded value.")]
    public void DifferentFromNeverReturnsTheExcludedValue() {
        foreach (string value in Samples(Any.String().WithLength(1).Alpha().DifferentFrom("A"))) {
            Check.That(value).IsNotEqualTo("A");
        }
    }

    [Fact(DisplayName = "Except excludes each listed value.")]
    public void ExceptExcludesEachListedValue() {
        string[] forbidden = { "A", "B", "C" };
        foreach (string value in Samples(Any.String().WithLength(1).Alpha().Except("A", "B", "C"))) {
            Check.That(forbidden.Contains(value)).IsFalse();
        }
    }

    [Fact(DisplayName = "An exclusion preserves the declared shape: only shape-matching survivors are drawn.")]
    public void ExclusionPreservesTheDeclaredShape() {
        foreach (string value in Samples(Any.String().StartingWith("ORD-").WithLength(5).DifferentFrom("ORD-A"))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Length).IsEqualTo(5);
            Check.That(value).IsNotEqualTo("ORD-A");
        }
    }

    [Fact(DisplayName = "Exclusions accumulate across several declarations.")]
    public void ExclusionsAccumulateAcrossDeclarations() {
        foreach (string value in Samples(Any.String().WithLength(1).Alpha().Except("A", "B").DifferentFrom("C"))) {
            Check.That(value is "A" or "B" or "C").IsFalse();
        }
    }

    [Fact(DisplayName = "An over-tight exclusion fails at generation with a bounded, seed-bearing AnyGenerationException.")]
    public void OverTightExclusionThrowsSeedBearingGenerationException() {
        string[] everyLetter = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Select(letter => letter.ToString()).ToArray();

        Check.ThatCode(() => Any.WithSeed(20260721).String().WithLength(1).Alpha().Except(everyLetter).Generate())
             .Throws<AnyGenerationException>()
             .WithProperty(error => error.Seed, 20260721)
             .And.WhichMember(error => error.Message).Contains("Any.WithSeed(20260721)");
    }

    [Fact(DisplayName = "An exhausted exclusion budget reports the budget, never a claim that the shape is unsatisfiable.")]
    public void ExhaustedExclusionBudgetDoesNotClaimUnsatisfiability() {
        // The redraw is bounded at 10,000 draws, and the message concluded from an exhausted budget that "the
        // exclusions leave the shape unsatisfiable". That does not follow: the failure probability is
        // (excluded / domain) ^ 10000, so a shape keeping one value free in a few hundred thousand exhausts the
        // budget most of the time and is still satisfiable. Here the domain really is empty — that is what makes the
        // case deterministic — but the message must state what was established, the budget, not a proof it never ran.
        string[] everyLetter = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Select(letter => letter.ToString()).ToArray();

        Check.ThatCode(() => Any.WithSeed(20260721).String().WithLength(1).Alpha().Except(everyLetter).Generate())
             .Throws<AnyGenerationException>()
             .WhichMember(error => error.Message)
             .Contains("10000 draws")
             .And.Contains("exhausted budget rather than a proof")
             .And.Not.Contains("so the exclusions leave the shape unsatisfiable")
             // The actionable half stays: the caller still learns what to change.
             .And.Contains("Loosen the exclusions or widen the shape");
    }

    [Fact(DisplayName = "A seeded exclusion is reproducible: the same seed yields the same value.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void SeededExclusionIsReproducible() {
        string first  = Any.WithSeed(4242).String().NonEmpty().Alpha().DifferentFrom("Q").Generate();
        string second = Any.WithSeed(4242).String().NonEmpty().Alpha().DifferentFrom("Q").Generate();

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "An exclusion composes with OneOf, removing the excluded value from the set.")]
    [SuppressMessage(JustDummiesRule.JD029.Category, JustDummiesRule.JD029.Id, Justification = SuppressionJustification.JD029.RefusedValueIsTheSubject)]
    public void AnExclusionComposesWithOneOf() {
        Check.That(Any.String().DifferentFrom("x").OneOf("a", "x").Generate()).IsEqualTo("a");
    }

    [Fact(DisplayName = "Exclusion arguments are validated as arguments, not as conflicts.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void ExclusionArgumentsAreValidated() {
        Check.ThatCode(() => Any.String().DifferentFrom(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().Except(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().Except("a", null!)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "WithChars draws every character from the supplied pool.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void WithCharsDrawsFromThePool() {
        const string pool = "0123456789ABCDEF";
        foreach (string value in Samples(Any.String().WithChars(pool).NonEmpty())) {
            Check.That(value.All(character => pool.Contains(character))).IsTrue();
        }
    }

    [Fact(DisplayName = "WithChars reaches every character in the pool.")]
    public void WithCharsReachesEveryCharacter() {
        const string  pool = "ACGT";
        HashSet<char> seen = [];
        foreach (string value in Samples(Any.String().WithChars(pool).WithLength(8))) {
            foreach (char character in value) { seen.Add(character); }
        }

        Check.That(pool.All(character => seen.Contains(character))).IsTrue();
    }

    [Fact(DisplayName = "WithChars reaches non-ASCII characters a named charset cannot.")]
    [SuppressMessage(JustDummiesRule.JD030.Category, JustDummiesRule.JD030.Id, Justification = SuppressionJustification.JD030.LengthIsNotWhatThisPins)]
    public void WithCharsReachesNonAscii() {
        const string pool = "àâäéèêëîïôùûüç";
        foreach (string value in Samples(Any.String().WithChars(pool).NonEmpty())) {
            Check.That(value.All(character => pool.Contains(character))).IsTrue();
        }
    }

    [Fact(DisplayName = "WithChars honours an exact length.")]
    public void WithCharsHonoursExactLength() {
        foreach (string value in Samples(Any.String().WithChars("xyz").WithLength(7))) {
            Check.That(value.Length).IsEqualTo(7);
        }
    }

    [Fact(DisplayName = "WithChars collapses duplicate characters in the pool.")]
    [SuppressMessage(JustDummiesRule.JD025.Category, JustDummiesRule.JD025.Id, Justification = SuppressionJustification.JD025.DuplicateIsTheSubject)]
    public void WithCharsCollapsesDuplicates() {
        foreach (string value in Samples(Any.String().WithChars("aaabbb").WithLength(4))) {
            Check.That(value.All(character => character is 'a' or 'b')).IsTrue();
        }
    }

    [Fact(DisplayName = "WithChars combines with an exclusion over its own pool.")]
    public void WithCharsCombinesWithExclusion() {
        foreach (string value in Samples(Any.String().WithChars("ab").WithLength(1).DifferentFrom("a"))) {
            Check.That(value).IsEqualTo("b");
        }
    }

    [Fact(DisplayName = "A subtraction removes its family from a WithChars pool.")]
    public void ASubtractionRemovesItsFamilyFromAWithCharsPool() {
        // Issue #210: the pool was drawn verbatim, so the digits WithoutNumeric() removes came back on every seed.
        using (Any.UseSeed(0)) {
            Check.That(Any.String().WithChars("abc123").WithoutNumeric().WithLength(20).Generate().All(character => character is 'a' or 'b' or 'c')).IsTrue();
        }
    }

    [Fact(DisplayName = "A subtraction that empties a WithChars pool is refused at declaration, naming both sides.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void ASubtractionThatEmptiesAWithCharsPoolIsRefused() {
        // Issue #210: accepted, then drawn from the untouched pool forever.
        Check.ThatCode(() => Any.String().WithChars("123").WithoutNumeric())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithoutNumeric() because WithChars(\"123\") admits nothing once it is applied.");
    }

    [Fact(DisplayName = "A family declared after the subtraction that empties it is refused, naming the subtraction.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void AFamilyDeclaredAfterTheSubtractionThatEmptiesItNamesTheSubtraction() {
        // Issue #211: the family was claimed as a phrase, which is never recognised as the constraint being applied,
        // so the message blamed the family on itself and never named the subtraction.
        Check.ThatCode(() => Any.String().WithoutAlpha().Alpha())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Alpha() because WithoutAlpha() removes every character Alpha() offers.");
        Check.ThatCode(() => Any.String().WithoutNumeric().Numeric())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Numeric() because WithoutNumeric() removes every character Numeric() offers.");
    }

    [Fact(DisplayName = "A family emptied by several subtractions names every one that removes a character it offers.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void AFamilyEmptiedBySeveralSubtractionsNamesEveryOneThatRemovesACharacterItOffers() {
        // Neither subtraction empties WithChars("a1") on its own, so naming only the last would blame one whose
        // removal leaves the chain just as refused. One that removes nothing the family offers takes no part.
        Check.ThatCode(() => Any.String().WithoutAlpha().WithoutNumeric().WithChars("a1"))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithChars(\"a1\") because WithoutAlpha() and WithoutNumeric() remove every character WithChars(\"a1\") offers.");
        Check.ThatCode(() => Any.String().WithoutAlpha().WithoutNumeric().WithChars("a"))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithChars(\"a\") because WithoutAlpha() removes every character WithChars(\"a\") offers.");
    }

    [Fact(DisplayName = "NotBlank blames the subtraction that left a WithChars pool with only whitespace.")]
    [SuppressMessage(JustDummiesRule.JD015.Category, JustDummiesRule.JD015.Id, Justification = SuppressionJustification.JD015.ThrowIsTheSubject)]
    public void NotBlankBlamesTheSubtractionThatLeftOnlyWhitespace() {
        // Issue #210: the guaranteed non-blank position was drawn from the unsubtracted pool. WithChars(" 1") alone
        // would satisfy NotBlank(), so the message names what removed the "1".
        Check.ThatCode(() => Any.String().WithChars(" 1").WithoutNumeric().NotBlank().WithLength(3).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because WithoutNumeric() removes every character WithChars(\" 1\") offers that is not whitespace.");
        Check.ThatCode(() => Any.String().WithChars(" a1").WithoutAlpha().WithoutNumeric().NotBlank().Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because WithoutAlpha() and WithoutNumeric() remove every character WithChars(\" a1\") offers that is not whitespace.");
    }

    [Fact(DisplayName = "A seeded WithChars draw is reproducible: the same seed yields the same value.")]
    public void SeededWithCharsIsReproducible() {
        string first  = Any.WithSeed(4242).String().WithChars("αβγδεζ").WithLength(5).Generate();
        string second = Any.WithSeed(4242).String().WithChars("αβγδεζ").WithLength(5).Generate();

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "WithChars then a named charset conflicts: one character family per generator.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void WithCharsThenNamedCharsetConflicts() {
        Check.ThatCode(() => Any.String().WithChars("abc").Numeric())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Numeric()", "WithChars(\"abc\")");
    }

    [Fact(DisplayName = "A named charset then WithChars conflicts: order does not matter.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void NamedCharsetThenWithCharsConflicts() {
        Check.ThatCode(() => Any.String().Alpha().WithChars("абвгд"))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithChars(\"абвгд\")", "Alpha()");
    }

    [Fact(DisplayName = "WithChars then a casing conflicts: the pool is the whole character definition.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void WithCharsThenCasingConflicts() {
        Check.ThatCode(() => Any.String().WithChars("abc").InLowerCase())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("InLowerCase()", "WithChars(\"abc\")");
    }

    [Fact(DisplayName = "A casing then WithChars conflicts: order does not matter.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void CasingThenWithCharsConflicts() {
        Check.ThatCode(() => Any.String().InUpperCase().WithChars("abc"))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithChars(\"abc\")", "InUpperCase()");
    }

    [Fact(DisplayName = "A WithChars pool anchors a prefix its pool cannot draw, and keeps drawing from the pool alone.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void WithCharsAnchorsAPrefixOutsideItsPool() {
        foreach (string value in Samples(Any.String().WithChars("0123456789").StartingWith("ID-").WithLengthBetween(6, 14))) {
            Check.That(value).StartsWith("ID-");
            Check.That(value.Substring(3).All(char.IsDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "Declaring WithChars after the fragment exempts it too: order does not matter.")]
    [SuppressMessage(JustDummiesRule.JD033.Category, JustDummiesRule.JD033.Id, Justification = SuppressionJustification.JD033.UnreachableLiteralIsTheSubject)]
    public void WithCharsAfterAFragmentExemptsItToo() {
        foreach (string value in Samples(Any.String().StartingWith("ID-").WithChars("0123456789").WithLengthBetween(6, 14))) {
            Check.That(value).StartsWith("ID-");
            Check.That(value.Substring(3).All(char.IsDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "WithChars arguments are validated as arguments, not as conflicts.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void WithCharsArgumentsAreValidated() {
        Check.ThatCode(() => Any.String().WithChars(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.String().WithChars("")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "WithChars rejects a pool with an astral code point and points to OneOf.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void WithCharsRejectsAstralPool() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.String().WithChars("😀🎉"));

        Check.That(error.Message).Contains("OneOf");
    }

}

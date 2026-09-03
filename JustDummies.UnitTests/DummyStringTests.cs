#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

[TestSubject(typeof(DummyString))]
public sealed class DummyStringTests {

    private const int SampleCount = 200;

    #region Statics members declarations

    private static IEnumerable<string> Samples(IDummy<string> generator) {
        for (int i = 0; i < SampleCount; i++) {
            yield return generator.Generate();
        }
    }

    #endregion

    [Fact(DisplayName = "An unconstrained String yields 0 to 1024 characters drawn from the whole of ASCII.")]
    public void UnconstrainedYieldsAsciiUpToTheSpread() {
        HashSet<char> seen = [];
        foreach (string value in Samples(Dummy.String())) {
            Check.That(value.Length).IsLessOrEqualThan(1024);
            Check.That(value.All(CharacterPools.IsAscii)).IsTrue();
            foreach (char character in value) { seen.Add(character); }
        }

        // The point of the default (ADR-0075): a control character is exactly what an unconstrained draw may hand
        // the code under test, so what the test survives, it has been shown to tolerate.
        Check.That(seen.Any(CharacterPools.IsAsciiNonPrintable)).IsTrue();
    }

    [Fact(DisplayName = "NonEmpty yields at least one character.")]
    public void NonEmptyHasAtLeastOneCharacter() {
        foreach (string value in Samples(Dummy.String().NonEmpty())) {
            Check.That(value.Length).IsStrictlyGreaterThan(0);
        }
    }

    [Fact(DisplayName = "NotBlank yields a value the guard behind IsNullOrWhiteSpace accepts.")]
    public void NotBlankIsNeverAllWhitespace() {
        foreach (string value in Samples(Dummy.String().NotBlank().WithMaxLength(4))) {
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
        foreach (string value in Samples(Dummy.String().NonEmpty().WithMaxLength(4))) {
            foreach (char character in value) { seen.Add(character); }
        }

        Check.That(seen.Any(character => CharacterPools.IsBlank(character) && !CharacterPools.IsAsciiWhitespace(character))).IsTrue();
    }

    [Fact(DisplayName = "NotBlank leaves interior whitespace legal — only an entirely blank value is refused.")]
    public void NotBlankAdmitsInteriorWhitespace() {
        Check.That(Samples(Dummy.String().NotBlank().WithLengthBetween(3, 8))
                   .Any(value => value.Skip(1).Take(value.Length - 2).Any(char.IsWhiteSpace)))
             .IsTrue();
    }

    [Fact(DisplayName = "NotBlank leans on an anchored literal that already carries a non-blank character.")]
    public void NotBlankAcceptsANonBlankAnchor() {
        foreach (string value in Samples(Dummy.String().StartingWith("A").NotBlank().WithLength(1))) {
            Check.That(value).IsEqualTo("A");
        }
    }

    [Fact(DisplayName = "NotBlank rescues a value whose every anchor is blank.")]
    public void NotBlankRescuesABlankAnchor() {
        foreach (string value in Samples(Dummy.String().StartingWith(" ").NotBlank().WithMaxLength(3))) {
            Check.That(string.IsNullOrWhiteSpace(value)).IsFalse();
        }
    }

    [Fact(DisplayName = "NotBlank names both sides when the declared family leaves only whitespace to draw.")]
    public void NotBlankConflictsWithTheWhitespacesFamily() {
        Check.ThatCode(() => Dummy.String().Whitespaces().NotBlank().Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because Whitespaces() leaves only whitespace to draw.");
    }

    [Fact(DisplayName = "NotBlank names both sides when the anchors fill the declared length.")]
    public void NotBlankConflictsWithAFullyAnchoredLength() {
        Check.ThatCode(() => Dummy.String().StartingWith(" ").WithLength(1).NotBlank().Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because the declared shape leaves no room for one.");
    }

    [Fact(DisplayName = "NotBlank blames the exhausted length rather than a family it never draws from.")]
    public void NotBlankBlamesTheShapeBeforeTheFamily() {
        // Both sides are unsatisfiable at once here. Dropping Whitespaces() would leave the chain just as refused,
        // so naming it would send the caller to a constraint whose departure changes nothing.
        Check.ThatCode(() => Dummy.String().StartingWith(" ").WithLength(1).Whitespaces().NotBlank().Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because the declared shape leaves no room for one.");
    }

    [Fact(DisplayName = "A contradiction is answered for once, whichever order the same constraints were written in.")]
    public void NotBlankRefusesAnEmptyAlphabetInEveryOrder() {
        // The diagnosis belongs to the constraint set, so the sentence has to be the same sentence every time --
        // not one message when the family came first and another when it came second.
        Check.ThatCode(() => Dummy.String().Whitespaces().NotBlank().Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because Whitespaces() leaves only whitespace to draw.");
        Check.ThatCode(() => Dummy.String().NotBlank().Whitespaces().Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WithMessage("Cannot apply NotBlank() because Whitespaces() leaves only whitespace to draw.");
    }

    [Theory(DisplayName = "An anchor satisfies NotBlank wherever in the chain it was declared.")]
    [MemberData(nameof(AnchorRescuesNotBlankCases))]
    public void AnchorRescuesNotBlankWhateverTheOrder(string because, Func<DummyString> chain, char anchored) {
        // The six shapes an exhaustive permutation sweep of the string surface found order-sensitive: a filler
        // alphabet holding no non-blank character, NotBlank(), and an anchor that carries one. Declared before the
        // other two the anchor was honoured; declared after, the chain was refused -- the same constraint set,
        // answered two ways. A specification is answered for once it is whole, so all of these draw.
        foreach (string value in Samples(chain())) {
            Check.WithCustomMessage(because).That(value).Contains(anchored.ToString());
            Check.WithCustomMessage(because).That(value.Any(character => !char.IsWhiteSpace(character))).IsTrue();
        }
    }

    public static TheoryData<string, Func<DummyString>, char> AnchorRescuesNotBlankCases() {
        return new TheoryData<string, Func<DummyString>, char> {
            { "Whitespaces + prefix", () => Dummy.String().Whitespaces().NotBlank().StartingWith("A"), 'A' },
            { "Whitespaces + suffix", () => Dummy.String().Whitespaces().NotBlank().EndingWith("Z"), 'Z' },
            { "Whitespaces + fragment", () => Dummy.String().Whitespaces().NotBlank().Containing("x"), 'x' },
            { "WithChars + prefix", () => Dummy.String().WithChars(" \t").NotBlank().StartingWith("A"), 'A' },
            { "WithChars + suffix", () => Dummy.String().WithChars(" \t").NotBlank().EndingWith("Z"), 'Z' },
            { "WithChars + fragment", () => Dummy.String().WithChars(" \t").NotBlank().Containing("x"), 'x' },
        };
    }

    [Fact(DisplayName = "NotBlank draws the same values whichever order its chain was written in.")]
    public void NotBlankIsIndifferentToDeclarationOrder() {
        // The three orders that reach a value must reach the SAME values, not merely all succeed: an anchor that
        // arrives late has to constrain the draw exactly as one that arrived early.
        foreach (Func<DummyString> chain in new Func<DummyString>[] {
                     () => Dummy.String().StartingWith("A").Whitespaces().NotBlank(),
                     () => Dummy.String().Whitespaces().StartingWith("A").NotBlank(),
                     () => Dummy.String().Whitespaces().NotBlank().StartingWith("A"),
                 }) {
            foreach (string value in Samples(chain())) {
                Check.That(value[0]).IsEqualTo('A');
                Check.That(value.Skip(1).All(char.IsWhiteSpace)).IsTrue();
            }
        }
    }

    [Fact(DisplayName = "WithLength yields exactly that many characters.")]
    public void WithLengthIsExact() {
        foreach (string value in Samples(Dummy.String().WithLength(10))) {
            Check.That(value.Length).IsEqualTo(10);
        }
    }

    [Fact(DisplayName = "WithLength(0) yields the empty string.")]
    public void WithLengthZeroIsEmpty() {
        Check.That(Dummy.String().WithLength(0).Generate()).IsEqualTo(string.Empty);
    }

    [Fact(DisplayName = "WithMinLength and WithMaxLength bound the length inclusively.")]
    public void MinAndMaxLengthAreInclusiveBounds() {
        foreach (string value in Samples(Dummy.String().WithMinLength(3).WithMaxLength(5))) {
            Check.That(value.Length).IsGreaterOrEqualThan(3);
            Check.That(value.Length).IsLessOrEqualThan(5);
        }
    }

    [Fact(DisplayName = "WithLengthBetween bounds the length inclusively and reaches its bounds.")]
    public void WithLengthBetweenIsInclusive() {
        HashSet<int> lengths = [];
        foreach (string value in Samples(Dummy.String().WithLengthBetween(2, 4))) {
            lengths.Add(value.Length);
            Check.That(value.Length).IsGreaterOrEqualThan(2);
            Check.That(value.Length).IsLessOrEqualThan(4);
        }

        Check.That(lengths.Contains(2)).IsTrue();
        Check.That(lengths.Contains(4)).IsTrue();
    }

    [Fact(DisplayName = "StartingWith anchors the prefix.")]
    public void StartingWithAnchorsThePrefix() {
        foreach (string value in Samples(Dummy.String().StartingWith("ORD-"))) {
            Check.That(value).StartsWith("ORD-");
        }
    }

    [Fact(DisplayName = "EndingWith anchors the suffix.")]
    public void EndingWithAnchorsTheSuffix() {
        foreach (string value in Samples(Dummy.String().EndingWith("-FR"))) {
            Check.That(value).EndsWith("-FR");
        }
    }

    [Fact(DisplayName = "Containing embeds the value.")]
    public void ContainingEmbedsTheValue() {
        foreach (string value in Samples(Dummy.String().Containing("ABC"))) {
            Check.That(value).Contains("ABC");
        }
    }

    [Fact(DisplayName = "Prefix, contained value, suffix and exact length hold together.")]
    public void FragmentsAndExactLengthHoldTogether() {
        foreach (string value in Samples(Dummy.String().StartingWith("ORD-").Containing("X").EndingWith("-FR").WithLength(12))) {
            Check.That(value.Length).IsEqualTo(12);
            Check.That(value).StartsWith("ORD-");
            Check.That(value).Contains("X");
            Check.That(value).EndsWith("-FR");
        }
    }

    [Fact(DisplayName = "A fragment-only budget is generable: length equals the fragment sum.")]
    public void FragmentsExactlyFillingTheLengthAreGenerable() {
        Check.That(Dummy.String().StartingWith("AB").EndingWith("CD").WithLength(4).Generate()).IsEqualTo("ABCD");
    }

    [Fact(DisplayName = "Alpha yields ASCII letters only.")]
    public void AlphaYieldsLettersOnly() {
        foreach (string value in Samples(Dummy.String().Alpha().NonEmpty())) {
            Check.That(value.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z')).IsTrue();
        }
    }

    [Fact(DisplayName = "Numeric yields ASCII digits only.")]
    public void NumericYieldsDigitsOnly() {
        foreach (string value in Samples(Dummy.String().Numeric().NonEmpty())) {
            Check.That(value.All(character => character is >= '0' and <= '9')).IsTrue();
        }
    }

    [Fact(DisplayName = "AlphaNumeric yields ASCII letters and digits only.")]
    public void AlphaNumericYieldsLettersAndDigitsOnly() {
        foreach (string value in Samples(Dummy.String().AlphaNumeric().NonEmpty())) {
            Check.That(value.All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9')).IsTrue();
        }
    }

    [Fact(DisplayName = "Punctuation yields printable non-alphanumerics only.")]
    public void PunctuationYieldsPrintableNonAlphaNumericsOnly() {
        foreach (string value in Samples(Dummy.String().Punctuation().NonEmpty())) {
            Check.That(value.All(CharacterPools.IsAsciiPunctuation)).IsTrue();
        }
    }

    [Fact(DisplayName = "Printable yields printable ASCII only, and actually reaches punctuation.")]
    public void PrintableYieldsPrintableAsciiOnly() {
        HashSet<char> seen = [];
        foreach (string value in Samples(Dummy.String().Printable().WithLength(16))) {
            Check.That(value.All(CharacterPools.IsAsciiPrintable)).IsTrue();
            foreach (char character in value) { seen.Add(character); }
        }

        // The point of the family: a filler that stops at letters and digits is what sent a caller looking for a
        // pool of their own in the first place.
        Check.That(seen.Any(CharacterPools.IsAsciiPunctuation)).IsTrue();
    }

    [Fact(DisplayName = "Printable admits a punctuated fragment, which the narrower families refuse.")]
    public void PrintableAdmitsAPunctuatedFragment() {
        foreach (string value in Samples(Dummy.String().Printable().StartingWith("ORD-").NonEmpty())) {
            Check.That(value).StartsWith("ORD-");
        }
    }

    [Fact(DisplayName = "WithoutAlpha yields no letter; WithoutAlpha().WithoutNumeric() yields neither a letter nor a digit.")]
    public void WithoutAlphaAndWithoutNumericSubtractFromTheFiller() {
        foreach (string value in Samples(Dummy.String().WithoutAlpha().NonEmpty())) {
            Check.That(value.Any(char.IsLetter)).IsFalse();
        }

        foreach (string value in Samples(Dummy.String().WithoutAlpha().WithoutNumeric().NonEmpty())) {
            Check.That(value.Any(char.IsLetterOrDigit)).IsFalse();
        }
    }

    [Fact(DisplayName = "WithoutAlpha keeps a contained value holding a letter: a subtraction governs the filler alone.")]
    public void WithoutAlphaKeepsAContainedValueHoldingALetter() {
        foreach (string value in Samples(Dummy.String().WithoutAlpha().Containing("abc").WithLengthBetween(6, 12))) {
            Check.That(value).Contains("abc");
            // The filler carries no letter, so the first occurrence is the literal itself: removing it leaves
            // exactly what was drawn, and the subtraction answers for all of that.
            Check.That(value.Remove(value.IndexOf("abc", StringComparison.Ordinal), 3).Any(char.IsLetter)).IsFalse();
        }
    }

    [Fact(DisplayName = "InLowerCase yields no uppercase letter; digits stay allowed.")]
    public void LowerCaseForbidsUppercaseLetters() {
        foreach (string value in Samples(Dummy.String().InLowerCase().NonEmpty())) {
            Check.That(value.Any(character => character is >= 'A' and <= 'Z')).IsFalse();
        }
    }

    [Fact(DisplayName = "InUpperCase draws no lowercase letter, and keeps a lowercase literal as written.")]
    public void UpperCaseForbidsLowercaseLetters() {
        foreach (string value in Samples(Dummy.String().InUpperCase().StartingWith("ord-").WithLengthBetween(6, 12))) {
            Check.That(value).StartsWith("ord-");
            Check.That(value.Substring(4).Any(character => character is >= 'a' and <= 'z')).IsFalse();
        }
    }

    // A character constraint governs what the generator DRAWS; a literal fixed by StartingWith, EndingWith or
    // Containing is not drawn, and is therefore exempt (ADR-0079). The cases below are the formats that could not be
    // written before: a fixed separator in the prefix, and a body the family still governs on its own.

    [Fact(DisplayName = "AlphaNumeric with an anchored prefix draws the separator nowhere but the prefix.")]
    public void AlphaNumericWithAPrefixDrawsTheSeparatorNowhereElse() {
        foreach (string value in Samples(Dummy.String().AlphaNumeric().StartingWith("ORD-").WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.IndexOf('-', 4)).IsEqualTo(-1);
            Check.That(value.Substring(4).All(char.IsLetterOrDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "An order reference keeps its four rules as named calls: prefix, family, casing and length.")]
    public void AnOrderReferenceKeepsItsFourRulesNamed() {
        foreach (string value in Samples(Dummy.String().StartingWith("ORD-").AlphaNumeric().InUpperCase().WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Length).IsGreaterOrEqualThan(8);
            Check.That(value.Length).IsLessOrEqualThan(20);
            Check.That(value.Substring(4).All(character => char.IsUpper(character) || char.IsDigit(character))).IsTrue();
        }
    }

    [Fact(DisplayName = "A family and an anchored suffix compose the same way whichever is declared first.")]
    public void AFamilyAndASuffixAreOrderIndependent() {
        foreach (DummyString generator in new[] { Dummy.String().Alpha().EndingWith("-42"), Dummy.String().EndingWith("-42").Alpha() }) {
            foreach (string value in Samples(generator.WithLengthBetween(6, 12))) {
                Check.That(value).EndsWith("-42");
                Check.That(value.Substring(0, value.Length - 3).All(char.IsLetter)).IsTrue();
            }
        }
    }

    [Fact(DisplayName = "WithChars keeps a contained value its pool cannot draw, and still draws only from the pool.")]
    public void WithCharsKeepsAContainedValueOutsideItsPool() {
        foreach (string value in Samples(Dummy.String().WithChars("0123456789").Containing("-OK-").WithLengthBetween(8, 16))) {
            Check.That(value).Contains("-OK-");
            // The pool is digits only, so no filler can spell the literal: the first occurrence is the literal.
            Check.That(value.Remove(value.IndexOf("-OK-", StringComparison.Ordinal), 4).All(char.IsDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "A length the prefix fills exactly draws nothing, so the family governs nothing.")]
    public void APrefixFillingTheWholeLengthDrawsNothing() {
        Check.That(Dummy.String().Numeric().WithLength(4).StartingWith("ORD-").Generate()).IsEqualTo("ORD-");
    }

    [Fact(DisplayName = "A second WithLength conflicts: the exact length is declared once.")]
    public void SecondWithLengthConflicts() {
        Check.ThatCode(() => Dummy.String().WithLength(3).WithLength(5))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithLength(5)", "WithLength(3)");
    }

    [Fact(DisplayName = "A prefix longer than the exact length conflicts, naming both sides.")]
    public void PrefixLongerThanExactLengthConflicts() {
        Check.ThatCode(() => Dummy.String().WithLength(3).StartingWith("ORD-"))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("StartingWith(\"ORD-\")", "WithLength(3)", "4");
    }

    [Fact(DisplayName = "An exact length shorter than an already declared prefix conflicts, naming both sides.")]
    public void ExactLengthShorterThanPrefixConflicts() {
        Check.ThatCode(() => Dummy.String().StartingWith("ORD-").WithLength(3))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithLength(3)", "ORD-", "4");
    }

    [Fact(DisplayName = "A numeric string anchors a non-numeric prefix: the family governs the draw, not the literal.")]
    public void NumericAnchorsANonNumericPrefix() {
        foreach (string value in Samples(Dummy.String().Numeric().StartingWith("ORD-").WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Substring(4).All(char.IsDigit)).IsTrue();
        }
    }

    // A contained value is the one constraint the specification records per occurrence rather than in a named slot,
    // so it is also the one whose name a message can lose without any other assertion noticing.
    [Fact(DisplayName = "An allow-list that offers no value carrying the fragment names Containing as the culprit.")]
    public void AllowListRejectedByAFragmentNamesContaining() {
        Check.ThatCode(() => Dummy.String().Containing("ABC").OneOf("x", "y"))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Containing(\"ABC\")");
    }

    [Fact(DisplayName = "Declaring the charset after the prefix exempts it too: order does not matter.")]
    public void CharsetAfterAPrefixExemptsItToo() {
        foreach (string value in Samples(Dummy.String().StartingWith("ORD-").Numeric().WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Substring(4).All(char.IsDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "A minimum length above the maximum conflicts.")]
    public void MinAboveMaxConflicts() {
        Check.ThatCode(() => Dummy.String().WithMinLength(10).WithMaxLength(3))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithMaxLength(3)", "WithMinLength(10)");
    }

    [Fact(DisplayName = "An exact length above an already declared maximum conflicts.")]
    public void ExactAboveMaxConflicts() {
        Check.ThatCode(() => Dummy.String().WithMaxLength(3).WithLength(5)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "InLowerCase then InUpperCase conflicts: one casing per generator.")]
    public void LowerThenUpperCaseConflicts() {
        Check.ThatCode(() => Dummy.String().InLowerCase().InUpperCase())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("InUpperCase()", "InLowerCase()");
    }

    [Fact(DisplayName = "Alpha then Numeric conflicts: one character family per generator.")]
    public void AlphaThenNumericConflicts() {
        Check.ThatCode(() => Dummy.String().Alpha().Numeric()).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "A lowercase string anchors an uppercase prefix, kept verbatim.")]
    public void LowerCaseAnchorsAnUppercasePrefix() {
        foreach (string value in Samples(Dummy.String().InLowerCase().StartingWith("ORD-").WithLengthBetween(8, 20))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Substring(4).Any(character => character is >= 'A' and <= 'Z')).IsFalse();
        }
    }

    [Fact(DisplayName = "A second StartingWith conflicts: the prefix is declared once.")]
    public void SecondStartingWithConflicts() {
        Check.ThatCode(() => Dummy.String().StartingWith("A").StartingWith("B")).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Fragments exceeding the maximum length conflict.")]
    public void FragmentsExceedingMaxLengthConflict() {
        Check.ThatCode(() => Dummy.String().WithMaxLength(5).StartingWith("ORD-").EndingWith("-FR"))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("EndingWith(\"-FR\")", "7");
    }

    [Fact(DisplayName = "Length arguments are validated as arguments, not as conflicts.")]
    public void LengthArgumentsAreValidated() {
        Check.ThatCode(() => Dummy.String().WithLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Dummy.String().WithMinLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Dummy.String().WithMaxLength(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Dummy.String().WithLengthBetween(5, 3)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A produced length is refused above the ceiling; the bound just below it is accepted.")]
    public void ProducedLengthsAreCeilinged() {
        // ADR-0029. The two coordinates that matter are the ceiling itself and the first value past it: a guard
        // written with the wrong comparison passes every other length and fails exactly here.
        Check.ThatCode(() => Dummy.String().WithLength(1_000_001)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Dummy.String().WithMinLength(1_000_001)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Dummy.String().WithLengthBetween(1_000_001, 2_000_000)).Throws<ArgumentOutOfRangeException>();

        Check.ThatCode(() => Dummy.String().WithLength(1_000_000)).DoesNotThrow();
        Check.ThatCode(() => Dummy.String().WithMinLength(1_000_000)).DoesNotThrow();
    }

    [Fact(DisplayName = "An enormous length names the caller's parameter instead of leaking an internal one.")]
    public void AnEnormousLengthNamesTheCallersParameter() {
        // Regression: WithLength(int.MaxValue) used to surface an ArgumentOutOfRangeException from inside the draw,
        // naming System.Random's own 'maxValue' parameter after an arithmetic overflow — a message about internals
        // for a mistake the caller made in the Arrange.
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Dummy.String().WithLength(int.MaxValue));

        Check.That(error.ParamName).IsEqualTo("length");
    }

    [Fact(DisplayName = "A maximum steers the draw and is ceilinged like every other size.")]
    public void AMaximumSteersTheDraw() {
        // The bound the caller writes is the bound they get (ADR-0076) — and because it now steers, it is a size
        // the generator may have to produce, so the ceiling that used to exempt it applies.
        foreach (string value in Samples(Dummy.String().WithMaxLength(50))) {
            Check.That(value.Length).IsLessOrEqualThan(50);
        }

        Check.ThatCode(() => Dummy.String().WithMaxLength(int.MaxValue)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Dummy.String().WithMaxLength(4_000_000)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Dummy.String().WithMaxLength(1_000_000)).DoesNotThrow();
    }

    [Fact(DisplayName = "Fragment arguments are validated as arguments, not as conflicts.")]
    public void FragmentArgumentsAreValidated() {
        Check.ThatCode(() => Dummy.String().StartingWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.String().StartingWith("")).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.String().EndingWith(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.String().Containing("")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "DifferentFrom never returns the excluded value.")]
    public void DifferentFromNeverReturnsTheExcludedValue() {
        foreach (string value in Samples(Dummy.String().WithLength(1).Alpha().DifferentFrom("A"))) {
            Check.That(value).IsNotEqualTo("A");
        }
    }

    [Fact(DisplayName = "Except excludes each listed value.")]
    public void ExceptExcludesEachListedValue() {
        string[] forbidden = { "A", "B", "C" };
        foreach (string value in Samples(Dummy.String().WithLength(1).Alpha().Except("A", "B", "C"))) {
            Check.That(forbidden.Contains(value)).IsFalse();
        }
    }

    [Fact(DisplayName = "An exclusion preserves the declared shape: only shape-matching survivors are drawn.")]
    public void ExclusionPreservesTheDeclaredShape() {
        foreach (string value in Samples(Dummy.String().StartingWith("ORD-").WithLength(5).DifferentFrom("ORD-A"))) {
            Check.That(value).StartsWith("ORD-");
            Check.That(value.Length).IsEqualTo(5);
            Check.That(value).IsNotEqualTo("ORD-A");
        }
    }

    [Fact(DisplayName = "Exclusions accumulate across several declarations.")]
    public void ExclusionsAccumulateAcrossDeclarations() {
        foreach (string value in Samples(Dummy.String().WithLength(1).Alpha().Except("A", "B").DifferentFrom("C"))) {
            Check.That(value is "A" or "B" or "C").IsFalse();
        }
    }

    [Fact(DisplayName = "An over-tight exclusion fails at generation with a bounded, seed-bearing DummyGenerationException.")]
    public void OverTightExclusionThrowsSeedBearingGenerationException() {
        string[] everyLetter = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Select(letter => letter.ToString()).ToArray();

        Check.ThatCode(() => Dummy.WithSeed(20260721).String().WithLength(1).Alpha().Except(everyLetter).Generate())
             .Throws<DummyGenerationException>()
             .WithProperty(error => error.Seed, 20260721)
             .And.WhichMember(error => error.Message).Contains("Dummy.WithSeed(20260721)");
    }

    [Fact(DisplayName = "An exhausted exclusion budget reports the budget, never a claim that the shape is unsatisfiable.")]
    public void ExhaustedExclusionBudgetDoesNotClaimUnsatisfiability() {
        // The redraw is bounded at 10,000 draws, and the message concluded from an exhausted budget that "the
        // exclusions leave the shape unsatisfiable". That does not follow: the failure probability is
        // (excluded / domain) ^ 10000, so a shape keeping one value free in a few hundred thousand exhausts the
        // budget most of the time and is still satisfiable. Here the domain really is empty — that is what makes the
        // case deterministic — but the message must state what was established, the budget, not a proof it never ran.
        string[] everyLetter = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".Select(letter => letter.ToString()).ToArray();

        Check.ThatCode(() => Dummy.WithSeed(20260721).String().WithLength(1).Alpha().Except(everyLetter).Generate())
             .Throws<DummyGenerationException>()
             .WhichMember(error => error.Message)
             .Contains("10000 draws")
             .And.Contains("exhausted budget rather than a proof")
             .And.Not.Contains("so the exclusions leave the shape unsatisfiable")
             // The actionable half stays: the caller still learns what to change.
             .And.Contains("Loosen the exclusions or widen the shape");
    }

    [Fact(DisplayName = "A seeded exclusion is reproducible: the same seed yields the same value.")]
    public void SeededExclusionIsReproducible() {
        string first  = Dummy.WithSeed(4242).String().NonEmpty().Alpha().DifferentFrom("Q").Generate();
        string second = Dummy.WithSeed(4242).String().NonEmpty().Alpha().DifferentFrom("Q").Generate();

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "An exclusion composes with OneOf, removing the excluded value from the set.")]
    public void AnExclusionComposesWithOneOf() {
        Check.That(Dummy.String().DifferentFrom("x").OneOf("a", "x").Generate()).IsEqualTo("a");
    }

    [Fact(DisplayName = "Exclusion arguments are validated as arguments, not as conflicts.")]
    public void ExclusionArgumentsAreValidated() {
        Check.ThatCode(() => Dummy.String().DifferentFrom(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.String().Except(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.String().Except()).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.String().Except("a", null!)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "WithChars draws every character from the supplied pool.")]
    public void WithCharsDrawsFromThePool() {
        const string pool = "0123456789ABCDEF";
        foreach (string value in Samples(Dummy.String().WithChars(pool).NonEmpty())) {
            Check.That(value.All(character => pool.Contains(character))).IsTrue();
        }
    }

    [Fact(DisplayName = "WithChars reaches every character in the pool.")]
    public void WithCharsReachesEveryCharacter() {
        const string  pool = "ACGT";
        HashSet<char> seen = [];
        foreach (string value in Samples(Dummy.String().WithChars(pool).WithLength(8))) {
            foreach (char character in value) { seen.Add(character); }
        }

        Check.That(pool.All(character => seen.Contains(character))).IsTrue();
    }

    [Fact(DisplayName = "WithChars reaches non-ASCII characters a named charset cannot.")]
    public void WithCharsReachesNonAscii() {
        const string pool = "àâäéèêëîïôùûüç";
        foreach (string value in Samples(Dummy.String().WithChars(pool).NonEmpty())) {
            Check.That(value.All(character => pool.Contains(character))).IsTrue();
        }
    }

    [Fact(DisplayName = "WithChars honours an exact length.")]
    public void WithCharsHonoursExactLength() {
        foreach (string value in Samples(Dummy.String().WithChars("xyz").WithLength(7))) {
            Check.That(value.Length).IsEqualTo(7);
        }
    }

    [Fact(DisplayName = "WithChars collapses duplicate characters in the pool.")]
    public void WithCharsCollapsesDuplicates() {
        foreach (string value in Samples(Dummy.String().WithChars("aaabbb").WithLength(4))) {
            Check.That(value.All(character => character is 'a' or 'b')).IsTrue();
        }
    }

    [Fact(DisplayName = "WithChars combines with an exclusion over its own pool.")]
    public void WithCharsCombinesWithExclusion() {
        foreach (string value in Samples(Dummy.String().WithChars("ab").WithLength(1).DifferentFrom("a"))) {
            Check.That(value).IsEqualTo("b");
        }
    }

    [Fact(DisplayName = "A seeded WithChars draw is reproducible: the same seed yields the same value.")]
    public void SeededWithCharsIsReproducible() {
        string first  = Dummy.WithSeed(4242).String().WithChars("αβγδεζ").WithLength(5).Generate();
        string second = Dummy.WithSeed(4242).String().WithChars("αβγδεζ").WithLength(5).Generate();

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "WithChars then a named charset conflicts: one character family per generator.")]
    public void WithCharsThenNamedCharsetConflicts() {
        Check.ThatCode(() => Dummy.String().WithChars("abc").Numeric())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Numeric()", "WithChars(\"abc\")");
    }

    [Fact(DisplayName = "A named charset then WithChars conflicts: order does not matter.")]
    public void NamedCharsetThenWithCharsConflicts() {
        Check.ThatCode(() => Dummy.String().Alpha().WithChars("абвгд"))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithChars(\"абвгд\")", "Alpha()");
    }

    [Fact(DisplayName = "WithChars then a casing conflicts: the pool is the whole character definition.")]
    public void WithCharsThenCasingConflicts() {
        Check.ThatCode(() => Dummy.String().WithChars("abc").InLowerCase())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("InLowerCase()", "WithChars(\"abc\")");
    }

    [Fact(DisplayName = "A casing then WithChars conflicts: order does not matter.")]
    public void CasingThenWithCharsConflicts() {
        Check.ThatCode(() => Dummy.String().InUpperCase().WithChars("abc"))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithChars(\"abc\")", "InUpperCase()");
    }

    [Fact(DisplayName = "A WithChars pool anchors a prefix its pool cannot draw, and keeps drawing from the pool alone.")]
    public void WithCharsAnchorsAPrefixOutsideItsPool() {
        foreach (string value in Samples(Dummy.String().WithChars("0123456789").StartingWith("ID-").WithLengthBetween(6, 14))) {
            Check.That(value).StartsWith("ID-");
            Check.That(value.Substring(3).All(char.IsDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "Declaring WithChars after the fragment exempts it too: order does not matter.")]
    public void WithCharsAfterAFragmentExemptsItToo() {
        foreach (string value in Samples(Dummy.String().StartingWith("ID-").WithChars("0123456789").WithLengthBetween(6, 14))) {
            Check.That(value).StartsWith("ID-");
            Check.That(value.Substring(3).All(char.IsDigit)).IsTrue();
        }
    }

    [Fact(DisplayName = "WithChars arguments are validated as arguments, not as conflicts.")]
    public void WithCharsArgumentsAreValidated() {
        Check.ThatCode(() => Dummy.String().WithChars(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.String().WithChars("")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "WithChars rejects a pool with an astral code point and points to OneOf.")]
    public void WithCharsRejectsAstralPool() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Dummy.String().WithChars("😀🎉"));

        Check.That(error.Message).Contains("OneOf");
    }

}

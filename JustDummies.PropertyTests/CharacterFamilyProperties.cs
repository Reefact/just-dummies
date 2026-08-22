#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="AnyChar" />'s alphabet — the character families, the casing, the
///     subtractions and the exclusions. <see cref="AnyChar" /> and <see cref="AnyString" /> share one
///     <c>Belongs()</c> definition for the pool filter (ADR-0075), and <see cref="StringShapeProperties" /> already
///     quantifies the same families over strings; these hold the singular type to the same account, over the
///     surface it does not share with strings — there is no length here, so no property below is about one.
/// </summary>
[TestSubject(typeof(AnyChar))]
public sealed class CharacterFamilyProperties {

    #region Statics members declarations

    /// <summary>
    ///     Applies one of the eight character families by index — 0 <c>Alpha</c>, 1 <c>Numeric</c>, 2
    ///     <c>AlphaNumeric</c>, 3 <c>Punctuation</c>, 4 <c>Printable</c>, 5 <c>NonPrintable</c>, 6
    ///     <c>Whitespaces</c>, 7 <c>Hexadecimal</c> — so a property can quantify over the family itself instead of
    ///     restating the same invariant eight times over.
    /// </summary>
    private static AnyChar ApplyCharacterFamily(AnyChar generator, int family) {
        return family switch {
            0 => generator.Alpha(),
            1 => generator.Numeric(),
            2 => generator.AlphaNumeric(),
            3 => generator.Punctuation(),
            4 => generator.Printable(),
            5 => generator.NonPrintable(),
            6 => generator.Whitespaces(),
            _ => generator.Hexadecimal()
        };
    }

    /// <summary>Whether <paramref name="character" /> belongs to the alphabet the family <paramref name="family" /> selects.</summary>
    private static bool AllowedByFamily(char character, int family) {
        return family switch {
            0 => IsAsciiLetter(character),
            1 => IsAsciiDigit(character),
            2 => IsAsciiLetter(character) || IsAsciiDigit(character),
            3 => IsAsciiPunctuation(character),
            4 => IsAsciiPrintable(character),
            5 => IsAsciiNonPrintable(character),
            6 => character is ' ' or '\t',
            _ => IsAsciiHexadecimal(character)
        };
    }

    /// <summary>Applies one of the two casings, so a property can quantify over the casing itself.</summary>
    private static AnyChar ApplyCasing(AnyChar generator, bool upper) {
        return upper ? generator.InUpperCase() : generator.InLowerCase();
    }

    // char.IsAsciiLetter/IsAsciiDigit are .NET 7+, and this suite also runs on the netstandard2.0 asset from the
    // net472 floor — so the two classifications the library itself uses are restated here, mirroring
    // StringShapeProperties.

    private static bool IsAsciiLetter(char character) {
        return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static bool IsAsciiDigit(char character) {
        return character is >= '0' and <= '9';
    }

    private static bool IsAsciiPrintable(char character) {
        return character is >= ' ' and <= '~';
    }

    private static bool IsAsciiPunctuation(char character) {
        return IsAsciiPrintable(character) && character != ' ' && !IsAsciiLetter(character) && !IsAsciiDigit(character);
    }

    private static bool IsAsciiNonPrintable(char character) {
        return character <= (char)0x7F && !IsAsciiPrintable(character);
    }

    private static bool IsAsciiHexadecimal(char character) {
        return IsAsciiDigit(character) || character is >= 'A' and <= 'F' or >= 'a' and <= 'f';
    }

    #endregion

    [Fact(DisplayName = "Every character family draws only from its own alphabet, whichever family is declared.")]
    public void CharacterFamiliesDrawOnlyFromTheirOwnAlphabet() {
        Prop.ForAll(Gen.Choose(0, 7).ToArbitrary(),
                    family => Expect.EveryDraw(ApplyCharacterFamily(Any.Char(), family),
                                               value => AllowedByFamily(value, family)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A casing constrains every cased character, whichever casing is declared.")]
    public void ACasingConstrainsEveryCasedCharacter() {
        Prop.ForAll(Gen.Elements(false, true).ToArbitrary(),
                    // A casing constrains the letters only: digits and punctuation stay drawable under either of them.
                    upper => Expect.EveryDraw(ApplyCasing(Any.Char(), upper),
                                              value => upper ? !(value is >= 'a' and <= 'z') : !(value is >= 'A' and <= 'Z')))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "WithoutAlpha and WithoutNumeric subtract and accumulate, whichever combination is applied.")]
    public void SubtractionsAccumulate() {
        Gen<(bool WithoutAlpha, bool WithoutNumeric)> cases =
            from withoutAlpha in Gen.Elements(false, true)
            from withoutNumeric in Gen.Elements(false, true)
            select (WithoutAlpha: withoutAlpha, WithoutNumeric: withoutNumeric);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => {
                        AnyChar generator = Any.Char();
                        if (testCase.WithoutAlpha) { generator = generator.WithoutAlpha(); }
                        if (testCase.WithoutNumeric) { generator = generator.WithoutNumeric(); }

                        return Expect.EveryDraw(generator,
                                                value => (!testCase.WithoutAlpha || !IsAsciiLetter(value))
                                                         && (!testCase.WithoutNumeric || !IsAsciiDigit(value)));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A second character family conflicts unless it repeats the first, whichever two are combined.")]
    public void ASecondCharacterFamilyConflictsUnlessItRepeatsTheFirst() {
        Gen<(int First, int Second)> cases =
            from first in Gen.Choose(0, 7)
            from second in Gen.Choose(0, 7)
            select (First: first, Second: second);

        Prop.ForAll(cases.ToArbitrary(),
                    // Repeating the same family is not a contradiction — the domain asked for is the one already in
                    // force — so it is a no-op; a different family contradicts it.
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(ApplyCharacterFamily(ApplyCharacterFamily(Any.Char(), testCase.First), testCase.Second),
                                                       value => AllowedByFamily(value, testCase.First))
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                        () => ApplyCharacterFamily(ApplyCharacterFamily(Any.Char(), testCase.First), testCase.Second)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A second casing conflicts unless it repeats the first, whichever two are combined.")]
    public void ASecondCasingConflictsUnlessItRepeatsTheFirst() {
        Gen<(bool First, bool Second)> cases =
            from first in Gen.Elements(false, true)
            from second in Gen.Elements(false, true)
            select (First: first, Second: second);

        Prop.ForAll(cases.ToArbitrary(),
                    testCase => testCase.First == testCase.Second
                                    ? Expect.EveryDraw(ApplyCasing(ApplyCasing(Any.Char(), testCase.First), testCase.Second),
                                                       value => testCase.First ? !(value is >= 'a' and <= 'z') : !(value is >= 'A' and <= 'Z'))
                                    : Expect.Throws<ConflictingAnyConstraintException>(
                                        () => ApplyCasing(ApplyCasing(Any.Char(), testCase.First), testCase.Second)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "OneOf draws only the supplied values, whichever pool is supplied.")]
    public void OneOfDrawsOnlyTheSuppliedValues() {
        Gen<char[]> pools = Gen.NonEmptyListOf(Gen.Elements("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray()))
                               .Select(values => values.Distinct().Take(10).ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Any.Char().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Exclusions accumulate and never yield an excluded value, while preserving the declared family.")]
    public void ExclusionsNeverYieldAnExcludedValue() {
        Prop.ForAll(Gen.Choose(1, 10).ToArbitrary(),
                    // The excluded values are drawn from the very generator they are then excluded from, so the
                    // exclusion is never vacuous. Alpha alone already allows 52 candidates, so removing up to ten of
                    // them plus one more banned value leaves the family amply satisfiable.
                    excludeCount => {
                        AnyChar shaped   = Any.Char().Alpha();
                        char[]  excluded = Expect.Draws(shaped, excludeCount).Distinct().ToArray();
                        char    banned   = shaped.Generate();
                        AnyChar narrowed = shaped.Except(excluded).DifferentFrom(banned);

                        return Expect.EveryDraw(narrowed,
                                                value => IsAsciiLetter(value) && !excluded.Contains(value) && value != banned);
                    })
            .QuickCheckThrowOnFailure();
    }

}

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     The library and <c>JD017</c> each decide, on their own, whether an enum type defines a given value, and
///     this is what stops the two answers drifting apart.
/// </summary>
/// <remarks>
///     <para>
///         The rule cannot ask the library: it reasons over symbols at build time, where no <c>AnyEnum</c> exists.
///         So the OR-reachability arithmetic is written twice — <c>AnyEnum.IsCombinationOfDeclaredMembers</c> and
///         <c>EnumUniverseViolationAnalyzer.IsCombinationOfDeclared</c> — and a copy nothing compares is a copy
///         that goes stale. Drift in one direction refuses at build time a chain the run time honours; in the
///         other it stays silent on one that throws.
///     </para>
///     <para>
///         Neither side names the answer here. The library's verdict is found by calling <c>OneOf</c>,
///         <c>Except</c> and <c>DifferentFrom</c> and seeing whether each refuses; the rule's by running it on the
///         same call and seeing whether it reports. The assertion is only that the two agree, over every value in a
///         range wide enough to reach past each shape's declared bits. Move either alone and this fails — the
///         exclusions included, which the library accepts as a no-op whatever the value (ADR-0105).
///     </para>
/// </remarks>
public sealed class EnumCombinationAgreementTests {

    /// <summary>
    ///     Wide enough to leave every shape below: past its declared bits, past the OR-closure of them, and onto
    ///     values no combination reaches. A range that stopped inside the closure would agree trivially.
    /// </summary>
    private const int PastEveryShapesBits = 15;

    /// <summary>The constraints that carry a value the two sides judge: the allow-list and both exclusions.</summary>
    private static readonly string[] Constraints = ["OneOf", "Except", "DifferentFrom"];

    [Fact(DisplayName = "A [Flags] enum with a zero member: both sides agree on every value.")]
    public async Task FlagsWithAZeroMember() {
        await BothSidesAgree<Permissions>("[Flags] public enum Permissions { None = 0, Read = 1, Write = 2 }");
    }

    [Fact(DisplayName = "A [Flags] enum with no zero member: both sides agree the empty combination is not a value.")]
    public async Task FlagsWithoutAZeroMember() {
        await BothSidesAgree<Sides>("[Flags] public enum Sides { Left = 1, Right = 2 }");
    }

    [Fact(DisplayName = "A [Flags] enum declaring a composite: both sides agree it adds no value of its own.")]
    public async Task FlagsWithADeclaredComposite() {
        await BothSidesAgree<Access>("[Flags] public enum Access { Read = 1, Write = 2, ReadWrite = 3 }");
    }

    [Fact(DisplayName = "A [Flags] enum with a gap in its bits: both sides agree on what the gap makes unreachable.")]
    public async Task FlagsWithAGapInItsBits() {
        await BothSidesAgree<Gaps>("[Flags] public enum Gaps { Low = 1, High = 4 }");
    }

    [Fact(DisplayName = "An enum that is not [Flags]: both sides agree only its declared members are values.")]
    public async Task NotAFlagsEnum() {
        await BothSidesAgree<Day>("public enum Day { Mon = 0, Tue = 1 }");
    }

    /// <summary>
    ///     Asks each side, over every value up to <see cref="PastEveryShapesBits" />, and asserts they answer the
    ///     same. The declaration is the type's source, so the rule reasons over the very shape the library is
    ///     called on — the two would otherwise agree about different enums.
    /// </summary>
    private static async Task BothSidesAgree<TEnum>(string declaration) where TEnum : struct, Enum {
        string name = typeof(TEnum).Name;

        foreach (string constraint in Constraints) {
            for (int bits = 0; bits <= PastEveryShapesBits; bits++) {
                bool libraryRefuses = LibraryRefuses<TEnum>(constraint, bits);
                bool ruleReports    = await RuleReports(declaration, name, constraint, bits);

                Check.WithCustomMessage($"{constraint}(({name}){bits}): the library {(libraryRefuses ? "refuses" : "accepts")} it, while JD017 {(ruleReports ? "reports" : "says nothing")}.")
                     .That(ruleReports).IsEqualTo(libraryRefuses);
            }
        }
    }

    /// <summary>Whether the library refuses the value, through <paramref name="constraint" />, as one its type does not define.</summary>
    [SuppressMessage(SonarRule.S3220.Category, SonarRule.S3220.Id, Justification = "OneOf carries a sequence overload alongside the params one, and TEnum is an unresolved type parameter the rule cannot clear — a concrete enum member does not trip it. Only the params form is applicable here, and wrapping the value in an array to disambiguate would immediately trip S3878, which asks for that array to be removed again.")]
    private static bool LibraryRefuses<TEnum>(string constraint, int bits) where TEnum : struct, Enum {
        TEnum value = (TEnum)Enum.ToObject(typeof(TEnum), bits);

        try {
            AnyEnum<TEnum> generator = constraint switch {
                "OneOf"  => Any.Enum<TEnum>().OneOf(value),
                "Except" => Any.Enum<TEnum>().Except(value),
                _        => Any.Enum<TEnum>().DifferentFrom(value),
            };
            _ = generator.Generate();

            return false;
        } catch (ArgumentException) {
            return true;
        }
    }

    /// <summary>Whether JD017 reports the same value, written at a call site through the same constraint.</summary>
    private static async Task<bool> RuleReports(string declaration, string name, string constraint, int bits) {
        string source = $$"""
            using System;
            using JustDummies;

            {{declaration}}

            public static class Sample {
                public static void M() {
                    _ = Any.Enum<{{name}}>().{{constraint}}(({{name}}){{bits}});
                }
            }
            """;

        ImmutableArray<Diagnostic> diagnostics = await AnalyzerTestHarness.GetDiagnosticsAsync(new EnumUniverseViolationAnalyzer(), source);

        return diagnostics.Length > 0;
    }

    #region Nested types declarations

    // Mirrors of the declarations above, so the library can be called on the very shapes the rule reads.

    [Flags]
    public enum Permissions { None = 0, Read = 1, Write = 2 }

    [Flags]
    public enum Sides { Left = 1, Right = 2 }

    [Flags]
    public enum Access { Read = 1, Write = 2, ReadWrite = 3 }

    [Flags]
    public enum Gaps { Low = 1, High = 4 }

    public enum Day { Mon = 0, Tue = 1 }

    #endregion

}

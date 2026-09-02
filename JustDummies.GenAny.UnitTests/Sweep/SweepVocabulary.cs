namespace JustDummies.GenAny.UnitTests.Sweep;

/// <summary>
///     The domain types every generated shape may lean on, declared once and prepended to each of them.
/// </summary>
/// <remarks>
///     Deliberately the corpus's own vocabulary rather than a parallel one: <c>Slot</c>, <c>Grade</c> and
///     <c>Permission</c> are spelled in <see cref="GuardCorpus" /> exactly as they are here, so a shape the
///     sweep flags can be lifted into the corpus as a named row without renaming anything. Two benches, one
///     set of nouns.
///     <para>
///         Every type here carries a declared cardinality in <see cref="SweepAxes" /> — how many distinct
///         values the library can draw from it — and that number is what makes the distinctness rule
///         checkable in both directions rather than merely observable.
///     </para>
/// </remarks>
internal static class SweepVocabulary {

    /// <summary>The usings and namespace every generated domain opens with.</summary>
    internal const string Preamble = """
                                     using System;
                                     using System.Collections.Generic;
                                     using System.Linq;

                                     namespace Shop.Domain;

                                     """;

    /// <summary>The shared declarations, between the preamble and the shape's own type.</summary>
    internal const string Declarations = """
                                         // Three members, and the corpus spells it the same way.
                                         public enum Slot { None, Morning, Evening }

                                         // Four members, the plain case.
                                         public enum Suit { Hearts, Diamonds, Clubs, Spades }

                                         // Five NAMES over three VALUES: an alias is not a distinct value, and a
                                         // generator that counted names would claim a capacity it does not have.
                                         public enum Grade { Low = 1, Medium = 2, High = 3, Min = 1, Max = 3 }

                                         // Wide enough that a count guard cannot exhaust it.
                                         public enum Wide {
                                             V00, V01, V02, V03, V04, V05, V06, V07, V08, V09, V10, V11, V12, V13, V14, V15,
                                             V16, V17, V18, V19, V20, V21, V22, V23, V24, V25, V26, V27, V28, V29, V30, V31
                                         }

                                         public enum Permission { Read, Write, Delete }

                                         // One member, and none: the two ends of the cardinality axis.
                                         public enum Lone { Only }

                                         public enum Nothing { }

                                         // A composed element with a public constructor: the engine scaffolds through it.
                                         public sealed class Code {
                                             public Code(string value) {
                                                 if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }
                                             }
                                         }

                                         public sealed class Tag {
                                             public Tag(string value) { }
                                         }

                                         // A composed element carrying a guard of its own, so composition and guard
                                         // reading meet rather than being exercised apart.
                                         public sealed class Delta {
                                             public Delta(int value) {
                                                 if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                             }
                                         }

                                         // No accessible constructor: the engine has to reach the static factory.
                                         public sealed class Badge {
                                             private Badge() { }
                                             public static Badge Create(string value) { return new Badge(); }
                                         }

                                         public sealed class Stamp {
                                             private Stamp() { }
                                             public static Stamp Create(int value) { return new Stamp(); }
                                         }

                                         // A guard no row of the closed table says: the engine must NOT claim to have
                                         // read it, and must say so rather than guess.
                                         public sealed class Doubtful {
                                             public Doubtful(int value) {
                                                 if (Improbable(value)) { throw new ArgumentOutOfRangeException(nameof(value)); }
                                             }

                                             private static bool Improbable(int value) { return value % 7 == 3; }
                                         }
                                         """;

}

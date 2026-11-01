namespace JustDummies.PropertyTests;

/// <summary>
///     The justifications carried by this suite's <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" />
///     declarations, one nested class per analyzer rule — the same convention as the library's
///     <c>SuppressionJustification</c>: a text lives here when it is duplicated, or when it is long enough that leaving
///     it inline would make the attribute unreadable, and the detailed reasoning sits in each constant's summary.
/// </summary>
internal static class SuppressionJustification {

    /// <summary>Justifications for CA1865 — "Use char overload of string.StartsWith/EndsWith".</summary>
    internal static class CA1865 {

        /// <summary>
        ///     <c>string.StartsWith(char)</c> is not on the .NET Framework 4.7.2 support floor this suite also runs on
        ///     (ADR-0007): measured, the net472 leg rejects it with CS1503, cannot convert from 'char' to 'string'. The
        ///     explicit <c>StringComparison.Ordinal</c> overload compiles on both legs and states the comparison it uses.
        /// </summary>
        internal const string NoStartsWithCharDownlevel = "string.StartsWith(char) does not compile on the net472 support floor this suite also runs on (ADR-0007). See the constant's summary.";

    }

    /// <summary>Justifications for CA1870 — "Use a cached 'SearchValues&lt;T&gt;' instance".</summary>
    internal static class CA1870 {

        /// <summary>
        ///     <c>SearchValues&lt;T&gt;</c> arrived in .NET 8 and this suite also runs on the .NET Framework 4.7.2 support
        ///     floor (ADR-0007, <c>build/Net472TestFloor.props</c>), where the type does not exist. The rule is right on
        ///     net10.0 only; <c>IndexOfAny</c> over a two-character array carries the same meaning on both legs. Same
        ///     downlevel wall as SYSLIB1045 and CA1510 (ADR-0037).
        /// </summary>
        internal const string NoSearchValuesDownlevel = "SearchValues<T> arrived in .NET 8 and this suite also runs on the net472 support floor (ADR-0007), where IndexOfAny is the spelling that compiles. See the constant's summary.";

    }

    /// <summary>Justifications for CA2249 — "Consider using String.Contains instead of String.IndexOf".</summary>
    internal static class CA2249 {

        /// <summary>
        ///     <c>string.Contains(string, StringComparison)</c> is not on the netstandard2.0 / net472 floor this suite runs
        ///     against (ADR-0007); <c>IndexOf</c> with the same <c>StringComparison.Ordinal</c> carries the identical
        ///     comparison and compiles on every leg. The rule is right on net10.0 only.
        /// </summary>
        internal const string NoContainsWithComparisonDownlevel = "string.Contains(string, StringComparison) is absent from the floor this suite runs against (ADR-0007); IndexOf with StringComparison.Ordinal is the identical comparison. See the constant's summary.";

        /// <summary>
        ///     <c>string.Contains(char)</c> is not on the .NET Framework 4.7.2 support floor this suite also runs on
        ///     (ADR-0007); <c>IndexOf(char)</c> carries the identical test and compiles on both legs. The rule is right
        ///     on net10.0 only.
        /// </summary>
        internal const string NoContainsCharDownlevel = "string.Contains(char) does not compile on the net472 support floor this suite also runs on (ADR-0007). See the constant's summary.";

    }

    /// <summary>Justifications for JD004 — the discarded-seeding-result diagnostic.</summary>
    internal static class JD004 {

        /// <inheritdoc cref="JD014.ThrowIsTheSubject" />
        internal const string ThrowIsTheSubject = "The throw IS the subject: the test asserts that the run time refuses this call, so it has to be written as it is. See the constant's summary.";

    }

    /// <summary>Justifications for JD006 — the discarded-generator-result diagnostic.</summary>
    internal static class JD006 {

        /// <inheritdoc cref="JD014.ThrowIsTheSubject" />
        internal const string ThrowIsTheSubject = "The throw IS the subject: the test asserts that the run time refuses this call, so it has to be written as it is. See the constant's summary.";

    }

    /// <summary>Justifications for JD014 — the rejected-constant-argument diagnostic.</summary>
    internal static class JD014 {

        /// <summary>
        ///     The throw IS the subject. The test asserts that the run time refuses this call, so the call has to be
        ///     written as it is, and the diagnostic predicting the refusal is the expected one. No rule guesses which
        ///     chains a test writes on purpose (ADR-0112): the test says so here.
        /// </summary>
        internal const string ThrowIsTheSubject = "The throw IS the subject: the test asserts that the run time refuses this call, so it has to be written as it is. See the constant's summary.";

    }

    /// <summary>Justifications for JD015 — the string-constraints-admit-no-value diagnostic.</summary>
    internal static class JD015 {

        /// <inheritdoc cref="JD014.ThrowIsTheSubject" />
        internal const string ThrowIsTheSubject = "The throw IS the subject: the test asserts that the run time refuses this call, so it has to be written as it is. See the constant's summary.";

    }

    /// <summary>Justifications for JD027 — the ignored-operand diagnostic.</summary>
    internal static class JD027 {

        /// <inheritdoc cref="JD014.ThrowIsTheSubject" />
        internal const string ThrowIsTheSubject = "The throw IS the subject: the test asserts that the run time refuses this call, so it has to be written as it is. See the constant's summary.";

    }

    /// <summary>Justifications for JD030 — the undeclared-string-length diagnostic.</summary>
    internal static class JD030 {

        /// <summary>
        ///     The length is not what these properties pin. Either the default spread is itself the draw under test, or the string
        ///     only has to exist and be well-formed while the test is about something else. Declaring a length would narrow
        ///     the sample the suite is meant to exercise, or add a number that looks as though it mattered.
        /// </summary>
        internal const string LengthIsNotWhatThisPins = "The length is not what this test pins: the default spread is the draw under test, or the string is incidental. See the constant's summary.";

    }

#if NET8_0_OR_GREATER
    /// <summary>Justifications for JD031 — two bounds that are the range form.</summary>
    internal static class JD031 {

        /// <summary>
        ///     The two bounds written separately ARE the subject. The test pins how each bound is honoured on its own, which
        ///     the range form the rule suggests would hide.
        /// </summary>
        internal const string SeparateBoundsAreTheSubject = "The two bounds written separately ARE the subject: the test pins how each is honoured on its own. See the constant's summary.";

    }
#endif

    /// <summary>Justifications for JD036 — the undeclared floating-point range diagnostic.</summary>
    internal static class JD036 {

        /// <summary>
        ///     The open side IS the subject. These properties pin what a floating-point draw does where the caller declared no
        ///     range on a side — the ordinary window, the edge draw ADR-0117 adds one draw in ten, the type's own edge, a
        ///     bound written against it. Declaring the range JD036 asks for would remove the very behaviour under test.
        /// </summary>
        internal const string TheOpenSideIsTheSubject = "The open side IS the subject: the test pins what a floating-point draw does where no range is declared (window, edge draw, domain edge). See the constant's summary.";

        /// <summary>
        ///     The magnitude is not what these properties pin. The chain exercises a lattice, a pool, a refusal or a message, and
        ///     the drawn value only has to exist; a range declared to quieten the rule would add numbers that look as though
        ///     they mattered, or change the declaration whose message the test reads.
        /// </summary>
        internal const string MagnitudeIsNotWhatThisPins = "The magnitude is not what this test pins: the chain exercises a lattice, a pool, a refusal or a message, and the value only has to exist. See the constant's summary.";

    }

    /// <summary>Justifications for S1854 — "Unused assignments should be removed".</summary>
    internal static class S1854 {

        /// <summary>
        ///     The assignment is dead and the CALL is not. These builders exist to provoke the declaration-time
        ///     conflict, so what matters is that <c>Except()</c> runs; nothing reads the spec afterwards because the
        ///     verdict is the exception or its absence. Dropping <c>spec =</c> from the last line alone would break
        ///     the uniform chain that makes the sequence of constraints readable.
        /// </summary>
        internal const string CallIsTheSubject = "The assignment is dead and the CALL is not: the verdict is the exception or its absence. See the constant's summary.";

    }

    /// <summary>Justifications for S2692 — "'IndexOf' checks should not be for positive numbers".</summary>
    internal static class S2692 {

        /// <summary>
        ///     0 is deliberately excluded. The check asserts that a user-info draw renders <c>user:password</c> with a
        ///     NON-EMPTY user, so a colon at index 0 — an empty local part — must fail the property, which is exactly what
        ///     <c>&gt; 0</c> says.
        /// </summary>
        internal const string EmptyLocalPartMustFail = "0 is excluded on purpose: a colon at index 0 is an empty user, which the property must reject. See the constant's summary.";

    }

    /// <summary>Justifications for S3376 — "Attribute, EventArgs, and Exception type names should end with the type being extended".</summary>
    internal static class S3376 {

        /// <summary>
        ///     Named for what it reads as at the throw site inside the property. The <c>Exception</c> suffix would say
        ///     nothing the base type does not, and this type is private to one test class.
        /// </summary>
        internal const string NamedForTheThrowSite = "Named for what it reads as at the throw site; the Exception suffix would say nothing the base type does not. See the constant's summary.";

    }

    /// <summary>Justifications for S3871 — "Exception types should be public".</summary>
    internal static class S3871 {

        /// <summary>
        ///     A fixture, not part of any contract. It exists so a test factory can raise a failure distinguishable from
        ///     anything the library itself throws; making it public would export a type from a test assembly for no reader.
        /// </summary>
        internal const string FixtureNotContract = "A fixture, not a contract: making it public would export a type from a test assembly for no reader. See the constant's summary.";

    }

}

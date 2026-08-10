// Compiler-required marker behind C# `init` accessors and records. The .NET Framework 4.7.2 BCL does not
// ship it, so this polyfill is compiled ONLY into net472 test builds (see build/Net472TestFloor.props). It is
// internal, so it never leaves the test assembly and each net472 test assembly gets its own. The shipped
// JustDummies libraries use neither `init` nor records, so nothing in the product relies on this.

using System.Diagnostics.CodeAnalysis;

namespace JustDummies.Net472Floor {

    /// <summary>
    ///     The justification for the one suppression this shared file carries. It sits here rather than in a project's
    ///     own <c>SuppressionJustification</c> because the file is compiled into every net472 test assembly separately,
    ///     and none of them shares the others' internals — there is no single home the attribute below could point at.
    ///     Same convention otherwise: one nested class per rule, the reasoning in the summary, one crisp sentence as the
    ///     value (ADR-0050).
    /// </summary>
    internal static class SuppressionJustification {

        /// <summary>Justifications for S2094 — "Classes should not be empty".</summary>
        internal static class S2094 {

            /// <summary>
            ///     A compiler-recognised marker type. Its emptiness is the specification: the compiler binds
            ///     <c>System.Runtime.CompilerServices.IsExternalInit</c> by name to enable <c>init</c> accessors and
            ///     records, and reads nothing from it. It cannot be an interface, and giving it a member would be noise.
            /// </summary>
            internal const string EmptinessIsTheSpecification = "The compiler binds this marker by name and reads nothing from it: its emptiness IS the specification. See the constant's summary.";

        }

    }

}

// ReSharper disable once CheckNamespace -- the compiler resolves this type by its exact fully-qualified name.
namespace System.Runtime.CompilerServices {

    // Empty BY CONTRACT. The compiler looks this type up by name and never reads a member, so there is
    // nothing to write and it cannot become an interface: it must be a class the compiler can bind to.
    // Sonar's S2094 only fires on the net472 inner build, which is the only one that compiles this file.
    [SuppressMessage(SonarRule.S2094.Category, SonarRule.S2094.Id, Justification = JustDummies.Net472Floor.SuppressionJustification.S2094.EmptinessIsTheSpecification)]
    internal static class IsExternalInit { }

}

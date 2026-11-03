// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate StowageAssignment --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace VesselStowage.Domain.Tests.Generators;

/// <summary>Hangs <c>Any.StowageAssignment()</c> off the library's own entry point.</summary>
public static class AnyStowageAssignmentEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>StowageAssignment</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyStowageAssignment StowageAssignment() {
            return new AnyStowageAssignment();
        }

    }

}

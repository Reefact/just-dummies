// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Enrolment --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Education.Domain;

/// <summary>Hangs <c>Any.Enrolment()</c> off the library's own entry point.</summary>
public static class AnyEnrolmentEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Enrolment</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyEnrolment Enrolment() {
            return new AnyEnrolment();
        }

    }

}

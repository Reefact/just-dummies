// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ClientApplication --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Identity.Domain.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.ClientApplication()</c> off the library's own entry point.</summary>
public static class AnyClientApplicationEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>ClientApplication</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyClientApplication ClientApplication() {
            return new AnyClientApplication();
        }

    }

}

// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Session --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

/// <summary>Hangs <c>Dummy.Session()</c> off the library's own entry point.</summary>
public static class DummySessionEntry {

    extension(Dummy) {

        /// <summary>Starts an arbitrary <c>Session</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static DummySession Session() {
            return new DummySession();
        }

    }

}

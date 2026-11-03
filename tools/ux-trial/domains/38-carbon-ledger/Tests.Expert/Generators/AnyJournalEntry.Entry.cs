// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate JournalEntry --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace CarbonLedger.Tests.Expert.Generators;

/// <summary>Hangs <c>Any.JournalEntry()</c> off the library's own entry point.</summary>
public static class AnyJournalEntryEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>JournalEntry</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyJournalEntry JournalEntry() {
            return new AnyJournalEntry();
        }

    }

}

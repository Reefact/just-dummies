// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Order --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Shop.Domain;

/// <summary>Hangs <c>Dummy.Order()</c> off the library's own entry point.</summary>
public static class DummyOrderEntry {

    extension(Dummy) {

        /// <summary>Starts an arbitrary <c>Order</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static DummyOrder Order() {
            return new DummyOrder();
        }

    }

}

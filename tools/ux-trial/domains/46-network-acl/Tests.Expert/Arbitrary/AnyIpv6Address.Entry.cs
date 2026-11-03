// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ipv6Address --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>Hangs <c>Any.Ipv6Address()</c> off the library's own entry point.</summary>
public static class AnyIpv6AddressEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Ipv6Address</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyIpv6Address Ipv6Address() {
            return new AnyIpv6Address();
        }

    }

}

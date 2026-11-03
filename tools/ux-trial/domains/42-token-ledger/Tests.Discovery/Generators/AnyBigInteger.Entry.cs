using JustDummies;

namespace TokenLedger.Domain;

/// <summary>Hangs <c>Any.BigInteger()</c> off the library's own entry point.</summary>
public static class AnyBigIntegerEntry {

    extension(Any) {

        public static AnyBigInteger BigInteger() {
            return new AnyBigInteger();
        }

    }

}

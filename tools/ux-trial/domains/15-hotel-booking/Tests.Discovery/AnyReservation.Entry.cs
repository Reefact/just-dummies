// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Reservation --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace Booking.Domain;

/// <summary>Hangs <c>Any.Reservation()</c> off the library's own entry point.</summary>
public static class AnyReservationEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Reservation</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyReservation Reservation() {
            return new AnyReservation();
        }

    }

}

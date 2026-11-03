using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>An instant a laboratory could plausibly stamp: far from the ends of the type, so date arithmetic in a test never overflows.</summary>
public static class AnyInstantEntry {

    extension(Any) {

        public static AnyDateTimeOffset Instant() {
            return Any.DateTimeOffset().Between(new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                                                new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        }

    }

}

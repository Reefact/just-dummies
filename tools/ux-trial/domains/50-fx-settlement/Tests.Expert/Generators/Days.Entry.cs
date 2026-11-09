using JustDummies;

namespace FxSettlement.Domain.Tests.Expert;

/// <summary>Hangs the calendar generators off the library's own entry point.</summary>
public static class DaysEntry {

    extension(Any) {

        public static IAny<DateOnly> BusinessDay() {
            return Days.Business();
        }

        public static IAny<DateOnly> WeekendDay() {
            return Days.Weekend();
        }

    }

}

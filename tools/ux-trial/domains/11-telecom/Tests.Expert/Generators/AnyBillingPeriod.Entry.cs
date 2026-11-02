using JustDummies;

namespace Telecom.Tests.Expert.Generators;

public static class AnyBillingPeriodEntry {

    extension(Any) {

        public static AnyBillingPeriod BillingPeriod() {
            return new AnyBillingPeriod();
        }

    }

}

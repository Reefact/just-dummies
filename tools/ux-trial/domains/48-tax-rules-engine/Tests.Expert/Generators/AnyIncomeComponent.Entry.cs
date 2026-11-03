using JustDummies;

namespace TaxRules.Domain;

public static class AnyIncomeComponentEntry {

    extension(Any) {

        public static AnyIncomeComponent IncomeComponent() {
            return new AnyIncomeComponent();
        }

    }

}

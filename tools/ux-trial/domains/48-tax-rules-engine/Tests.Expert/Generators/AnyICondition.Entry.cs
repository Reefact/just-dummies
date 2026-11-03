using JustDummies;

namespace TaxRules.Domain;

public static class AnyIConditionEntry {

    extension(Any) {

        public static AnyICondition Condition() {
            return new AnyICondition();
        }

    }

}

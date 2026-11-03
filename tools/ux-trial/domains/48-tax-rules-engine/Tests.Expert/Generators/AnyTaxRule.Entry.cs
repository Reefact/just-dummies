using JustDummies;

namespace TaxRules.Domain;

public static class AnyTaxRuleEntry {

    extension(Any) {

        public static AnyTaxRule TaxRule() {
            return new AnyTaxRule();
        }

    }

}

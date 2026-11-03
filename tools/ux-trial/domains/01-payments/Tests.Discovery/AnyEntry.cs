using JustDummies;

namespace Payments.Domain;

public static class AnyEntry {

    extension(Any) {

        public static AnyIban Iban() {
            return new AnyIban();
        }

        public static AnyBic Bic() {
            return new AnyBic();
        }

        public static AnyMoney Money() {
            return new AnyMoney();
        }

        public static AnyRemittanceInformation RemittanceInformation() {
            return new AnyRemittanceInformation();
        }

        public static AnyParty Party() {
            return new AnyParty();
        }

        public static AnyCreditTransfer CreditTransfer() {
            return new AnyCreditTransfer();
        }

        public static AnyPaymentBatch PaymentBatch() {
            return new AnyPaymentBatch();
        }

        public static AnyFeePolicy FeePolicy() {
            return new AnyFeePolicy();
        }

    }

}

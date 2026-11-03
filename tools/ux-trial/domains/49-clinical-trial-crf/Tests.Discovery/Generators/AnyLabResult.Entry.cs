using JustDummies;

namespace ClinicalTrial.Domain;

public static class AnyLabResultEntry {

    extension(Any) {

        public static AnyLabResult LabResult() {
            return new AnyLabResult();
        }

    }

}

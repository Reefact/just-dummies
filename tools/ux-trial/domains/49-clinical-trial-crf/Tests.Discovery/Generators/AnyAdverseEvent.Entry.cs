using JustDummies;

namespace ClinicalTrial.Domain;

public static class AnyAdverseEventEntry {

    extension(Any) {

        public static AnyAdverseEvent AdverseEvent() {
            return new AnyAdverseEvent();
        }

    }

}

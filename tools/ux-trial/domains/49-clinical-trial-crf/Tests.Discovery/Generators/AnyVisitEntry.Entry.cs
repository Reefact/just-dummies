using JustDummies;

namespace ClinicalTrial.Domain;

public static class AnyVisitEntryEntry {

    extension(Any) {

        public static AnyVisitEntry VisitEntry() {
            return new AnyVisitEntry();
        }

    }

}

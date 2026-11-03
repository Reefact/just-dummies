using JustDummies;

namespace ClinicalTrial.Domain;

public static class AnyVitalSignsEntry {

    extension(Any) {

        public static AnyVitalSigns VitalSigns() {
            return new AnyVitalSigns();
        }

    }

}

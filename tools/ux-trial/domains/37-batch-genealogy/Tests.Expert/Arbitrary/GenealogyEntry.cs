using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>Recipes for the domain's identifiers that are plain strings rather than value objects.</summary>
public static class GenealogyEntry {

    extension(Any) {

        /// <summary>A manufacturing site code: four upper-case letters.</summary>
        public static AnyPattern SiteCode() {
            return Any.StringMatching("[A-Z]{4}");
        }

        /// <summary>A transformation id: TRF- followed by six digits.</summary>
        public static AnyPattern TransformationId() {
            return Any.StringMatching("TRF-[0-9]{6}");
        }

    }

}

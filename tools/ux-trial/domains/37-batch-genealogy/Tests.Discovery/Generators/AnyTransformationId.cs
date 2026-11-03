using JustDummies;

namespace BatchGenealogy.Domain;

/// <summary>The identifier alone, for the tests that build their own transformation.</summary>
public static class AnyTransformationIdEntry {

    extension(Any) {

        public static AnyPattern TransformationId() {
            return Any.StringMatching("TRF-[0-9]{6}");
        }

    }

}

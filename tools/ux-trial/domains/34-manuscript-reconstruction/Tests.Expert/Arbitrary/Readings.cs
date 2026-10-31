using JustDummies;

namespace ManuscriptReconstruction.Domain;

/// <summary>
///     The shape of a reading as the tests draw it: one to six lower-case words separated by single
///     spaces. It is already normalised, so a drawn reading compares equal to what the domain stores.
/// </summary>
public static class ReadingsEntry {

    extension(Any) {

        public static AnyPattern Reading() {
            return Any.StringMatching("[a-z]{1,12}( [a-z]{1,12}){0,5}");
        }

    }

}

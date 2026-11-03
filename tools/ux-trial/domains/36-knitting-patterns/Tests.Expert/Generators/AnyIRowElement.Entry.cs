using JustDummies;

namespace KnittingPatterns.Tests.Expert.Generators;

public static class AnyIRowElementEntry {

    extension(Any) {

        public static AnyIRowElement RowElement() {
            return new AnyIRowElement();
        }

    }

}

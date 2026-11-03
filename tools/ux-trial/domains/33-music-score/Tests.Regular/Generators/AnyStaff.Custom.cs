using JustDummies;

namespace MusicScore.Domain;

// Written by hand: reaches the valid base of the name, which the scaffolded file keeps private.
public sealed partial class AnyStaff {

    internal static AnyString ValidName() { return AnyValidName(); }

    public AnyStaff WithMeasureCount(int count) {
        AnyStaff copy = new AnyStaff(this);

        copy._measureCount = Any.Fixed(count);

        return copy;
    }

}

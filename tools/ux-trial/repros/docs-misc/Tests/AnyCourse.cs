using DocsMisc.Domain;
using JustDummies;

namespace DocsMisc.Tests;

// Same shape as JustDummies.GenAny/GeneratorEmitter.cs:315 emits: configure(AnyValidCredits()).
public sealed class AnyCourse {
    private readonly IAny<int> _credits;

    public AnyCourse() : this(AnyValidCredits()) { }

    private AnyCourse(IAny<int> credits) {
        _credits = credits;
    }

    private static AnyInt32 AnyValidCredits() {
        return Any.Int32().Between(1, 10);
    }

    public AnyCourse WithCredits(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCourse(configure(AnyValidCredits()));
    }

    public Course Generate() {
        return new Course(_credits.Generate());
    }
}

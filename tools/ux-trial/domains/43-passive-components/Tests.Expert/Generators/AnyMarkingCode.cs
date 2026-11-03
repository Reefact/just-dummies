// Scaffolded by `any` (JustDummies), then rewritten by hand: what a code may say depends on the kind it is read
// for (a four-digit code is a resistor's; the multiplier digit must keep an inductor under 1 H), so the code is
// drawn from a pattern chosen by the drawn kind. Only the numeric notation is drawn.

using System;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

public sealed partial class AnyMarkingCode : IAny<MarkingCode> {

    private IAny<ComponentKind> _kind = Any.Enum<ComponentKind>();
    private IAny<string>?       _code;

    public AnyMarkingCode() { }

    private AnyMarkingCode(AnyMarkingCode from) {
        _kind = from._kind;
        _code = from._code;
    }

    public AnyMarkingCode WithKind(ComponentKind value) {
        AnyMarkingCode copy = new AnyMarkingCode(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins the code's text; the kind is still drawn, and the pair is refused on the runs where it does not fit.</summary>
    public AnyMarkingCode WithCode(string value) {
        AnyMarkingCode copy = new AnyMarkingCode(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    public MarkingCode Generate() {
        ComponentKind kind = _kind.Generate();
        string        code = (_code ?? Any.StringMatching(NumericPatternOf(kind))).Generate();

        return new MarkingCode(code, kind);
    }

    private static string NumericPatternOf(ComponentKind kind) {
        return kind switch {
            ComponentKind.Capacitor => "[1-9][0-9][0-9]",
            ComponentKind.Inductor  => "[1-9][0-9][0-489]",
            _                       => "[1-9][0-9]{2}[0-9]?",
        };
    }

}

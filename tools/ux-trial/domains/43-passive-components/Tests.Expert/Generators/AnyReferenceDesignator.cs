// Scaffolded by `any` (JustDummies), then edited: the scaffold drew Any.String().NotBlank() and marked it "to
// verify" (the format guard is a Regex it does not read). The base is now the format itself, an AnyPattern, retyped
// on the three lines the documentation names.

using System;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

public sealed partial class AnyReferenceDesignator : IAny<ReferenceDesignator> {

    private IAny<string> _value;

    public AnyReferenceDesignator() {
        _value = AnyValidValue();
    }

    private static AnyPattern AnyValidValue() {
        return Any.StringMatching("[CLR][1-9][0-9]{0,3}");
    }

    private AnyReferenceDesignator(AnyReferenceDesignator from) {
        _value = from._value;
    }

    public AnyReferenceDesignator WithValue(string value) {
        AnyReferenceDesignator copy = new AnyReferenceDesignator(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Narrows the draw to the designators of one kind: the format states it once, here.</summary>
    public AnyReferenceDesignator WithKind(ComponentKind kind) {
        AnyReferenceDesignator copy = new AnyReferenceDesignator(this);

        copy._value = Any.StringMatching(LetterOf(kind) + "[1-9][0-9]{0,3}");

        return copy;
    }

    public ReferenceDesignator Generate() {
        return new ReferenceDesignator(_value.Generate());
    }

    private static string LetterOf(ComponentKind kind) {
        return kind switch {
            ComponentKind.Capacitor => "C",
            ComponentKind.Inductor  => "L",
            _                       => "R",
        };
    }

}

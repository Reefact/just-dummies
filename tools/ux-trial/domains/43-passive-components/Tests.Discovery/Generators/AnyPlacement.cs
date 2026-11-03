using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     Hand-written: a placement's designator letter must match its part's kind, a tie `any` cannot draw.
///     The part is drawn first, the designator is built from its kind.
/// </summary>
public sealed partial class AnyPlacement : IAny<Placement> {

    private readonly AnyPart              _part;
    private readonly ReferenceDesignator? _designator;

    public AnyPlacement() : this(new AnyPart(), null) { }

    private AnyPlacement(AnyPart part, ReferenceDesignator? designator) {
        _part       = part;
        _designator = designator;
    }

    /// <summary>Refines the part; the designator follows its kind.</summary>
    public AnyPlacement WithPart(Func<AnyPart, AnyPart> configure) {
        return new AnyPlacement(configure(_part), _designator);
    }

    /// <summary>Pins the designator; the part is drawn of its kind.</summary>
    public AnyPlacement WithDesignator(ReferenceDesignator designator) {
        return new AnyPlacement(_part.WithKind(designator.Kind), designator);
    }

    public Placement Generate() {
        Part part = _part.Generate();
        ReferenceDesignator designator = _designator ?? new AnyReferenceDesignator().WithValue(_ => Any.StringMatching(LetterOf(part.Kind) + "[1-9][0-9]{0,3}")).Generate();

        return new Placement(designator, part);
    }

    private static string LetterOf(ComponentKind kind) {
        return kind switch {
            ComponentKind.Capacitor => "C",
            ComponentKind.Inductor  => "L",
            _                       => "R"
        };
    }

}

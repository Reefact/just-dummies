using System;
using System.Linq;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>Hand-written: every designator of a line takes the part's kind, none repeated.</summary>
public sealed partial class AnyBomLine : IAny<BomLine> {

    private readonly AnyPart _part;
    private readonly int     _min;
    private readonly int     _max;

    public AnyBomLine() : this(new AnyPart(), 1, 6) { }

    private AnyBomLine(AnyPart part, int min, int max) {
        _part = part;
        _min  = min;
        _max  = max;
    }

    public AnyBomLine WithPart(Func<AnyPart, AnyPart> configure) {
        return new AnyBomLine(configure(_part), _min, _max);
    }

    public AnyBomLine WithQuantity(int quantity) {
        return new AnyBomLine(_part, quantity, quantity);
    }

    public BomLine Generate() {
        Part part = _part.Generate();
        string letter = part.Kind switch {
            ComponentKind.Capacitor => "C",
            ComponentKind.Inductor  => "L",
            _                       => "R"
        };
        ReferenceDesignator[] designators = Any.SetOf(Any.Int32().Between(1, 9999))
                                               .WithCountBetween(_min, _max)
                                               .Generate()
                                               .Select(n => new ReferenceDesignator(letter + n))
                                               .ToArray();

        return new BomLine(part, designators);
    }

}

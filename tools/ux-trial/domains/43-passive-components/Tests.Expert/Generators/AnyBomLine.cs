// Scaffolded by `any` (JustDummies), then rewritten by hand: the scaffold marked both parameters "to verify"
// (designators must be at least one, distinct, and of the part's kind). The part is drawn first; its kind
// decides the designators, which are drawn as a set so they are distinct by construction.

using System;
using System.Collections.Generic;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

public sealed partial class AnyBomLine : IAny<BomLine> {

    private IAny<Part> _part     = new AnyPart();
    private int        _minCount = 1;
    private int        _maxCount = 8;

    public AnyBomLine() { }

    private AnyBomLine(AnyBomLine from) {
        _part     = from._part;
        _minCount = from._minCount;
        _maxCount = from._maxCount;
    }

    public AnyBomLine WithPart(Part value) {
        AnyBomLine copy = new AnyBomLine(this);

        copy._part = Any.Fixed(value);

        return copy;
    }

    public AnyBomLine WithPart(Func<AnyPart, AnyPart> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyBomLine copy = new AnyBomLine(this);

        copy._part = configure(new AnyPart());

        return copy;
    }

    public AnyBomLine WithQuantity(int value) {
        AnyBomLine copy = new AnyBomLine(this);

        copy._minCount = value;
        copy._maxCount = value;

        return copy;
    }

    public BomLine Generate() {
        Part                                part        = _part.Generate();
        ISet<ReferenceDesignator>           designators = Any.SetOf(new AnyReferenceDesignator().WithKind(part.Kind))
                                                             .WithCountBetween(_minCount, _maxCount)
                                                             .Generate();

        return new BomLine(part, designators);
    }

}

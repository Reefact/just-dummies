// Scaffolded by `any` (JustDummies), then edited: both parameters were marked "to verify" (an untrimmed name
// and a Regex format are guards the scaffold does not read). The board name starts and ends with a non-space,
// whatever its middle, and fits 64 characters; the revision is one or two capital letters.

using System;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

public sealed partial class AnyBillOfMaterials : IAny<BillOfMaterials> {

    private IAny<string> _boardName = Any.StringMatching("[A-Za-z0-9]([A-Za-z0-9 ._-]{0,62}[A-Za-z0-9])?");
    private IAny<string> _revision  = Any.StringMatching("[A-Z]{1,2}");

    public AnyBillOfMaterials() { }

    private AnyBillOfMaterials(AnyBillOfMaterials from) {
        _boardName = from._boardName;
        _revision  = from._revision;
    }

    public AnyBillOfMaterials WithBoardName(string value) {
        AnyBillOfMaterials copy = new AnyBillOfMaterials(this);

        copy._boardName = Any.Fixed(value);

        return copy;
    }

    public AnyBillOfMaterials WithRevision(string value) {
        AnyBillOfMaterials copy = new AnyBillOfMaterials(this);

        copy._revision = Any.Fixed(value);

        return copy;
    }

    public BillOfMaterials Generate() {
        return new BillOfMaterials(_boardName.Generate(), _revision.Generate());
    }

}

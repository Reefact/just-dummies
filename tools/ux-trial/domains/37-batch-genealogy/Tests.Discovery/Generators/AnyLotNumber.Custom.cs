using JustDummies;

namespace BatchGenealogy.Domain;

// Written by hand, beside the scaffolded file: `any generate LotNumber --force` leaves it alone.
public sealed partial class AnyLotNumber {

    /// <summary>A sub-lot number (<c>AAA-123456.07</c>) rather than a base lot number.</summary>
    public AnyLotNumber SubLot() {
        AnyLotNumber copy = new AnyLotNumber(this);

        copy._value = Any.StringMatching("[A-Z]{2,4}-[0-9]{6}\\.(0[1-9]|[1-9][0-9])");

        return copy;
    }

}

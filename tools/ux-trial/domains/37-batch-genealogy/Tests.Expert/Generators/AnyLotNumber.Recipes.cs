// Written by hand beside the scaffolded generator: `any generate LotNumber --force` leaves it alone.

using JustDummies;

namespace BatchGenealogy.Domain;

public static class AnyLotNumberRecipes {

    /// <summary>A lot number that is not a sub-lot: prefix, hyphen, six digits.</summary>
    public static AnyLotNumber Base(this AnyLotNumber lotNumbers) {
        return lotNumbers.WithValue(_ => Any.StringMatching(@"[A-Z]{2,4}-[0-9]{6}"));
    }

    /// <summary>A sub-lot number: the base lot number plus a two-digit index from 01 to 99.</summary>
    public static AnyLotNumber SubLot(this AnyLotNumber lotNumbers) {
        return lotNumbers.WithValue(_ => Any.StringMatching(@"[A-Z]{2,4}-[0-9]{6}\.(0[1-9]|[1-9][0-9])"));
    }

}

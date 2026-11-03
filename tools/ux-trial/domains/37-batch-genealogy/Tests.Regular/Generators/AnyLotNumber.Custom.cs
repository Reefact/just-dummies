namespace BatchGenealogy.Domain;

// Written by hand, beside the scaffold: `any generate --force` leaves it alone.
public sealed partial class AnyLotNumber {

    /// <summary>A lot number without a sub-lot suffix.</summary>
    public AnyLotNumber WhollyLot() {
        return WithValue(_ => Any.StringMatching("[A-Z]{2,4}-[0-9]{6}"));
    }

    /// <summary>A sub-lot number, such as <c>API-240117.03</c>.</summary>
    public AnyLotNumber SubLot() {
        return WithValue(_ => Any.StringMatching("[A-Z]{2,4}-[0-9]{6}\\.(0[1-9]|[1-9][0-9])"));
    }

}

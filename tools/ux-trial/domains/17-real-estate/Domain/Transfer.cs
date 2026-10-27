namespace RealEstate.Domain;

/// <summary>A transfer of ownership (a "mutation") of a parcel, as published by the land registry.</summary>
public sealed class Transfer {

    /// <summary>The usual combined transfer duty rate on older property: 5.80665 %.</summary>
    public const decimal DutyRate = 0.0580665m;

    public Transfer(CadastralReference parcel, decimal price, DateOnly signedOn, DateOnly registeredOn) {
        ArgumentNullException.ThrowIfNull(parcel);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(price, 1_000_000_000m);
        ArgumentOutOfRangeException.ThrowIfLessThan(registeredOn, signedOn);
        Parcel = parcel;
        Price = price;
        SignedOn = signedOn;
        RegisteredOn = registeredOn;
    }

    public CadastralReference Parcel { get; }

    public decimal Price { get; }

    public DateOnly SignedOn { get; }

    public DateOnly RegisteredOn { get; }

    public decimal TransferDuty => Math.Round(Price * DutyRate, 2, MidpointRounding.AwayFromZero);

    public int RegistrationDelayDays => RegisteredOn.DayNumber - SignedOn.DayNumber;

    /// <summary>A deed must be published within a month of signing, or penalties apply.</summary>
    public bool IsLate => RegistrationDelayDays > 30;
}

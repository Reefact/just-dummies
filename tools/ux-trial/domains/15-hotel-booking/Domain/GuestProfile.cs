using System.Text.RegularExpressions;

namespace Booking.Domain;

/// <summary>Everything the front desk keeps about a guest. Ten fields: a deliberately wide constructor.</summary>
public sealed class GuestProfile {

    public GuestProfile(GuestName firstName,
                        GuestName lastName,
                        string email,
                        string phone,
                        string nationality,
                        DateOnly dateOfBirth,
                        string? loyaltyNumber,
                        RoomType preferredRoomType,
                        bool marketingConsent,
                        IReadOnlyList<string> specialRequests) {
        ArgumentNullException.ThrowIfNull(firstName);
        ArgumentNullException.ThrowIfNull(lastName);
        if (!Regex.IsMatch(email, @"^[a-z0-9._-]+@[a-z0-9-]+\.[a-z]{2,6}$")) {
            throw new ArgumentException("Not an e-mail address.", nameof(email));
        }
        if (!Regex.IsMatch(phone, @"^\+[1-9][0-9]{7,14}$")) {
            throw new ArgumentException("Not an E.164 phone number.", nameof(phone));
        }
        if (nationality.Length != 2 || !nationality.All(char.IsAsciiLetterUpper)) {
            throw new ArgumentException("Not an ISO 3166 alpha-2 code.", nameof(nationality));
        }
        if (dateOfBirth > new DateOnly(2008, 1, 1) || dateOfBirth < new DateOnly(1900, 1, 1)) {
            throw new ArgumentOutOfRangeException(nameof(dateOfBirth), "A guest is an adult born after 1900.");
        }
        if (loyaltyNumber is not null && !Regex.IsMatch(loyaltyNumber, "^LY[0-9]{8}$")) {
            throw new ArgumentException("Not a loyalty number.", nameof(loyaltyNumber));
        }
        if (specialRequests.Count > 5) {
            throw new ArgumentException("At most five special requests.", nameof(specialRequests));
        }
        FirstName         = firstName;
        LastName          = lastName;
        Email             = email;
        Phone             = phone;
        Nationality       = nationality;
        DateOfBirth       = dateOfBirth;
        LoyaltyNumber     = loyaltyNumber;
        PreferredRoomType = preferredRoomType;
        MarketingConsent  = marketingConsent;
        SpecialRequests   = specialRequests;
    }

    public GuestName             FirstName         { get; }
    public GuestName             LastName          { get; }
    public string                Email             { get; }
    public string                Phone             { get; }
    public string                Nationality       { get; }
    public DateOnly              DateOfBirth       { get; }
    public string?               LoyaltyNumber     { get; }
    public RoomType              PreferredRoomType { get; }
    public bool                  MarketingConsent  { get; }
    public IReadOnlyList<string> SpecialRequests   { get; }

    public bool IsLoyaltyMember => LoyaltyNumber is not null;

    public string DisplayName => $"{LastName.Value.ToUpperInvariant()}, {FirstName.Value}";

}

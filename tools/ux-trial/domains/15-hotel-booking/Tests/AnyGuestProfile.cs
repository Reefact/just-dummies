// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GuestProfile --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Booking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GuestProfile" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGuestProfile : IAny<GuestProfile> {

    private readonly IAny<GuestName>             _firstName;
    private readonly IAny<GuestName>             _lastName;
    private readonly IAny<string>                _email;
    private readonly IAny<string>                _phone;
    private readonly IAny<string>                _nationality;
    private readonly IAny<DateOnly>              _dateOfBirth;
    private readonly IAny<string>                _loyaltyNumber;
    private readonly IAny<RoomType>              _preferredRoomType;
    private readonly IAny<bool>                  _marketingConsent;
    private readonly IAny<IReadOnlyList<string>> _specialRequests;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGuestProfile()
        : this(firstName:         AnyValidFirstName(),
               lastName:          AnyValidLastName(),
               email:             AnyValidEmail(),
               phone:             AnyValidPhone(),
               nationality:       AnyValidNationality(),
               dateOfBirth:       AnyValidDateOfBirth(),
               loyaltyNumber:     AnyValidLoyaltyNumber(),
               preferredRoomType: AnyValidPreferredRoomType(),
               marketingConsent:  AnyValidMarketingConsent(),
               specialRequests:   AnyValidSpecialRequests()) { }

    private static AnyGuestName AnyValidFirstName() {
        return new AnyGuestName();
    }

    private static AnyGuestName AnyValidLastName() {
        return new AnyGuestName();
    }

    private static AnyPattern AnyValidEmail() {
        return Any.StringMatching(@"^[a-z0-9._-]+@[a-z0-9-]+\.[a-z]{2,6}$");
    }

    private static AnyPattern AnyValidPhone() {
        return Any.StringMatching(@"^\+[1-9][0-9]{7,14}$");
    }

    private static AnyString AnyValidNationality() {
        return Any.String().Alpha().InUpperCase().WithLength(2);
    }

    private static AnyDateOnly AnyValidDateOfBirth() {
        return Any.DateOnly().Between(new DateOnly(1900, 1, 1), new DateOnly(2008, 1, 1));
    }

    private static AnyPattern AnyValidLoyaltyNumber() {
        return Any.StringMatching("^LY[0-9]{8}$");
    }

    private static AnyEnum<RoomType> AnyValidPreferredRoomType() {
        return Any.Enum<RoomType>();
    }

    private static AnyBoolean AnyValidMarketingConsent() {
        return Any.Boolean();
    }

    private static AnyList<string> AnyValidSpecialRequests() {
        return Any.ListOf(Any.String().NonEmpty()).WithMaxCount(5);
    }

    private AnyGuestProfile(IAny<GuestName>             firstName,
                            IAny<GuestName>             lastName,
                            IAny<string>                email,
                            IAny<string>                phone,
                            IAny<string>                nationality,
                            IAny<DateOnly>              dateOfBirth,
                            IAny<string>                loyaltyNumber,
                            IAny<RoomType>              preferredRoomType,
                            IAny<bool>                  marketingConsent,
                            IAny<IReadOnlyList<string>> specialRequests) {
        _firstName         = firstName;
        _lastName          = lastName;
        _email             = email;
        _phone             = phone;
        _nationality       = nationality;
        _dateOfBirth       = dateOfBirth;
        _loyaltyNumber     = loyaltyNumber;
        _preferredRoomType = preferredRoomType;
        _marketingConsent  = marketingConsent;
        _specialRequests   = specialRequests;
    }

    /// <summary>Pins <c>firstName</c> to a fixed value.</summary>
    public AnyGuestProfile WithFirstName(GuestName value) {
        return new AnyGuestProfile(new FixedValue<GuestName>(value), _lastName, _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Draws <c>firstName</c> from <c>AnyValidFirstName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithFirstName(Func<AnyGuestName, AnyGuestName> configure) {
        return new AnyGuestProfile(configure(AnyValidFirstName()), _lastName, _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Pins <c>lastName</c> to a fixed value.</summary>
    public AnyGuestProfile WithLastName(GuestName value) {
        return new AnyGuestProfile(_firstName, new FixedValue<GuestName>(value), _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Draws <c>lastName</c> from <c>AnyValidLastName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithLastName(Func<AnyGuestName, AnyGuestName> configure) {
        return new AnyGuestProfile(_firstName, configure(AnyValidLastName()), _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Pins <c>email</c> to a fixed value.</summary>
    public AnyGuestProfile WithEmail(string value) {
        return new AnyGuestProfile(_firstName, _lastName, new FixedValue<string>(value), _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Draws <c>email</c> from <c>AnyValidEmail()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithEmail(Func<AnyPattern, AnyPattern> configure) {
        return new AnyGuestProfile(_firstName, _lastName, configure(AnyValidEmail()), _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Pins <c>phone</c> to a fixed value.</summary>
    public AnyGuestProfile WithPhone(string value) {
        return new AnyGuestProfile(_firstName, _lastName, _email, new FixedValue<string>(value), _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Draws <c>phone</c> from <c>AnyValidPhone()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithPhone(Func<AnyPattern, AnyPattern> configure) {
        return new AnyGuestProfile(_firstName, _lastName, _email, configure(AnyValidPhone()), _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Pins <c>nationality</c> to a fixed value.</summary>
    public AnyGuestProfile WithNationality(string value) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, new FixedValue<string>(value), _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Draws <c>nationality</c> from <c>AnyValidNationality()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithNationality(Func<AnyString, AnyString> configure) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, configure(AnyValidNationality()), _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Pins <c>dateOfBirth</c> to a fixed value.</summary>
    public AnyGuestProfile WithDateOfBirth(DateOnly value) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, new FixedValue<DateOnly>(value), _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Draws <c>dateOfBirth</c> from <c>AnyValidDateOfBirth()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithDateOfBirth(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, configure(AnyValidDateOfBirth()), _loyaltyNumber, _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Pins <c>loyaltyNumber</c> to a fixed value.</summary>
    public AnyGuestProfile WithLoyaltyNumber(string value) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, _dateOfBirth, new FixedValue<string>(value), _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Draws <c>loyaltyNumber</c> from <c>AnyValidLoyaltyNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithLoyaltyNumber(Func<AnyPattern, AnyPattern> configure) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, _dateOfBirth, configure(AnyValidLoyaltyNumber()), _preferredRoomType, _marketingConsent, _specialRequests);
    }

    /// <summary>Pins <c>preferredRoomType</c> to a fixed value.</summary>
    public AnyGuestProfile WithPreferredRoomType(RoomType value) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, new FixedValue<RoomType>(value), _marketingConsent, _specialRequests);
    }

    /// <summary>Draws <c>preferredRoomType</c> from <c>AnyValidPreferredRoomType()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithPreferredRoomType(Func<AnyEnum<RoomType>, AnyEnum<RoomType>> configure) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, configure(AnyValidPreferredRoomType()), _marketingConsent, _specialRequests);
    }

    /// <summary>Pins <c>marketingConsent</c> to a fixed value.</summary>
    public AnyGuestProfile WithMarketingConsent(bool value) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, new FixedValue<bool>(value), _specialRequests);
    }

    /// <summary>Draws <c>marketingConsent</c> from <c>AnyValidMarketingConsent()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithMarketingConsent(Func<AnyBoolean, AnyBoolean> configure) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, configure(AnyValidMarketingConsent()), _specialRequests);
    }

    /// <summary>Pins <c>specialRequests</c> to a fixed value.</summary>
    public AnyGuestProfile WithSpecialRequests(IReadOnlyList<string> value) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, new FixedValue<IReadOnlyList<string>>(value));
    }

    /// <summary>Draws <c>specialRequests</c> from <c>AnyValidSpecialRequests()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGuestProfile WithSpecialRequests(Func<AnyList<string>, AnyList<string>> configure) {
        return new AnyGuestProfile(_firstName, _lastName, _email, _phone, _nationality, _dateOfBirth, _loyaltyNumber, _preferredRoomType, _marketingConsent, configure(AnyValidSpecialRequests()));
    }

    /// <summary>Produces one arbitrary <see cref="GuestProfile" />.</summary>
    public GuestProfile Generate() {
        return new GuestProfile(_firstName.Generate(),
                                _lastName.Generate(),
                                _email.Generate(),
                                _phone.Generate(),
                                _nationality.Generate(),
                                _dateOfBirth.Generate(),
                                _loyaltyNumber.Generate(),
                                _preferredRoomType.Generate(),
                                _marketingConsent.Generate(),
                                _specialRequests.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GuestProfile --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

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

    private IAny<GuestName>             _firstName;
    private IAny<GuestName>             _lastName;
    private IAny<string>                _email;
    private IAny<string>                _phone;
    private IAny<string>                _nationality;
    private IAny<DateOnly>              _dateOfBirth;
    private IAny<string?>               _loyaltyNumber;
    private IAny<RoomType>              _preferredRoomType;
    private IAny<bool>                  _marketingConsent;
    private IAny<IReadOnlyList<string>> _specialRequests;

    private AnyGuestName      _firstNameRecipe;
    private AnyGuestName      _lastNameRecipe;
    private AnyPattern         _emailRecipe;
    private AnyPattern         _phoneRecipe;
    private AnyPattern         _nationalityRecipe;
    private AnyDateOnly       _dateOfBirthRecipe;
    private AnyPattern         _loyaltyNumberRecipe;
    private AnyEnum<RoomType> _preferredRoomTypeRecipe;
    private AnyBoolean        _marketingConsentRecipe;
    private AnyList<string>   _specialRequestsRecipe;

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
        return Any.StringMatching(@"[a-z0-9._-]+@[a-z0-9-]+\.[a-z]{2,6}");
    }

    private static AnyPattern AnyValidPhone() {
        return Any.StringMatching(@"\+[1-9][0-9]{7,14}");
    }

    private static AnyPattern AnyValidNationality() {
        return Any.StringMatching("[A-Z]{2}");
    }

    private static AnyDateOnly AnyValidDateOfBirth() {
        return Any.DateOnly().Between(new DateOnly(1900, 1, 1), new DateOnly(2008, 1, 1));
    }

    private static AnyPattern AnyValidLoyaltyNumber() {
        return Any.StringMatching("LY[0-9]{8}");
    }

    private static AnyEnum<RoomType> AnyValidPreferredRoomType() {
        return Any.Enum<RoomType>();
    }

    private static AnyBoolean AnyValidMarketingConsent() {
        return Any.Boolean();
    }

    private static AnyList<string> AnyValidSpecialRequests() {
        return Any.ListOf(Any.String()).WithMaxCount(5);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyGuestProfile(AnyGuestName      firstName,
                            AnyGuestName      lastName,
                            AnyPattern         email,
                            AnyPattern         phone,
                            AnyPattern         nationality,
                            AnyDateOnly       dateOfBirth,
                            AnyPattern         loyaltyNumber,
                            AnyEnum<RoomType> preferredRoomType,
                            AnyBoolean        marketingConsent,
                            AnyList<string>   specialRequests) {
        _firstName               = firstName;
        _firstNameRecipe         = firstName;
        _lastName                = lastName;
        _lastNameRecipe          = lastName;
        _email                   = email;
        _emailRecipe             = email;
        _phone                   = phone;
        _phoneRecipe             = phone;
        _nationality             = nationality;
        _nationalityRecipe       = nationality;
        _dateOfBirth             = dateOfBirth;
        _dateOfBirthRecipe       = dateOfBirth;
        _loyaltyNumber           = loyaltyNumber.OrNull();
        _loyaltyNumberRecipe     = loyaltyNumber;
        _preferredRoomType       = preferredRoomType;
        _preferredRoomTypeRecipe = preferredRoomType;
        _marketingConsent        = marketingConsent;
        _marketingConsentRecipe  = marketingConsent;
        _specialRequests         = specialRequests;
        _specialRequestsRecipe   = specialRequests;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyGuestProfile(AnyGuestProfile from) {
        _firstName               = from._firstName;
        _firstNameRecipe         = from._firstNameRecipe;
        _lastName                = from._lastName;
        _lastNameRecipe          = from._lastNameRecipe;
        _email                   = from._email;
        _emailRecipe             = from._emailRecipe;
        _phone                   = from._phone;
        _phoneRecipe             = from._phoneRecipe;
        _nationality             = from._nationality;
        _nationalityRecipe       = from._nationalityRecipe;
        _dateOfBirth             = from._dateOfBirth;
        _dateOfBirthRecipe       = from._dateOfBirthRecipe;
        _loyaltyNumber           = from._loyaltyNumber;
        _loyaltyNumberRecipe     = from._loyaltyNumberRecipe;
        _preferredRoomType       = from._preferredRoomType;
        _preferredRoomTypeRecipe = from._preferredRoomTypeRecipe;
        _marketingConsent        = from._marketingConsent;
        _marketingConsentRecipe  = from._marketingConsentRecipe;
        _specialRequests         = from._specialRequests;
        _specialRequestsRecipe   = from._specialRequestsRecipe;
    }

    /// <summary>Pins <c>firstName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithFirstName(GuestName value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._firstName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>firstName</c> is drawn from — <c>AnyValidFirstName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuestName, AnyGuestName&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithFirstName(Func<AnyGuestName, AnyGuestName> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._firstNameRecipe = configure(_firstNameRecipe);
        copy._firstName       = copy._firstNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>lastName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithLastName(GuestName value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._lastName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lastName</c> is drawn from — <c>AnyValidLastName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuestName, AnyGuestName&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithLastName(Func<AnyGuestName, AnyGuestName> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._lastNameRecipe = configure(_lastNameRecipe);
        copy._lastName       = copy._lastNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>email</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithEmail(string value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._email = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>email</c> is drawn from — <c>AnyValidEmail()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithEmail(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._emailRecipe = configure(_emailRecipe);
        copy._email       = copy._emailRecipe;

        return copy;
    }

    /// <summary>Pins <c>phone</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithPhone(string value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._phone = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>phone</c> is drawn from — <c>AnyValidPhone()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithPhone(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._phoneRecipe = configure(_phoneRecipe);
        copy._phone       = copy._phoneRecipe;

        return copy;
    }

    /// <summary>Pins <c>nationality</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithNationality(string value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._nationality = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>nationality</c> is drawn from — <c>AnyValidNationality()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithNationality(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._nationalityRecipe = configure(_nationalityRecipe);
        copy._nationality       = copy._nationalityRecipe;

        return copy;
    }

    /// <summary>Pins <c>dateOfBirth</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithDateOfBirth(DateOnly value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._dateOfBirth = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>dateOfBirth</c> is drawn from — <c>AnyValidDateOfBirth()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithDateOfBirth(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._dateOfBirthRecipe = configure(_dateOfBirthRecipe);
        copy._dateOfBirth       = copy._dateOfBirthRecipe;

        return copy;
    }

    /// <summary>Pins <c>loyaltyNumber</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithLoyaltyNumber(string? value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._loyaltyNumber = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>loyaltyNumber</c> is drawn from — <c>AnyValidLoyaltyNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithLoyaltyNumber(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._loyaltyNumberRecipe = configure(_loyaltyNumberRecipe);
        copy._loyaltyNumber       = copy._loyaltyNumberRecipe;

        return copy;
    }

    /// <summary>Pins <c>loyaltyNumber</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithoutLoyaltyNumber() {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._loyaltyNumber = Any.Fixed<string?>(null);

        return copy;
    }

    /// <summary>Pins <c>preferredRoomType</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithPreferredRoomType(RoomType value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._preferredRoomType = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>preferredRoomType</c> is drawn from — <c>AnyValidPreferredRoomType()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;RoomType&gt;, AnyEnum&lt;RoomType&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithPreferredRoomType(Func<AnyEnum<RoomType>, AnyEnum<RoomType>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._preferredRoomTypeRecipe = configure(_preferredRoomTypeRecipe);
        copy._preferredRoomType       = copy._preferredRoomTypeRecipe;

        return copy;
    }

    /// <summary>Pins <c>marketingConsent</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithMarketingConsent(bool value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._marketingConsent = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>marketingConsent</c> is drawn from — <c>AnyValidMarketingConsent()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBoolean, AnyBoolean&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithMarketingConsent(Func<AnyBoolean, AnyBoolean> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._marketingConsentRecipe = configure(_marketingConsentRecipe);
        copy._marketingConsent       = copy._marketingConsentRecipe;

        return copy;
    }

    /// <summary>Pins <c>specialRequests</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyGuestProfile WithSpecialRequests(IReadOnlyList<string> value) {
        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._specialRequests = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>specialRequests</c> is drawn from — <c>AnyValidSpecialRequests()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;string&gt;, AnyList&lt;string&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyGuestProfile WithSpecialRequests(Func<AnyList<string>, AnyList<string>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGuestProfile copy = new AnyGuestProfile(this);

        copy._specialRequestsRecipe = configure(_specialRequestsRecipe);
        copy._specialRequests       = copy._specialRequestsRecipe;

        return copy;
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

}

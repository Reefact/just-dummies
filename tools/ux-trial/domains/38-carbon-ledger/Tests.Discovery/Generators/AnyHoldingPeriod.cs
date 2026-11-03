// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate HoldingPeriod --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using CarbonLedger.Domain;
using JustDummies;

namespace CarbonLedger.Tests.Discovery.Generators;

/// <summary>
///     A generator of arbitrary <see cref="HoldingPeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyHoldingPeriod : IAny<HoldingPeriod> {

    private IAny<CreditSerial>    _serial;
    private IAny<string>          _accountId;
    private IAny<DateTimeOffset>  _from;
    private IAny<DateTimeOffset?> _until;

    private AnyCreditSerial   _serialRecipe;
    private AnyString         _accountIdRecipe;
    private AnyDateTimeOffset _fromRecipe;
    private AnyDateTimeOffset _untilRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyHoldingPeriod()
        : this(serial:    AnyValidSerial(),
               accountId: AnyValidAccountId(),
               from:      AnyValidFrom(),
               until:     AnyValidUntil()) { }

    private static AnyCreditSerial AnyValidSerial() {
        return new AnyCreditSerial();
    }

    private static AnyString AnyValidAccountId() {
        return Any.String().NotBlank();
    }

    private static AnyDateTimeOffset AnyValidFrom() {
        return Any.DateTimeOffset();
    }

    private static AnyDateTimeOffset AnyValidUntil() {
        return Any.DateTimeOffset();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyHoldingPeriod(AnyCreditSerial   serial,
                             AnyString         accountId,
                             AnyDateTimeOffset from,
                             AnyDateTimeOffset until) {
        _serial          = serial;
        _serialRecipe    = serial;
        _accountId       = accountId;
        _accountIdRecipe = accountId;
        _from            = from;
        _fromRecipe      = from;
        _until           = until.OrNull();
        _untilRecipe     = until;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyHoldingPeriod(AnyHoldingPeriod from) {
        _serial          = from._serial;
        _serialRecipe    = from._serialRecipe;
        _accountId       = from._accountId;
        _accountIdRecipe = from._accountIdRecipe;
        _from            = from._from;
        _fromRecipe      = from._fromRecipe;
        _until           = from._until;
        _untilRecipe     = from._untilRecipe;
    }

    /// <summary>Pins <c>serial</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHoldingPeriod WithSerial(CreditSerial value) {
        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._serial = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>serial</c> is drawn from — <c>AnyValidSerial()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCreditSerial, AnyCreditSerial&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHoldingPeriod WithSerial(Func<AnyCreditSerial, AnyCreditSerial> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._serialRecipe = configure(_serialRecipe);
        copy._serial       = copy._serialRecipe;

        return copy;
    }

    /// <summary>Pins <c>accountId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHoldingPeriod WithAccountId(string value) {
        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._accountId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>accountId</c> is drawn from — <c>AnyValidAccountId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHoldingPeriod WithAccountId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._accountIdRecipe = configure(_accountIdRecipe);
        copy._accountId       = copy._accountIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>from</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHoldingPeriod WithFrom(DateTimeOffset value) {
        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._from = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>from</c> is drawn from — <c>AnyValidFrom()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHoldingPeriod WithFrom(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._fromRecipe = configure(_fromRecipe);
        copy._from       = copy._fromRecipe;

        return copy;
    }

    /// <summary>Pins <c>until</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHoldingPeriod WithUntil(DateTimeOffset? value) {
        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._until = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>until</c> is drawn from — <c>AnyValidUntil()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHoldingPeriod WithUntil(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._untilRecipe = configure(_untilRecipe);
        copy._until       = copy._untilRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>until</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyHoldingPeriod WithoutUntil() {
        AnyHoldingPeriod copy = new AnyHoldingPeriod(this);

        copy._until = Any.Fixed<DateTimeOffset?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="HoldingPeriod" />.</summary>
    public HoldingPeriod Generate() {
        DateTimeOffset  from  = _from.Generate();
        DateTimeOffset? until = _until.Generate();

        // A holding period never ends before it starts: when the two independent draws come out in the
        // wrong order, swap them. (Pinning both ends in the wrong order is the test's own mistake.)
        if (until is { } end && end < from) { (from, until) = (end, from); }

        return new HoldingPeriod(_serial.Generate(),
                                 _accountId.Generate(),
                                 from,
                                 until);
    }

}

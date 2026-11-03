// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CreditTransfer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Payments.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CreditTransfer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCreditTransfer : IAny<CreditTransfer> {

    private IAny<string>                 _endToEndId;
    private IAny<Party>                  _debtor;
    private IAny<Party>                  _creditor;
    private IAny<Money>                  _amount;
    private IAny<DateOnly>               _requestedExecutionDate;
    private IAny<TransferPriority>       _priority;
    private IAny<RemittanceInformation?> _remittanceInformation;

    private AnyString                 _endToEndIdRecipe;
    private AnyParty                  _debtorRecipe;
    private AnyParty                  _creditorRecipe;
    private AnyMoney                  _amountRecipe;
    private IAny<DateOnly>               _requestedExecutionDateRecipe;
    private AnyEnum<TransferPriority> _priorityRecipe;
    private AnyRemittanceInformation  _remittanceInformationRecipe;

    private bool _amountPinned;
    private bool _priorityPinned;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCreditTransfer()
        : this(endToEndId:             AnyValidEndToEndId(),
               debtor:                 AnyValidDebtor(),
               creditor:               AnyValidCreditor(),
               amount:                 AnyValidAmount(),
               requestedExecutionDate: AnyValidRequestedExecutionDate(),
               priority:               AnyValidPriority(),
               remittanceInformation:  AnyValidRemittanceInformation()) { }

    private static AnyString AnyValidEndToEndId() {
        return Any.String().NonEmpty().WithMaxLength(35);
    }

    private static AnyParty AnyValidDebtor() {
        return new AnyParty();
    }

    private static AnyParty AnyValidCreditor() {
        return new AnyParty();
    }

    private static AnyMoney AnyValidAmount() {
        return new AnyMoney().WithCurrency(Currency.EUR);
    }

    private static IAny<DateOnly> AnyValidRequestedExecutionDate() {
        return Any.DateOnly().As(BusinessCalendar.NextBusinessDayOnOrAfter);
    }

    private static AnyEnum<TransferPriority> AnyValidPriority() {
        return Any.Enum<TransferPriority>();
    }

    private static AnyRemittanceInformation AnyValidRemittanceInformation() {
        return new AnyRemittanceInformation();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyCreditTransfer(AnyString                 endToEndId,
                              AnyParty                  debtor,
                              AnyParty                  creditor,
                              AnyMoney                  amount,
                              IAny<DateOnly>               requestedExecutionDate,
                              AnyEnum<TransferPriority> priority,
                              AnyRemittanceInformation  remittanceInformation) {
        _endToEndId                   = endToEndId;
        _endToEndIdRecipe             = endToEndId;
        _debtor                       = debtor;
        _debtorRecipe                 = debtor;
        _creditor                     = creditor;
        _creditorRecipe               = creditor;
        _amount                       = amount;
        _amountRecipe                 = amount;
        _requestedExecutionDate       = requestedExecutionDate;
        _requestedExecutionDateRecipe = requestedExecutionDate;
        _priority                     = priority;
        _priorityRecipe               = priority;
        _remittanceInformation        = remittanceInformation.OrNull();
        _remittanceInformationRecipe  = remittanceInformation;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyCreditTransfer(AnyCreditTransfer from) {
        _endToEndId                   = from._endToEndId;
        _endToEndIdRecipe             = from._endToEndIdRecipe;
        _debtor                       = from._debtor;
        _debtorRecipe                 = from._debtorRecipe;
        _creditor                     = from._creditor;
        _creditorRecipe               = from._creditorRecipe;
        _amount                       = from._amount;
        _amountRecipe                 = from._amountRecipe;
        _requestedExecutionDate       = from._requestedExecutionDate;
        _requestedExecutionDateRecipe = from._requestedExecutionDateRecipe;
        _priority                     = from._priority;
        _priorityRecipe               = from._priorityRecipe;
        _remittanceInformation        = from._remittanceInformation;
        _remittanceInformationRecipe  = from._remittanceInformationRecipe;
        _amountPinned                 = from._amountPinned;
        _priorityPinned               = from._priorityPinned;
    }

    /// <summary>Pins <c>endToEndId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditTransfer WithEndToEndId(string value) {
        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._endToEndId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>endToEndId</c> is drawn from — <c>AnyValidEndToEndId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditTransfer WithEndToEndId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._endToEndIdRecipe = configure(_endToEndIdRecipe);
        copy._endToEndId       = copy._endToEndIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>debtor</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditTransfer WithDebtor(Party value) {
        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._debtor = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>debtor</c> is drawn from — <c>AnyValidDebtor()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyParty, AnyParty&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditTransfer WithDebtor(Func<AnyParty, AnyParty> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._debtorRecipe = configure(_debtorRecipe);
        copy._debtor       = copy._debtorRecipe;

        return copy;
    }

    /// <summary>Pins <c>creditor</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditTransfer WithCreditor(Party value) {
        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._creditor = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>creditor</c> is drawn from — <c>AnyValidCreditor()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyParty, AnyParty&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditTransfer WithCreditor(Func<AnyParty, AnyParty> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._creditorRecipe = configure(_creditorRecipe);
        copy._creditor       = copy._creditorRecipe;

        return copy;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditTransfer WithAmount(Money value) {
        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._amount       = Any.Fixed(value);
        copy._amountPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditTransfer WithAmount(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Pins <c>requestedExecutionDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditTransfer WithRequestedExecutionDate(DateOnly value) {
        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._requestedExecutionDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>requestedExecutionDate</c> is drawn from — <c>AnyValidRequestedExecutionDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;IAny<DateOnly>, IAny<DateOnly>&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditTransfer WithRequestedExecutionDate(Func<IAny<DateOnly>, IAny<DateOnly>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._requestedExecutionDateRecipe = configure(_requestedExecutionDateRecipe);
        copy._requestedExecutionDate       = copy._requestedExecutionDateRecipe;

        return copy;
    }

    /// <summary>Pins <c>priority</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditTransfer WithPriority(TransferPriority value) {
        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._priority       = Any.Fixed(value);
        copy._priorityPinned = true;

        return copy;
    }

    /// <summary>Refines the recipe <c>priority</c> is drawn from — <c>AnyValidPriority()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;TransferPriority&gt;, AnyEnum&lt;TransferPriority&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditTransfer WithPriority(Func<AnyEnum<TransferPriority>, AnyEnum<TransferPriority>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._priorityRecipe = configure(_priorityRecipe);
        copy._priority       = copy._priorityRecipe;

        return copy;
    }

    /// <summary>Pins <c>remittanceInformation</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyCreditTransfer WithRemittanceInformation(RemittanceInformation? value) {
        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._remittanceInformation = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>remittanceInformation</c> is drawn from — <c>AnyValidRemittanceInformation()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRemittanceInformation, AnyRemittanceInformation&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyCreditTransfer WithRemittanceInformation(Func<AnyRemittanceInformation, AnyRemittanceInformation> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._remittanceInformationRecipe = configure(_remittanceInformationRecipe);
        copy._remittanceInformation       = copy._remittanceInformationRecipe;

        return copy;
    }

    /// <summary>Pins <c>remittanceInformation</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyCreditTransfer WithoutRemittanceInformation() {
        AnyCreditTransfer copy = new AnyCreditTransfer(this);

        copy._remittanceInformation = Any.Fixed<RemittanceInformation?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="CreditTransfer" />.</summary>
    public CreditTransfer Generate() {
        TransferPriority priority = _priority.Generate();
        Money            amount   = _amount.Generate();

        // An instant transfer is capped. What the test pinned never moves (the constructor then refuses the
        // pair, on every run); what was drawn gives way: the amount first, the priority when the amount is pinned.
        if (priority == TransferPriority.Instant && amount.Amount > CreditTransfer.InstantCeiling) {
            if (!_amountPinned) {
                amount = _amountRecipe.WithAmount(a => a.LessThanOrEqualTo(CreditTransfer.InstantCeiling)).Generate();
            } else if (!_priorityPinned) {
                priority = TransferPriority.Normal;
            }
        }

        return new CreditTransfer(_endToEndId.Generate(),
                                  _debtor.Generate(),
                                  _creditor.Generate(),
                                  amount,
                                  _requestedExecutionDate.Generate(),
                                  priority,
                                  _remittanceInformation.Generate());
    }

}

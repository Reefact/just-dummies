// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate JournalEntry --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="JournalEntry" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyJournalEntry : IAny<JournalEntry> {

    private IAny<long>                              _sequence;
    private IAny<DateTimeOffset>                    _recordedAt;
    private IAny<EntryKind>                         _kind;
    private IAny<Account>                           _debit;
    private IAny<Account>                           _credit;
    private IAny<IReadOnlyCollection<CreditSerial>> _serials;
    private IAny<RetirementClaim?>                  _claim;

    private AnyInt64              _sequenceRecipe;
    private AnyDateTimeOffset     _recordedAtRecipe;
    private AnyEnum<EntryKind>    _kindRecipe;
    private AnyAccount            _debitRecipe;
    private AnyAccount            _creditRecipe;
    private AnyList<CreditSerial> _serialsRecipe;
    private AnyRetirementClaim    _claimRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyJournalEntry()
        : this(sequence:   AnyValidSequence(),
               recordedAt: AnyValidRecordedAt(),
               kind:       AnyValidKind(),
               debit:      AnyValidDebit(),
               credit:     AnyValidCredit(),
               serials:    AnyValidSerials(),
               claim:      AnyValidClaim()) { }

    private static AnyInt64 AnyValidSequence() {
        return Any.Int64().Positive();
    }

    private static AnyDateTimeOffset AnyValidRecordedAt() {
        return Any.DateTimeOffset();
    }

    private static AnyEnum<EntryKind> AnyValidKind() {
        return Any.Enum<EntryKind>();
    }

    private static AnyAccount AnyValidDebit() {
        return new AnyAccount();
    }

    private static AnyAccount AnyValidCredit() {
        return new AnyAccount();
    }

    private static AnyList<CreditSerial> AnyValidSerials() {
        return Any.ListOf(new AnyCreditSerial()).WithCountBetween(1, 5).Distinct();
    }

    private static AnyRetirementClaim AnyValidClaim() {
        return new AnyRetirementClaim();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyJournalEntry(AnyInt64              sequence,
                            AnyDateTimeOffset     recordedAt,
                            AnyEnum<EntryKind>    kind,
                            AnyAccount            debit,
                            AnyAccount            credit,
                            AnyList<CreditSerial> serials,
                            AnyRetirementClaim    claim) {
        _sequence         = sequence;
        _sequenceRecipe   = sequence;
        _recordedAt       = recordedAt;
        _recordedAtRecipe = recordedAt;
        _kind             = kind;
        _kindRecipe       = kind;
        _debit            = debit;
        _debitRecipe      = debit;
        _credit           = credit;
        _creditRecipe     = credit;
        _serials          = serials;
        _serialsRecipe    = serials;
        _claim            = claim.OrNull();
        _claimRecipe      = claim;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyJournalEntry(AnyJournalEntry from) {
        _sequence         = from._sequence;
        _sequenceRecipe   = from._sequenceRecipe;
        _recordedAt       = from._recordedAt;
        _recordedAtRecipe = from._recordedAtRecipe;
        _kind             = from._kind;
        _kindRecipe       = from._kindRecipe;
        _debit            = from._debit;
        _debitRecipe      = from._debitRecipe;
        _credit           = from._credit;
        _creditRecipe     = from._creditRecipe;
        _serials          = from._serials;
        _serialsRecipe    = from._serialsRecipe;
        _claim            = from._claim;
        _claimRecipe      = from._claimRecipe;
    }

    /// <summary>Pins <c>sequence</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyJournalEntry WithSequence(long value) {
        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._sequence = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sequence</c> is drawn from — <c>AnyValidSequence()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt64, AnyInt64&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyJournalEntry WithSequence(Func<AnyInt64, AnyInt64> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._sequenceRecipe = configure(_sequenceRecipe);
        copy._sequence       = copy._sequenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>recordedAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyJournalEntry WithRecordedAt(DateTimeOffset value) {
        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._recordedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>recordedAt</c> is drawn from — <c>AnyValidRecordedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTimeOffset, AnyDateTimeOffset&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyJournalEntry WithRecordedAt(Func<AnyDateTimeOffset, AnyDateTimeOffset> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._recordedAtRecipe = configure(_recordedAtRecipe);
        copy._recordedAt       = copy._recordedAtRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyJournalEntry WithKind(EntryKind value) {
        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;EntryKind&gt;, AnyEnum&lt;EntryKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyJournalEntry WithKind(Func<AnyEnum<EntryKind>, AnyEnum<EntryKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>debit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyJournalEntry WithDebit(Account value) {
        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._debit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>debit</c> is drawn from — <c>AnyValidDebit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAccount, AnyAccount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyJournalEntry WithDebit(Func<AnyAccount, AnyAccount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._debitRecipe = configure(_debitRecipe);
        copy._debit       = copy._debitRecipe;

        return copy;
    }

    /// <summary>Pins <c>credit</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyJournalEntry WithCredit(Account value) {
        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._credit = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>credit</c> is drawn from — <c>AnyValidCredit()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyAccount, AnyAccount&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyJournalEntry WithCredit(Func<AnyAccount, AnyAccount> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._creditRecipe = configure(_creditRecipe);
        copy._credit       = copy._creditRecipe;

        return copy;
    }

    /// <summary>Pins <c>serials</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyJournalEntry WithSerials(IReadOnlyCollection<CreditSerial> value) {
        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._serials = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>serials</c> is drawn from — <c>AnyValidSerials()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;CreditSerial&gt;, AnyList&lt;CreditSerial&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyJournalEntry WithSerials(Func<AnyList<CreditSerial>, AnyList<CreditSerial>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._serialsRecipe = configure(_serialsRecipe);
        copy._serials       = copy._serialsRecipe;

        return copy;
    }

    /// <summary>Pins <c>claim</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyJournalEntry WithClaim(RetirementClaim? value) {
        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._claim = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>claim</c> is drawn from — <c>AnyValidClaim()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyRetirementClaim, AnyRetirementClaim&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyJournalEntry WithClaim(Func<AnyRetirementClaim, AnyRetirementClaim> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._claimRecipe = configure(_claimRecipe);
        copy._claim       = copy._claimRecipe;

        return copy;
    }

    /// <summary>Pins <c>claim</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyJournalEntry WithoutClaim() {
        AnyJournalEntry copy = new AnyJournalEntry(this);

        copy._claim = Any.Fixed<RetirementClaim?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="JournalEntry" />.</summary>
    public JournalEntry Generate() {
        // The kind decides the kinds of both accounts and whether a claim exists: draw it first, then the rest
        // consistently with it. Hand-written (the tool cannot read these ties); pins on debit, credit and claim
        // are therefore not honoured, only their recipes.
        EntryKind kind = _kind.Generate();
        (AccountKind debitKind, AccountKind creditKind) = kind switch {
            EntryKind.Issuance => (AccountKind.Issuance, AccountKind.Holding),
            EntryKind.Transfer => (AccountKind.Holding, AccountKind.Holding),
            _                  => (AccountKind.Holding, AccountKind.Retirement)
        };

        Account debit = _debitRecipe.WithKind(debitKind).Generate();
        string  debitDigits = debit.AccountId.Substring(3);
        Account credit = _creditRecipe.WithKind(creditKind).WithAccountId(digits => digits.DifferentFrom(debitDigits)).Generate();

        IReadOnlyCollection<CreditSerial> serials = _serials.Generate();
        RetirementClaim? claim = null;
        if (kind == EntryKind.Retirement) {
            int latestVintage = 0;
            foreach (CreditSerial serial in serials) { latestVintage = Math.Max(latestVintage, serial.Vintage); }

            claim = _claimRecipe.WithReportingYear(y => y.GreaterThanOrEqualTo(latestVintage)).Generate();
        }

        return new JournalEntry(_sequence.Generate(),
                                _recordedAt.Generate(),
                                kind,
                                debit,
                                credit,
                                serials,
                                claim);
    }

}

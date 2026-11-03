// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate DispensedLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Clinical.Domain;
using HealthcareClaims.Domain;
using JustDummies;
using Payments.Domain;

namespace HealthcareClaims.Tests.Regular.Generators;

/// <summary>
///     A generator of arbitrary <see cref="DispensedLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDispensedLine : IAny<DispensedLine> {

    private IAny<PrescriptionLine>      _prescriptionLine;
    private IAny<ReimbursementCategory> _category;
    private IAny<int>                   _packs;
    private IAny<Money>                 _unitPrice;
    private IAny<Money?>?               _statutoryReimbursement;

    private AnyPrescriptionLine            _prescriptionLineRecipe;
    private AnyEnum<ReimbursementCategory> _categoryRecipe;
    private AnyInt32                       _packsRecipe;
    private AnyMoney                       _unitPriceRecipe;
    private AnyMoney                       _statutoryReimbursementRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDispensedLine()
        : this(prescriptionLine:       AnyValidPrescriptionLine(),
               category:               AnyValidCategory(),
               packs:                  AnyValidPacks(),
               unitPrice:              AnyValidUnitPrice(),
               statutoryReimbursement: AnyValidStatutoryReimbursement()) { }

    private static AnyPrescriptionLine AnyValidPrescriptionLine() {
        return new AnyPrescriptionLine();
    }

    private static AnyEnum<ReimbursementCategory> AnyValidCategory() {
        return Any.Enum<ReimbursementCategory>();
    }

    private static AnyInt32 AnyValidPacks() {
        return Any.Int32().Positive().LessThanOrEqualTo(DispensedLine.MaxPacks);
    }

    private static AnyMoney AnyValidUnitPrice() {
        return new AnyMoney().WithCurrency(Currency.EUR).WithAmount(amount => amount.LessThanOrEqualTo(DispensedLine.MaxUnitPrice));
    }

    private static AnyMoney AnyValidStatutoryReimbursement() {
        return new AnyMoney().WithCurrency(Currency.EUR);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyDispensedLine(AnyPrescriptionLine            prescriptionLine,
                             AnyEnum<ReimbursementCategory> category,
                             AnyInt32                       packs,
                             AnyMoney                       unitPrice,
                             AnyMoney                       statutoryReimbursement) {
        _prescriptionLine             = prescriptionLine;
        _prescriptionLineRecipe       = prescriptionLine;
        _category                     = category;
        _categoryRecipe               = category;
        _packs                        = packs;
        _packsRecipe                  = packs;
        _unitPrice                    = unitPrice;
        _unitPriceRecipe              = unitPrice;
        _statutoryReimbursement       = null;   // derived from the price paid in Generate(), unless pinned
        _statutoryReimbursementRecipe = statutoryReimbursement;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyDispensedLine(AnyDispensedLine from) {
        _prescriptionLine             = from._prescriptionLine;
        _prescriptionLineRecipe       = from._prescriptionLineRecipe;
        _category                     = from._category;
        _categoryRecipe               = from._categoryRecipe;
        _packs                        = from._packs;
        _packsRecipe                  = from._packsRecipe;
        _unitPrice                    = from._unitPrice;
        _unitPriceRecipe              = from._unitPriceRecipe;
        _statutoryReimbursement       = from._statutoryReimbursement;
        _statutoryReimbursementRecipe = from._statutoryReimbursementRecipe;
    }

    /// <summary>Pins <c>prescriptionLine</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDispensedLine WithPrescriptionLine(PrescriptionLine value) {
        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._prescriptionLine = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>prescriptionLine</c> is drawn from — <c>AnyValidPrescriptionLine()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPrescriptionLine, AnyPrescriptionLine&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDispensedLine WithPrescriptionLine(Func<AnyPrescriptionLine, AnyPrescriptionLine> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._prescriptionLineRecipe = configure(_prescriptionLineRecipe);
        copy._prescriptionLine       = copy._prescriptionLineRecipe;

        return copy;
    }

    /// <summary>Pins <c>category</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDispensedLine WithCategory(ReimbursementCategory value) {
        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._category = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>category</c> is drawn from — <c>AnyValidCategory()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ReimbursementCategory&gt;, AnyEnum&lt;ReimbursementCategory&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDispensedLine WithCategory(Func<AnyEnum<ReimbursementCategory>, AnyEnum<ReimbursementCategory>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._categoryRecipe = configure(_categoryRecipe);
        copy._category       = copy._categoryRecipe;

        return copy;
    }

    /// <summary>Pins <c>packs</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDispensedLine WithPacks(int value) {
        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._packs = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>packs</c> is drawn from — <c>AnyValidPacks()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDispensedLine WithPacks(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._packsRecipe = configure(_packsRecipe);
        copy._packs       = copy._packsRecipe;

        return copy;
    }

    /// <summary>Pins <c>unitPrice</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDispensedLine WithUnitPrice(Money value) {
        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._unitPrice = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>unitPrice</c> is drawn from — <c>AnyValidUnitPrice()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyMoney, AnyMoney&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyDispensedLine WithUnitPrice(Func<AnyMoney, AnyMoney> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._unitPriceRecipe = configure(_unitPriceRecipe);
        copy._unitPrice       = copy._unitPriceRecipe;

        return copy;
    }

    /// <summary>Pins <c>statutoryReimbursement</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyDispensedLine WithStatutoryReimbursement(Money? value) {
        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._statutoryReimbursement = Any.Fixed(value);

        return copy;
    }

    /// <summary>Pins <c>statutoryReimbursement</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyDispensedLine WithoutStatutoryReimbursement() {
        AnyDispensedLine copy = new AnyDispensedLine(this);

        copy._statutoryReimbursement = Any.Fixed<Money?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="DispensedLine" />.</summary>
    public DispensedLine Generate() {
        ReimbursementCategory category  = _category.Generate();
        int                   packs     = _packs.Generate();
        Money                 unitPrice = _unitPrice.Generate();

        return new DispensedLine(_prescriptionLine.Generate(),
                                 category,
                                 packs,
                                 unitPrice,
                                 _statutoryReimbursement is null ? DrawStatutoryReimbursement(category, unitPrice.Amount * packs) : _statutoryReimbursement.Generate());
    }

}

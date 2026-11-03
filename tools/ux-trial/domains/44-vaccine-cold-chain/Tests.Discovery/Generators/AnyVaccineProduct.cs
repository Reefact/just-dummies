// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VaccineProduct --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;
using Serialisation.Domain;
using Warehouse.Domain;

namespace ColdChain.Domain;

/// <summary>
///     A generator of arbitrary <see cref="VaccineProduct" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVaccineProduct : IAny<VaccineProduct> {

    private IAny<Gtin>             _gtin;
    private IAny<string>           _name;
    private IAny<TemperatureRange> _labelledRange;
    private IAny<StabilityBudget>  _budget;
    private IAny<int>              _minimumShelfLifeOnReceiptDays;

    private AnyGtin             _gtinRecipe;
    private AnyPattern          _nameRecipe;
    private AnyTemperatureRange _labelledRangeRecipe;
    private AnyStabilityBudget  _budgetRecipe;
    private AnyInt32            _minimumShelfLifeOnReceiptDaysRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVaccineProduct()
        : this(gtin:                          AnyValidGtin(),
               name:                          AnyValidName(),
               labelledRange:                 AnyValidLabelledRange(),
               budget:                        AnyValidBudget(),
               minimumShelfLifeOnReceiptDays: AnyValidMinimumShelfLifeOnReceiptDays()) { }

    private static AnyGtin AnyValidGtin() {
        return new AnyGtin();
    }

    private static AnyPattern AnyValidName() {
        // 1 to 60 characters, interior spaces allowed, none at either end.
        return Any.StringMatching("[A-Za-z0-9]([A-Za-z0-9 -]{0,58}[A-Za-z0-9])?");
    }

    private static AnyTemperatureRange AnyValidLabelledRange() {
        // The label lies inside the 2-8 degree cold chain; it may be narrower.
        return new AnyTemperatureRange().WithMinCelsius(m => m.GreaterThanOrEqualTo(VaccineProduct.ColdChainMinCelsius))
                                        .WithMaxCelsius(m => m.LessThanOrEqualTo(VaccineProduct.ColdChainMaxCelsius));
    }

    private static AnyStabilityBudget AnyValidBudget() {
        return new AnyStabilityBudget();
    }

    private static AnyInt32 AnyValidMinimumShelfLifeOnReceiptDays() {
        return Any.Int32().Between(0, 365);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyVaccineProduct(AnyGtin             gtin,
                              AnyPattern          name,
                              AnyTemperatureRange labelledRange,
                              AnyStabilityBudget  budget,
                              AnyInt32            minimumShelfLifeOnReceiptDays) {
        _gtin                                = gtin;
        _gtinRecipe                          = gtin;
        _name                                = name;
        _nameRecipe                          = name;
        _labelledRange                       = labelledRange;
        _labelledRangeRecipe                 = labelledRange;
        _budget                              = budget;
        _budgetRecipe                        = budget;
        _minimumShelfLifeOnReceiptDays       = minimumShelfLifeOnReceiptDays;
        _minimumShelfLifeOnReceiptDaysRecipe = minimumShelfLifeOnReceiptDays;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyVaccineProduct(AnyVaccineProduct from) {
        _gtin                                = from._gtin;
        _gtinRecipe                          = from._gtinRecipe;
        _name                                = from._name;
        _nameRecipe                          = from._nameRecipe;
        _labelledRange                       = from._labelledRange;
        _labelledRangeRecipe                 = from._labelledRangeRecipe;
        _budget                              = from._budget;
        _budgetRecipe                        = from._budgetRecipe;
        _minimumShelfLifeOnReceiptDays       = from._minimumShelfLifeOnReceiptDays;
        _minimumShelfLifeOnReceiptDaysRecipe = from._minimumShelfLifeOnReceiptDaysRecipe;
    }

    /// <summary>Pins <c>gtin</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVaccineProduct WithGtin(Gtin value) {
        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._gtin = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>gtin</c> is drawn from — <c>AnyValidGtin()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGtin, AnyGtin&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVaccineProduct WithGtin(Func<AnyGtin, AnyGtin> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._gtinRecipe = configure(_gtinRecipe);
        copy._gtin       = copy._gtinRecipe;

        return copy;
    }

    /// <summary>Pins <c>name</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVaccineProduct WithName(string value) {
        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>name</c> is drawn from — <c>AnyValidName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVaccineProduct WithName(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._nameRecipe = configure(_nameRecipe);
        copy._name       = copy._nameRecipe;

        return copy;
    }

    /// <summary>Pins <c>labelledRange</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVaccineProduct WithLabelledRange(TemperatureRange value) {
        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._labelledRange = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>labelledRange</c> is drawn from — <c>AnyValidLabelledRange()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperatureRange, AnyTemperatureRange&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVaccineProduct WithLabelledRange(Func<AnyTemperatureRange, AnyTemperatureRange> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._labelledRangeRecipe = configure(_labelledRangeRecipe);
        copy._labelledRange       = copy._labelledRangeRecipe;

        return copy;
    }

    /// <summary>Pins <c>budget</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVaccineProduct WithBudget(StabilityBudget value) {
        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._budget = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>budget</c> is drawn from — <c>AnyValidBudget()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStabilityBudget, AnyStabilityBudget&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVaccineProduct WithBudget(Func<AnyStabilityBudget, AnyStabilityBudget> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._budgetRecipe = configure(_budgetRecipe);
        copy._budget       = copy._budgetRecipe;

        return copy;
    }

    /// <summary>Pins <c>minimumShelfLifeOnReceiptDays</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyVaccineProduct WithMinimumShelfLifeOnReceiptDays(int value) {
        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._minimumShelfLifeOnReceiptDays = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>minimumShelfLifeOnReceiptDays</c> is drawn from — <c>AnyValidMinimumShelfLifeOnReceiptDays()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyVaccineProduct WithMinimumShelfLifeOnReceiptDays(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyVaccineProduct copy = new AnyVaccineProduct(this);

        copy._minimumShelfLifeOnReceiptDaysRecipe = configure(_minimumShelfLifeOnReceiptDaysRecipe);
        copy._minimumShelfLifeOnReceiptDays       = copy._minimumShelfLifeOnReceiptDaysRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="VaccineProduct" />.</summary>
    public VaccineProduct Generate() {
        return new VaccineProduct(_gtin.Generate(),
                                  _name.Generate(),
                                  _labelledRange.Generate(),
                                  _budget.Generate(),
                                  _minimumShelfLifeOnReceiptDays.Generate());
    }

}

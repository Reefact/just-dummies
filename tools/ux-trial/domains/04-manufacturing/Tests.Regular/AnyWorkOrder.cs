// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate WorkOrder --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="WorkOrder" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyWorkOrder : IAny<WorkOrder> {

    private IAny<BatchCode>             _batch;
    private IAny<string>                _productSku;
    private IAny<int>                   _plannedQuantity;
    private IAny<Cadence>               _cadence;
    private IAny<Tolerance>             _tolerance;
    private IAny<ShiftWindow>           _window;
    private IAny<IReadOnlyList<string>> _operators;

    private AnyBatchCode    _batchRecipe;
    private AnyString       _productSkuRecipe;
    private AnyInt32        _plannedQuantityRecipe;
    private AnyCadence      _cadenceRecipe;
    private AnyTolerance    _toleranceRecipe;
    private AnyShiftWindow  _windowRecipe;
    private AnyList<string> _operatorsRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyWorkOrder()
        : this(batch:           AnyValidBatch(),
               productSku:      AnyValidProductSku(),
               plannedQuantity: AnyValidPlannedQuantity(),
               cadence:         AnyValidCadence(),
               tolerance:       AnyValidTolerance(),
               window:          AnyValidWindow(),
               operators:       AnyValidOperators()) { }

    private static AnyBatchCode AnyValidBatch() {
        return new AnyBatchCode();
    }

    private static AnyString AnyValidProductSku() {
        return Any.String().NonEmpty().WithMaxLength(20);
    }

    private static AnyInt32 AnyValidPlannedQuantity() {
        return Any.Int32().Positive().LessThanOrEqualTo(100000);
    }

    private static AnyCadence AnyValidCadence() {
        return new AnyCadence();
    }

    private static AnyTolerance AnyValidTolerance() {
        return new AnyTolerance();
    }

    private static AnyShiftWindow AnyValidWindow() {
        return new AnyShiftWindow();
    }

    private static AnyList<string> AnyValidOperators() {
        return Any.ListOf(Any.String()).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyWorkOrder(AnyBatchCode    batch,
                         AnyString       productSku,
                         AnyInt32        plannedQuantity,
                         AnyCadence      cadence,
                         AnyTolerance    tolerance,
                         AnyShiftWindow  window,
                         AnyList<string> operators) {
        _batch                 = batch;
        _batchRecipe           = batch;
        _productSku            = productSku;
        _productSkuRecipe      = productSku;
        _plannedQuantity       = plannedQuantity;
        _plannedQuantityRecipe = plannedQuantity;
        _cadence               = cadence;
        _cadenceRecipe         = cadence;
        _tolerance             = tolerance;
        _toleranceRecipe       = tolerance;
        _window                = window;
        _windowRecipe          = window;
        _operators             = operators;
        _operatorsRecipe       = operators;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyWorkOrder(AnyWorkOrder from) {
        _batch                 = from._batch;
        _batchRecipe           = from._batchRecipe;
        _productSku            = from._productSku;
        _productSkuRecipe      = from._productSkuRecipe;
        _plannedQuantity       = from._plannedQuantity;
        _plannedQuantityRecipe = from._plannedQuantityRecipe;
        _cadence               = from._cadence;
        _cadenceRecipe         = from._cadenceRecipe;
        _tolerance             = from._tolerance;
        _toleranceRecipe       = from._toleranceRecipe;
        _window                = from._window;
        _windowRecipe          = from._windowRecipe;
        _operators             = from._operators;
        _operatorsRecipe       = from._operatorsRecipe;
    }

    /// <summary>Pins <c>batch</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkOrder WithBatch(BatchCode value) {
        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._batch = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>batch</c> is drawn from — <c>AnyValidBatch()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyBatchCode, AnyBatchCode&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkOrder WithBatch(Func<AnyBatchCode, AnyBatchCode> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._batchRecipe = configure(_batchRecipe);
        copy._batch       = copy._batchRecipe;

        return copy;
    }

    /// <summary>Pins <c>productSku</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkOrder WithProductSku(string value) {
        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._productSku = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>productSku</c> is drawn from — <c>AnyValidProductSku()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkOrder WithProductSku(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._productSkuRecipe = configure(_productSkuRecipe);
        copy._productSku       = copy._productSkuRecipe;

        return copy;
    }

    /// <summary>Pins <c>plannedQuantity</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkOrder WithPlannedQuantity(int value) {
        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._plannedQuantity = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>plannedQuantity</c> is drawn from — <c>AnyValidPlannedQuantity()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyInt32, AnyInt32&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkOrder WithPlannedQuantity(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._plannedQuantityRecipe = configure(_plannedQuantityRecipe);
        copy._plannedQuantity       = copy._plannedQuantityRecipe;

        return copy;
    }

    /// <summary>Pins <c>cadence</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkOrder WithCadence(Cadence value) {
        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._cadence = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>cadence</c> is drawn from — <c>AnyValidCadence()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCadence, AnyCadence&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkOrder WithCadence(Func<AnyCadence, AnyCadence> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._cadenceRecipe = configure(_cadenceRecipe);
        copy._cadence       = copy._cadenceRecipe;

        return copy;
    }

    /// <summary>Pins <c>tolerance</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkOrder WithTolerance(Tolerance value) {
        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._tolerance = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>tolerance</c> is drawn from — <c>AnyValidTolerance()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTolerance, AnyTolerance&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkOrder WithTolerance(Func<AnyTolerance, AnyTolerance> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._toleranceRecipe = configure(_toleranceRecipe);
        copy._tolerance       = copy._toleranceRecipe;

        return copy;
    }

    /// <summary>Pins <c>window</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkOrder WithWindow(ShiftWindow value) {
        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._window = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>window</c> is drawn from — <c>AnyValidWindow()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyShiftWindow, AnyShiftWindow&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkOrder WithWindow(Func<AnyShiftWindow, AnyShiftWindow> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._windowRecipe = configure(_windowRecipe);
        copy._window       = copy._windowRecipe;

        return copy;
    }

    /// <summary>Pins <c>operators</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyWorkOrder WithOperators(IReadOnlyList<string> value) {
        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._operators = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>operators</c> is drawn from — <c>AnyValidOperators()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;string&gt;, AnyList&lt;string&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyWorkOrder WithOperators(Func<AnyList<string>, AnyList<string>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyWorkOrder copy = new AnyWorkOrder(this);

        copy._operatorsRecipe = configure(_operatorsRecipe);
        copy._operators       = copy._operatorsRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="WorkOrder" />.</summary>
    public WorkOrder Generate() {
        return new WorkOrder(_batch.Generate(),
                             _productSku.Generate(),
                             _plannedQuantity.Generate(),
                             _cadence.Generate(),
                             _tolerance.Generate(),
                             _window.Generate(),
                             _operators.Generate());
    }

}

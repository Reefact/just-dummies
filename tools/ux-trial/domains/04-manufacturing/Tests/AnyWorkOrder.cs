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

    private readonly IAny<BatchCode>             _batch;
    private readonly IAny<string>                _productSku;
    private readonly IAny<int>                   _plannedQuantity;
    private readonly IAny<Cadence>               _cadence;
    private readonly IAny<Tolerance>             _tolerance;
    private readonly IAny<ShiftWindow>           _window;
    private readonly IAny<IReadOnlyList<string>> _operators;

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
        return Any.Int32().Between(1, 100_000);
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
        return Any.ListOf(Any.String().NonEmpty()).NonEmpty();
    }

    private AnyWorkOrder(IAny<BatchCode>             batch,
                         IAny<string>                productSku,
                         IAny<int>                   plannedQuantity,
                         IAny<Cadence>               cadence,
                         IAny<Tolerance>             tolerance,
                         IAny<ShiftWindow>           window,
                         IAny<IReadOnlyList<string>> operators) {
        _batch           = batch;
        _productSku      = productSku;
        _plannedQuantity = plannedQuantity;
        _cadence         = cadence;
        _tolerance       = tolerance;
        _window          = window;
        _operators       = operators;
    }

    /// <summary>Pins <c>batch</c> to a fixed value.</summary>
    public AnyWorkOrder WithBatch(BatchCode value) {
        return new AnyWorkOrder(new FixedValue<BatchCode>(value), _productSku, _plannedQuantity, _cadence, _tolerance, _window, _operators);
    }

    /// <summary>Draws <c>batch</c> from <c>AnyValidBatch()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWorkOrder WithBatch(Func<AnyBatchCode, AnyBatchCode> configure) {
        return new AnyWorkOrder(configure(AnyValidBatch()), _productSku, _plannedQuantity, _cadence, _tolerance, _window, _operators);
    }

    /// <summary>Pins <c>productSku</c> to a fixed value.</summary>
    public AnyWorkOrder WithProductSku(string value) {
        return new AnyWorkOrder(_batch, new FixedValue<string>(value), _plannedQuantity, _cadence, _tolerance, _window, _operators);
    }

    /// <summary>Draws <c>productSku</c> from <c>AnyValidProductSku()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWorkOrder WithProductSku(Func<AnyString, AnyString> configure) {
        return new AnyWorkOrder(_batch, configure(AnyValidProductSku()), _plannedQuantity, _cadence, _tolerance, _window, _operators);
    }

    /// <summary>Pins <c>plannedQuantity</c> to a fixed value.</summary>
    public AnyWorkOrder WithPlannedQuantity(int value) {
        return new AnyWorkOrder(_batch, _productSku, new FixedValue<int>(value), _cadence, _tolerance, _window, _operators);
    }

    /// <summary>Draws <c>plannedQuantity</c> from <c>AnyValidPlannedQuantity()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWorkOrder WithPlannedQuantity(Func<AnyInt32, AnyInt32> configure) {
        return new AnyWorkOrder(_batch, _productSku, configure(AnyValidPlannedQuantity()), _cadence, _tolerance, _window, _operators);
    }

    /// <summary>Pins <c>cadence</c> to a fixed value.</summary>
    public AnyWorkOrder WithCadence(Cadence value) {
        return new AnyWorkOrder(_batch, _productSku, _plannedQuantity, new FixedValue<Cadence>(value), _tolerance, _window, _operators);
    }

    /// <summary>Draws <c>cadence</c> from <c>AnyValidCadence()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWorkOrder WithCadence(Func<AnyCadence, AnyCadence> configure) {
        return new AnyWorkOrder(_batch, _productSku, _plannedQuantity, configure(AnyValidCadence()), _tolerance, _window, _operators);
    }

    /// <summary>Pins <c>tolerance</c> to a fixed value.</summary>
    public AnyWorkOrder WithTolerance(Tolerance value) {
        return new AnyWorkOrder(_batch, _productSku, _plannedQuantity, _cadence, new FixedValue<Tolerance>(value), _window, _operators);
    }

    /// <summary>Draws <c>tolerance</c> from <c>AnyValidTolerance()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWorkOrder WithTolerance(Func<AnyTolerance, AnyTolerance> configure) {
        return new AnyWorkOrder(_batch, _productSku, _plannedQuantity, _cadence, configure(AnyValidTolerance()), _window, _operators);
    }

    /// <summary>Pins <c>window</c> to a fixed value.</summary>
    public AnyWorkOrder WithWindow(ShiftWindow value) {
        return new AnyWorkOrder(_batch, _productSku, _plannedQuantity, _cadence, _tolerance, new FixedValue<ShiftWindow>(value), _operators);
    }

    /// <summary>Draws <c>window</c> from <c>AnyValidWindow()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWorkOrder WithWindow(Func<AnyShiftWindow, AnyShiftWindow> configure) {
        return new AnyWorkOrder(_batch, _productSku, _plannedQuantity, _cadence, _tolerance, configure(AnyValidWindow()), _operators);
    }

    /// <summary>Pins <c>operators</c> to a fixed value.</summary>
    public AnyWorkOrder WithOperators(IReadOnlyList<string> value) {
        return new AnyWorkOrder(_batch, _productSku, _plannedQuantity, _cadence, _tolerance, _window, new FixedValue<IReadOnlyList<string>>(value));
    }

    /// <summary>Draws <c>operators</c> from <c>AnyValidOperators()</c>, refined by <paramref name="configure" />.</summary>
    public AnyWorkOrder WithOperators(Func<AnyList<string>, AnyList<string>> configure) {
        return new AnyWorkOrder(_batch, _productSku, _plannedQuantity, _cadence, _tolerance, _window, configure(AnyValidOperators()));
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

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ReceiptLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ReceiptLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyReceiptLine : IAny<ReceiptLine> {

    private readonly IAny<Ean13>     _gtin;
    private readonly IAny<Quantity>  _expected;
    private readonly IAny<Quantity>  _received;
    private readonly IAny<LotNumber> _lot;
    private readonly IAny<DateOnly>  _bestBefore;
    private readonly IAny<decimal>   _unitWeightKg;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyReceiptLine()
        : this(gtin:         AnyValidGtin(),
               expected:     AnyValidExpected(),
               received:     AnyValidReceived(),
               lot:          AnyValidLot(),
               bestBefore:   AnyValidBestBefore(),
               unitWeightKg: AnyValidUnitWeightKg()) { }

    private static AnyEan13 AnyValidGtin() {
        return new AnyEan13();
    }

    private static AnyQuantity AnyValidExpected() {
        return new AnyQuantity();
    }

    private static AnyQuantity AnyValidReceived() {
        return new AnyQuantity();
    }

    private static AnyLotNumber AnyValidLot() {
        return new AnyLotNumber();
    }

    private static AnyDateOnly AnyValidBestBefore() {
        return Any.DateOnly();
    }

    private static AnyDecimal AnyValidUnitWeightKg() {
        return Any.Decimal().Positive();
    }

    private AnyReceiptLine(IAny<Ean13>     gtin,
                           IAny<Quantity>  expected,
                           IAny<Quantity>  received,
                           IAny<LotNumber> lot,
                           IAny<DateOnly>  bestBefore,
                           IAny<decimal>   unitWeightKg) {
        _gtin         = gtin;
        _expected     = expected;
        _received     = received;
        _lot          = lot;
        _bestBefore   = bestBefore;
        _unitWeightKg = unitWeightKg;
    }

    /// <summary>Pins <c>gtin</c> to a fixed value.</summary>
    public AnyReceiptLine WithGtin(Ean13 value) {
        return new AnyReceiptLine(new FixedValue<Ean13>(value), _expected, _received, _lot, _bestBefore, _unitWeightKg);
    }

    /// <summary>Draws <c>gtin</c> from <c>AnyValidGtin()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReceiptLine WithGtin(Func<AnyEan13, AnyEan13> configure) {
        return new AnyReceiptLine(configure(AnyValidGtin()), _expected, _received, _lot, _bestBefore, _unitWeightKg);
    }

    /// <summary>Pins <c>expected</c> to a fixed value.</summary>
    public AnyReceiptLine WithExpected(Quantity value) {
        return new AnyReceiptLine(_gtin, new FixedValue<Quantity>(value), _received, _lot, _bestBefore, _unitWeightKg);
    }

    /// <summary>Draws <c>expected</c> from <c>AnyValidExpected()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReceiptLine WithExpected(Func<AnyQuantity, AnyQuantity> configure) {
        return new AnyReceiptLine(_gtin, configure(AnyValidExpected()), _received, _lot, _bestBefore, _unitWeightKg);
    }

    /// <summary>Pins <c>received</c> to a fixed value.</summary>
    public AnyReceiptLine WithReceived(Quantity value) {
        return new AnyReceiptLine(_gtin, _expected, new FixedValue<Quantity>(value), _lot, _bestBefore, _unitWeightKg);
    }

    /// <summary>Draws <c>received</c> from <c>AnyValidReceived()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReceiptLine WithReceived(Func<AnyQuantity, AnyQuantity> configure) {
        return new AnyReceiptLine(_gtin, _expected, configure(AnyValidReceived()), _lot, _bestBefore, _unitWeightKg);
    }

    /// <summary>Pins <c>lot</c> to a fixed value.</summary>
    public AnyReceiptLine WithLot(LotNumber value) {
        return new AnyReceiptLine(_gtin, _expected, _received, new FixedValue<LotNumber>(value), _bestBefore, _unitWeightKg);
    }

    /// <summary>Draws <c>lot</c> from <c>AnyValidLot()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReceiptLine WithLot(Func<AnyLotNumber, AnyLotNumber> configure) {
        return new AnyReceiptLine(_gtin, _expected, _received, configure(AnyValidLot()), _bestBefore, _unitWeightKg);
    }

    /// <summary>Pins <c>bestBefore</c> to a fixed value.</summary>
    public AnyReceiptLine WithBestBefore(DateOnly value) {
        return new AnyReceiptLine(_gtin, _expected, _received, _lot, new FixedValue<DateOnly>(value), _unitWeightKg);
    }

    /// <summary>Draws <c>bestBefore</c> from <c>AnyValidBestBefore()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReceiptLine WithBestBefore(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyReceiptLine(_gtin, _expected, _received, _lot, configure(AnyValidBestBefore()), _unitWeightKg);
    }

    /// <summary>Pins <c>unitWeightKg</c> to a fixed value.</summary>
    public AnyReceiptLine WithUnitWeightKg(decimal value) {
        return new AnyReceiptLine(_gtin, _expected, _received, _lot, _bestBefore, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>unitWeightKg</c> from <c>AnyValidUnitWeightKg()</c>, refined by <paramref name="configure" />.</summary>
    public AnyReceiptLine WithUnitWeightKg(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyReceiptLine(_gtin, _expected, _received, _lot, _bestBefore, configure(AnyValidUnitWeightKg()));
    }

    /// <summary>Produces one arbitrary <see cref="ReceiptLine" />.</summary>
    public ReceiptLine Generate() {
        return new ReceiptLine(_gtin.Generate(),
                               _expected.Generate(),
                               _received.Generate(),
                               _lot.Generate(),
                               _bestBefore.Generate(),
                               _unitWeightKg.Generate());
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

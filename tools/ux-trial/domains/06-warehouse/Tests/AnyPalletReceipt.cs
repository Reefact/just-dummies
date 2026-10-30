// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PalletReceipt --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PalletReceipt" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPalletReceipt : IAny<PalletReceipt> {

    private readonly IAny<Sscc>                       _sscc;
    private readonly IAny<string>                     _supplierCode;
    private readonly IAny<DateTime>                   _receivedAt;
    private readonly IAny<TemperatureRange>           _requiredTemperature;
    private readonly IAny<IReadOnlyList<ReceiptLine>> _lines;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPalletReceipt()
        : this(sscc:                AnyValidSscc(),
               supplierCode:        AnyValidSupplierCode(),
               receivedAt:          AnyValidReceivedAt(),
               requiredTemperature: AnyValidRequiredTemperature(),
               lines:               AnyValidLines()) { }

    private static AnySscc AnyValidSscc() {
        return new AnySscc();
    }

    private static AnyString AnyValidSupplierCode() {
        // A supplier code is SUP- followed by five digits.
        return Any.String().StartingWith("SUP-").Numeric().WithLength(9);
    }

    private static AnyDateTime AnyValidReceivedAt() {
        // Receipt times are recorded in UTC.
        return Any.DateTime().Between(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private static AnyTemperatureRange AnyValidRequiredTemperature() {
        return new AnyTemperatureRange();
    }

    private static AnyList<ReceiptLine> AnyValidLines() {
        return Any.ListOf(new AnyReceiptLine()).NonEmpty();
    }

    private AnyPalletReceipt(IAny<Sscc>                       sscc,
                             IAny<string>                     supplierCode,
                             IAny<DateTime>                   receivedAt,
                             IAny<TemperatureRange>           requiredTemperature,
                             IAny<IReadOnlyList<ReceiptLine>> lines) {
        _sscc                = sscc;
        _supplierCode        = supplierCode;
        _receivedAt          = receivedAt;
        _requiredTemperature = requiredTemperature;
        _lines               = lines;
    }

    /// <summary>Pins <c>sscc</c> to a fixed value.</summary>
    public AnyPalletReceipt WithSscc(Sscc value) {
        return new AnyPalletReceipt(new FixedValue<Sscc>(value), _supplierCode, _receivedAt, _requiredTemperature, _lines);
    }

    /// <summary>Draws <c>sscc</c> from <c>AnyValidSscc()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPalletReceipt WithSscc(Func<AnySscc, AnySscc> configure) {
        return new AnyPalletReceipt(configure(AnyValidSscc()), _supplierCode, _receivedAt, _requiredTemperature, _lines);
    }

    /// <summary>Pins <c>supplierCode</c> to a fixed value.</summary>
    public AnyPalletReceipt WithSupplierCode(string value) {
        return new AnyPalletReceipt(_sscc, new FixedValue<string>(value), _receivedAt, _requiredTemperature, _lines);
    }

    /// <summary>Draws <c>supplierCode</c> from <c>AnyValidSupplierCode()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPalletReceipt WithSupplierCode(Func<AnyString, AnyString> configure) {
        return new AnyPalletReceipt(_sscc, configure(AnyValidSupplierCode()), _receivedAt, _requiredTemperature, _lines);
    }

    /// <summary>Pins <c>receivedAt</c> to a fixed value.</summary>
    public AnyPalletReceipt WithReceivedAt(DateTime value) {
        return new AnyPalletReceipt(_sscc, _supplierCode, new FixedValue<DateTime>(value), _requiredTemperature, _lines);
    }

    /// <summary>Draws <c>receivedAt</c> from <c>AnyValidReceivedAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPalletReceipt WithReceivedAt(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyPalletReceipt(_sscc, _supplierCode, configure(AnyValidReceivedAt()), _requiredTemperature, _lines);
    }

    /// <summary>Pins <c>requiredTemperature</c> to a fixed value.</summary>
    public AnyPalletReceipt WithRequiredTemperature(TemperatureRange value) {
        return new AnyPalletReceipt(_sscc, _supplierCode, _receivedAt, new FixedValue<TemperatureRange>(value), _lines);
    }

    /// <summary>Draws <c>requiredTemperature</c> from <c>AnyValidRequiredTemperature()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPalletReceipt WithRequiredTemperature(Func<AnyTemperatureRange, AnyTemperatureRange> configure) {
        return new AnyPalletReceipt(_sscc, _supplierCode, _receivedAt, configure(AnyValidRequiredTemperature()), _lines);
    }

    /// <summary>Pins <c>lines</c> to a fixed value.</summary>
    public AnyPalletReceipt WithLines(IReadOnlyList<ReceiptLine> value) {
        return new AnyPalletReceipt(_sscc, _supplierCode, _receivedAt, _requiredTemperature, new FixedValue<IReadOnlyList<ReceiptLine>>(value));
    }

    /// <summary>Draws <c>lines</c> from <c>AnyValidLines()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPalletReceipt WithLines(Func<AnyList<ReceiptLine>, AnyList<ReceiptLine>> configure) {
        return new AnyPalletReceipt(_sscc, _supplierCode, _receivedAt, _requiredTemperature, configure(AnyValidLines()));
    }

    /// <summary>Produces one arbitrary <see cref="PalletReceipt" />.</summary>
    public PalletReceipt Generate() {
        return new PalletReceipt(_sscc.Generate(),
                                 _supplierCode.Generate(),
                                 _receivedAt.Generate(),
                                 _requiredTemperature.Generate(),
                                 _lines.Generate());
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

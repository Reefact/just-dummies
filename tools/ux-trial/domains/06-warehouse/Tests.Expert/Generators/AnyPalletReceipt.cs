// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PalletReceipt --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;

using JustDummies;
using Warehouse.Domain;

namespace Warehouse.Domain.Tests.Generators;

/// <summary>
///     A generator of arbitrary <see cref="PalletReceipt" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPalletReceipt : IAny<PalletReceipt> {

    private IAny<Sscc>                       _sscc;
    private IAny<string>                     _supplierCode;
    private IAny<DateTime>                   _receivedAt;
    private IAny<TemperatureRange?>          _requiredTemperature;
    private IAny<IReadOnlyList<ReceiptLine>> _lines;

    private AnySscc              _ssccRecipe;
    private AnyPattern            _supplierCodeRecipe;
    private AnyDateTime          _receivedAtRecipe;
    private AnyTemperatureRange  _requiredTemperatureRecipe;
    private AnyList<ReceiptLine> _linesRecipe;

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

    private static AnyPattern AnyValidSupplierCode() {
        return Any.StringMatching(@"SUP-\d{5}");
    }

    private static AnyDateTime AnyValidReceivedAt() {
        return Any.DateTime();
    }

    private static AnyTemperatureRange AnyValidRequiredTemperature() {
        return new AnyTemperatureRange();
    }

    private static AnyList<ReceiptLine> AnyValidLines() {
        return Any.ListOf(new AnyReceiptLine()).NonEmpty();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPalletReceipt(AnySscc              sscc,
                             AnyPattern            supplierCode,
                             AnyDateTime          receivedAt,
                             AnyTemperatureRange  requiredTemperature,
                             AnyList<ReceiptLine> lines) {
        _sscc                      = sscc;
        _ssccRecipe                = sscc;
        _supplierCode              = supplierCode;
        _supplierCodeRecipe        = supplierCode;
        _receivedAt                = receivedAt;
        _receivedAtRecipe          = receivedAt;
        _requiredTemperature       = requiredTemperature.OrNull();
        _requiredTemperatureRecipe = requiredTemperature;
        _lines                     = lines;
        _linesRecipe               = lines;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPalletReceipt(AnyPalletReceipt from) {
        _sscc                      = from._sscc;
        _ssccRecipe                = from._ssccRecipe;
        _supplierCode              = from._supplierCode;
        _supplierCodeRecipe        = from._supplierCodeRecipe;
        _receivedAt                = from._receivedAt;
        _receivedAtRecipe          = from._receivedAtRecipe;
        _requiredTemperature       = from._requiredTemperature;
        _requiredTemperatureRecipe = from._requiredTemperatureRecipe;
        _lines                     = from._lines;
        _linesRecipe               = from._linesRecipe;
    }

    /// <summary>Pins <c>sscc</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPalletReceipt WithSscc(Sscc value) {
        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._sscc = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sscc</c> is drawn from — <c>AnyValidSscc()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySscc, AnySscc&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPalletReceipt WithSscc(Func<AnySscc, AnySscc> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._ssccRecipe = configure(_ssccRecipe);
        copy._sscc       = copy._ssccRecipe;

        return copy;
    }

    /// <summary>Pins <c>supplierCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPalletReceipt WithSupplierCode(string value) {
        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._supplierCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>supplierCode</c> is drawn from — <c>AnyValidSupplierCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPalletReceipt WithSupplierCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._supplierCodeRecipe = configure(_supplierCodeRecipe);
        copy._supplierCode       = copy._supplierCodeRecipe;

        return copy;
    }

    /// <summary>Pins <c>receivedAt</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPalletReceipt WithReceivedAt(DateTime value) {
        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._receivedAt = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>receivedAt</c> is drawn from — <c>AnyValidReceivedAt()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateTime, AnyDateTime&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPalletReceipt WithReceivedAt(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._receivedAtRecipe = configure(_receivedAtRecipe);
        copy._receivedAt       = copy._receivedAtRecipe;

        return copy;
    }

    /// <summary>Pins <c>requiredTemperature</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPalletReceipt WithRequiredTemperature(TemperatureRange? value) {
        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._requiredTemperature = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>requiredTemperature</c> is drawn from — <c>AnyValidRequiredTemperature()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyTemperatureRange, AnyTemperatureRange&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPalletReceipt WithRequiredTemperature(Func<AnyTemperatureRange, AnyTemperatureRange> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._requiredTemperatureRecipe = configure(_requiredTemperatureRecipe);
        copy._requiredTemperature       = copy._requiredTemperatureRecipe;

        return copy;
    }

    /// <summary>Pins <c>requiredTemperature</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyPalletReceipt WithoutRequiredTemperature() {
        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._requiredTemperature = Any.Fixed<TemperatureRange?>(null);

        return copy;
    }

    /// <summary>Pins <c>lines</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPalletReceipt WithLines(IReadOnlyList<ReceiptLine> value) {
        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._lines = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>lines</c> is drawn from — <c>AnyValidLines()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;ReceiptLine&gt;, AnyList&lt;ReceiptLine&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPalletReceipt WithLines(Func<AnyList<ReceiptLine>, AnyList<ReceiptLine>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPalletReceipt copy = new AnyPalletReceipt(this);

        copy._linesRecipe = configure(_linesRecipe);
        copy._lines       = copy._linesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PalletReceipt" />.</summary>
    public PalletReceipt Generate() {
        return new PalletReceipt(_sscc.Generate(),
                                 _supplierCode.Generate(),
                                 _receivedAt.Generate(),
                                 _requiredTemperature.Generate(),
                                 _lines.Generate());
    }

}

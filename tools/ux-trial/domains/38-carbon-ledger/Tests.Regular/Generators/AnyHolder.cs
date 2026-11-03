// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Holder --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace CarbonLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Holder" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyHolder : IAny<Holder> {

    private IAny<string> _holderId;
    private IAny<string> _legalName;
    private IAny<string> _countryCode;

    private AnyPattern _holderIdRecipe;
    private AnyString _legalNameRecipe;
    private AnyPattern _countryCodeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyHolder()
        : this(holderId:    AnyValidHolderId(),
               legalName:   AnyValidLegalName(),
               countryCode: AnyValidCountryCode()) { }

    private static AnyPattern AnyValidHolderId() {
        return Any.StringMatching("[A-Z0-9]{4,12}");
    }

    private static AnyString AnyValidLegalName() {
        return Any.String().NotBlank().WithMaxLength(Holder.MaxLegalNameLength);
    }

    private static AnyPattern AnyValidCountryCode() {
        return Any.StringMatching("[A-Z]{2}");
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyHolder(AnyPattern holderId,
                      AnyString legalName,
                      AnyPattern countryCode) {
        _holderId          = holderId;
        _holderIdRecipe    = holderId;
        _legalName         = legalName;
        _legalNameRecipe   = legalName;
        _countryCode       = countryCode;
        _countryCodeRecipe = countryCode;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyHolder(AnyHolder from) {
        _holderId          = from._holderId;
        _holderIdRecipe    = from._holderIdRecipe;
        _legalName         = from._legalName;
        _legalNameRecipe   = from._legalNameRecipe;
        _countryCode       = from._countryCode;
        _countryCodeRecipe = from._countryCodeRecipe;
    }

    /// <summary>Pins <c>holderId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHolder WithHolderId(string value) {
        AnyHolder copy = new AnyHolder(this);

        copy._holderId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>holderId</c> is drawn from — <c>AnyValidHolderId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHolder WithHolderId(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHolder copy = new AnyHolder(this);

        copy._holderIdRecipe = configure(_holderIdRecipe);
        copy._holderId       = copy._holderIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>legalName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHolder WithLegalName(string value) {
        AnyHolder copy = new AnyHolder(this);

        copy._legalName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>legalName</c> is drawn from — <c>AnyValidLegalName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHolder WithLegalName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHolder copy = new AnyHolder(this);

        copy._legalNameRecipe = configure(_legalNameRecipe);
        copy._legalName       = copy._legalNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>countryCode</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyHolder WithCountryCode(string value) {
        AnyHolder copy = new AnyHolder(this);

        copy._countryCode = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>countryCode</c> is drawn from — <c>AnyValidCountryCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyHolder WithCountryCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyHolder copy = new AnyHolder(this);

        copy._countryCodeRecipe = configure(_countryCodeRecipe);
        copy._countryCode       = copy._countryCodeRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Holder" />.</summary>
    public Holder Generate() {
        return new Holder(_holderId.Generate(),
                          _legalName.Generate(),
                          _countryCode.Generate());
    }

}

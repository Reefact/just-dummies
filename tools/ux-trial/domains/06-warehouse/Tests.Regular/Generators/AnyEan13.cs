// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Ean13 --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Warehouse.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Ean13" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEan13 : IAny<Ean13> {

    private IAny<string> _code;

    private IAny<string> _codeRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEan13()
        : this(code: AnyValidCode()) { }

    private static IAny<string> AnyValidCode() {
        return Any.StringMatching("[0-9]{12}").As(digits => digits + Gs1.ComputeCheckDigit(digits));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEan13(IAny<string> code) {
        _code       = code;
        _codeRecipe = code;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEan13(AnyEan13 from) {
        _code       = from._code;
        _codeRecipe = from._codeRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEan13 WithCode(string value) {
        AnyEan13 copy = new AnyEan13(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Ean13" />.</summary>
    public Ean13 Generate() {
        return new Ean13(_code.Generate());
    }

}

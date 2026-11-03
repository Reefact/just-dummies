// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MarkingCode --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace PassiveComponents.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MarkingCode" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMarkingCode : IAny<MarkingCode> {

    private IAny<string>        _code;
    private IAny<ComponentKind> _kind;

    private AnyPattern              _codeRecipe;
    private AnyEnum<ComponentKind> _kindRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMarkingCode()
        : this(code: AnyValidCode(),
               kind: AnyValidKind()) { }

    private static AnyPattern AnyValidCode() {
        // Codes every kind reads: "104" (10 x 10^4) with a multiplier of 0-4 stays within the inductor range
        // (up to 1 H), and "4R7" is read by all three kinds (R is a decimal point on a capacitor, an inductor and a resistor).
        return Any.StringMatching("[1-9][0-9][0-4]|[1-9]R[1-9]");
    }

    private static AnyEnum<ComponentKind> AnyValidKind() {
        return Any.Enum<ComponentKind>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMarkingCode(AnyPattern              code,
                           AnyEnum<ComponentKind> kind) {
        _code       = code;
        _codeRecipe = code;
        _kind       = kind;
        _kindRecipe = kind;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyMarkingCode(AnyMarkingCode from) {
        _code       = from._code;
        _codeRecipe = from._codeRecipe;
        _kind       = from._kind;
        _kindRecipe = from._kindRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMarkingCode WithCode(string value) {
        AnyMarkingCode copy = new AnyMarkingCode(this);

        copy._code = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>code</c> is drawn from — <c>AnyValidCode()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyPattern, AnyPattern&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMarkingCode WithCode(Func<AnyPattern, AnyPattern> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMarkingCode copy = new AnyMarkingCode(this);

        copy._codeRecipe = configure(_codeRecipe);
        copy._code       = copy._codeRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMarkingCode WithKind(ComponentKind value) {
        AnyMarkingCode copy = new AnyMarkingCode(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;ComponentKind&gt;, AnyEnum&lt;ComponentKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyMarkingCode WithKind(Func<AnyEnum<ComponentKind>, AnyEnum<ComponentKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyMarkingCode copy = new AnyMarkingCode(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="MarkingCode" />.</summary>
    public MarkingCode Generate() {
        return new MarkingCode(_code.Generate(),
                               _kind.Generate());
    }

}

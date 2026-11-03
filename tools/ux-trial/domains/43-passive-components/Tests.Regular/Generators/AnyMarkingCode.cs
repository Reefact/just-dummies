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
    private bool                _codePinned;
    private IAny<ComponentKind> _kind;

    private AnyString              _codeRecipe;
    private AnyEnum<ComponentKind> _kindRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMarkingCode()
        : this(code: AnyValidCode(),
               kind: AnyValidKind()) { }

    private static AnyString AnyValidCode() {
        return Any.String();
    }

    private static AnyEnum<ComponentKind> AnyValidKind() {
        return Any.Enum<ComponentKind>();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyMarkingCode(AnyString              code,
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
        _codePinned = from._codePinned;
        _kind       = from._kind;
        _kindRecipe = from._kindRecipe;
    }

    /// <summary>Pins <c>code</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyMarkingCode WithCode(string value) {
        AnyMarkingCode copy = new AnyMarkingCode(this);

        copy._code       = Any.Fixed(value);
        copy._codePinned = true;

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
        ComponentKind kind = _kind.Generate();
        string        code = _codePinned ? _code.Generate() : AnyValidCodeFor(kind).Generate();

        return new MarkingCode(code, kind);
    }

    /// <summary>
    ///     The codes a kind reads: numeric (three digits, four for a resistor) and letter notations, within
    ///     the kind's range. Inductors count in µH, so their numeric multiplier stops at 4 (99 × 10⁴ µH &lt; 1 H).
    /// </summary>
    private static AnyPattern AnyValidCodeFor(ComponentKind kind) {
        return kind switch {
            ComponentKind.Capacitor => Any.StringMatching("[1-9][0-9][0-9]|[1-9][pnuR][0-9]|[1-9][0-9]{1,2}[pnu]|R[1-9][0-9]|[1-9]m[0-9]"),
            ComponentKind.Inductor  => Any.StringMatching("[1-9][0-9][0-489]|[1-9][nuR][0-9]|[1-9][0-9]{1,2}[um]|R[1-9][0-9]|[1-9]m[0-9]|[1-9][0-9]n"),
            _                       => Any.StringMatching("[1-9][0-9]{2,3}|[1-9][0-9]?[RKkMG][0-9]{0,2}|R[1-9][0-9]")
        };
    }

}

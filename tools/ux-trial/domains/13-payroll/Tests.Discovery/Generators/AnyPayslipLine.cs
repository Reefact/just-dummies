// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PayslipLine --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Payroll.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PayslipLine" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPayslipLine : IAny<PayslipLine> {

    private IAny<string>   _label;
    private IAny<LineKind> _kind;
    private IAny<decimal>  _amount;

    private AnyString         _labelRecipe;
    private AnyEnum<LineKind> _kindRecipe;
    private AnyDecimal        _amountRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPayslipLine()
        : this(label:  AnyValidLabel(),
               kind:   AnyValidKind(),
               amount: AnyValidAmount()) { }

    private static AnyString AnyValidLabel() {
        return Any.String().NotBlank().WithMaxLength(PayslipLine.MaximumLabelLength);
    }

    private static AnyEnum<LineKind> AnyValidKind() {
        return Any.Enum<LineKind>();
    }

    private static AnyDecimal AnyValidAmount() {
        return Any.Decimal().Positive().WithScale(2);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPayslipLine(AnyString         label,
                           AnyEnum<LineKind> kind,
                           AnyDecimal        amount) {
        _label        = label;
        _labelRecipe  = label;
        _kind         = kind;
        _kindRecipe   = kind;
        _amount       = amount;
        _amountRecipe = amount;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPayslipLine(AnyPayslipLine from) {
        _label        = from._label;
        _labelRecipe  = from._labelRecipe;
        _kind         = from._kind;
        _kindRecipe   = from._kindRecipe;
        _amount       = from._amount;
        _amountRecipe = from._amountRecipe;
    }

    /// <summary>Pins <c>label</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayslipLine WithLabel(string value) {
        AnyPayslipLine copy = new AnyPayslipLine(this);

        copy._label = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>label</c> is drawn from — <c>AnyValidLabel()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPayslipLine WithLabel(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPayslipLine copy = new AnyPayslipLine(this);

        copy._labelRecipe = configure(_labelRecipe);
        copy._label       = copy._labelRecipe;

        return copy;
    }

    /// <summary>Pins <c>kind</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayslipLine WithKind(LineKind value) {
        AnyPayslipLine copy = new AnyPayslipLine(this);

        copy._kind = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>kind</c> is drawn from — <c>AnyValidKind()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;LineKind&gt;, AnyEnum&lt;LineKind&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPayslipLine WithKind(Func<AnyEnum<LineKind>, AnyEnum<LineKind>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPayslipLine copy = new AnyPayslipLine(this);

        copy._kindRecipe = configure(_kindRecipe);
        copy._kind       = copy._kindRecipe;

        return copy;
    }

    /// <summary>Pins <c>amount</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPayslipLine WithAmount(decimal value) {
        AnyPayslipLine copy = new AnyPayslipLine(this);

        copy._amount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>amount</c> is drawn from — <c>AnyValidAmount()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDecimal, AnyDecimal&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPayslipLine WithAmount(Func<AnyDecimal, AnyDecimal> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPayslipLine copy = new AnyPayslipLine(this);

        copy._amountRecipe = configure(_amountRecipe);
        copy._amount       = copy._amountRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="PayslipLine" />.</summary>
    public PayslipLine Generate() {
        return new PayslipLine(_label.Generate(),
                               _kind.Generate(),
                               _amount.Generate());
    }

}

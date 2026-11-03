// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Portfolio --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using LiquidityGap.Domain;

namespace LiquidityGap.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="Portfolio" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPortfolio : IAny<Portfolio> {

    private IAny<string>   _portfolioId;
    private IAny<Currency> _reportingCurrency;
    private IAny<DateOnly> _asOfDate;

    private AnyString   _portfolioIdRecipe;
    private AnyCurrency _reportingCurrencyRecipe;
    private AnyDateOnly _asOfDateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPortfolio()
        : this(portfolioId:       AnyValidPortfolioId(),
               reportingCurrency: AnyValidReportingCurrency(),
               asOfDate:          AnyValidAsOfDate()) { }

    private static AnyString AnyValidPortfolioId() {
        return Any.String().NotBlank();
    }

    private static AnyCurrency AnyValidReportingCurrency() {
        return new AnyCurrency();
    }

    private static AnyDateOnly AnyValidAsOfDate() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyPortfolio(AnyString   portfolioId,
                         AnyCurrency reportingCurrency,
                         AnyDateOnly asOfDate) {
        _portfolioId             = portfolioId;
        _portfolioIdRecipe       = portfolioId;
        _reportingCurrency       = reportingCurrency;
        _reportingCurrencyRecipe = reportingCurrency;
        _asOfDate                = asOfDate;
        _asOfDateRecipe          = asOfDate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyPortfolio(AnyPortfolio from) {
        _portfolioId             = from._portfolioId;
        _portfolioIdRecipe       = from._portfolioIdRecipe;
        _reportingCurrency       = from._reportingCurrency;
        _reportingCurrencyRecipe = from._reportingCurrencyRecipe;
        _asOfDate                = from._asOfDate;
        _asOfDateRecipe          = from._asOfDateRecipe;
    }

    /// <summary>Pins <c>portfolioId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPortfolio WithPortfolioId(string value) {
        AnyPortfolio copy = new AnyPortfolio(this);

        copy._portfolioId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>portfolioId</c> is drawn from — <c>AnyValidPortfolioId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPortfolio WithPortfolioId(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPortfolio copy = new AnyPortfolio(this);

        copy._portfolioIdRecipe = configure(_portfolioIdRecipe);
        copy._portfolioId       = copy._portfolioIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>reportingCurrency</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPortfolio WithReportingCurrency(Currency value) {
        AnyPortfolio copy = new AnyPortfolio(this);

        copy._reportingCurrency = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>reportingCurrency</c> is drawn from — <c>AnyValidReportingCurrency()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyCurrency, AnyCurrency&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPortfolio WithReportingCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPortfolio copy = new AnyPortfolio(this);

        copy._reportingCurrencyRecipe = configure(_reportingCurrencyRecipe);
        copy._reportingCurrency       = copy._reportingCurrencyRecipe;

        return copy;
    }

    /// <summary>Pins <c>asOfDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyPortfolio WithAsOfDate(DateOnly value) {
        AnyPortfolio copy = new AnyPortfolio(this);

        copy._asOfDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>asOfDate</c> is drawn from — <c>AnyValidAsOfDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyPortfolio WithAsOfDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPortfolio copy = new AnyPortfolio(this);

        copy._asOfDateRecipe = configure(_asOfDateRecipe);
        copy._asOfDate       = copy._asOfDateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Portfolio" />.</summary>
    public Portfolio Generate() {
        return new Portfolio(_portfolioId.Generate(),
                             _reportingCurrency.Generate(),
                             _asOfDate.Generate());
    }

}

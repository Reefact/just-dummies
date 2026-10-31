// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Portfolio --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Portfolio" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPortfolio : IAny<Portfolio> {

    private readonly IAny<string>   _portfolioId;
    private readonly IAny<Currency> _reportingCurrency;
    private readonly IAny<DateOnly> _asOfDate;

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
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2050, 12, 31));
    }

    private AnyPortfolio(IAny<string>   portfolioId,
                         IAny<Currency> reportingCurrency,
                         IAny<DateOnly> asOfDate) {
        _portfolioId       = portfolioId;
        _reportingCurrency = reportingCurrency;
        _asOfDate          = asOfDate;
    }

    /// <summary>Pins <c>portfolioId</c> to a fixed value.</summary>
    public AnyPortfolio WithPortfolioId(string value) {
        return new AnyPortfolio(new FixedValue<string>(value), _reportingCurrency, _asOfDate);
    }

    /// <summary>Draws <c>portfolioId</c> from <c>AnyValidPortfolioId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPortfolio WithPortfolioId(Func<AnyString, AnyString> configure) {
        return new AnyPortfolio(configure(AnyValidPortfolioId()), _reportingCurrency, _asOfDate);
    }

    /// <summary>Pins <c>reportingCurrency</c> to a fixed value.</summary>
    public AnyPortfolio WithReportingCurrency(Currency value) {
        return new AnyPortfolio(_portfolioId, new FixedValue<Currency>(value), _asOfDate);
    }

    /// <summary>Draws <c>reportingCurrency</c> from <c>AnyValidReportingCurrency()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPortfolio WithReportingCurrency(Func<AnyCurrency, AnyCurrency> configure) {
        return new AnyPortfolio(_portfolioId, configure(AnyValidReportingCurrency()), _asOfDate);
    }

    /// <summary>Pins <c>asOfDate</c> to a fixed value.</summary>
    public AnyPortfolio WithAsOfDate(DateOnly value) {
        return new AnyPortfolio(_portfolioId, _reportingCurrency, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>asOfDate</c> from <c>AnyValidAsOfDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPortfolio WithAsOfDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyPortfolio(_portfolioId, _reportingCurrency, configure(AnyValidAsOfDate()));
    }

    /// <summary>Produces one arbitrary <see cref="Portfolio" />.</summary>
    public Portfolio Generate() {
        return new Portfolio(_portfolioId.Generate(),
                             _reportingCurrency.Generate(),
                             _asOfDate.Generate());
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

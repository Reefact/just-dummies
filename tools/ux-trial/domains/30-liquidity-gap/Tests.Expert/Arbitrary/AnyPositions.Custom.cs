// Written by hand, beside the scaffolded generators: `any generate … --force` never touches it.

using JustDummies;

namespace LiquidityGap.Domain;

public sealed partial class AnyContractualFlow {

    /// <summary>A flow the portfolio accepts: its reporting currency, settling on or after its as-of date.</summary>
    public AnyContractualFlow BookableIn(Portfolio portfolio) {
        return WithCurrency(portfolio.ReportingCurrency)
              .WithValueDate(d => d.AfterOrEqualTo(portfolio.AsOfDate));
    }

}

public sealed partial class AnyCashFlowSchedule {

    /// <summary>A schedule in the portfolio's reporting currency.</summary>
    public AnyCashFlowSchedule BookableIn(Portfolio portfolio) {
        return WithCurrency(portfolio.ReportingCurrency);
    }

}

public sealed partial class AnyNonMaturityDeposit {

    /// <summary>A deposit in the portfolio's reporting currency.</summary>
    public AnyNonMaturityDeposit BookableIn(Portfolio portfolio) {
        return WithCurrency(portfolio.ReportingCurrency);
    }

}

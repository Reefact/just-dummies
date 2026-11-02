namespace TaxRules.Domain;

/// <summary>Kinds of taxable income; a rule's base combines one or more of them.</summary>
[Flags]
public enum IncomeKind
{
    None = 0,
    Employment = 1,
    SelfEmployment = 2,
    Investment = 4,
    All = Employment | SelfEmployment | Investment,
}

public enum InvestmentKind
{
    Interest,
    Dividend,
}

/// <summary>Whether a rule's amount adds to the tax or is a credit set against it.</summary>
public enum RuleEffect
{
    Tax,
    Credit,
}

/// <summary>What happened to a rule when a taxpayer was evaluated.</summary>
public enum LineOutcome
{
    Applied,
    NotApplicable,
    CappedAtMaximum,
    RaisedToMinimum,
}

public enum ScheduleStatus
{
    Draft,
    Validated,
}

/// <summary>Why a tax schedule refused an operation.</summary>
public enum ScheduleRefusal
{
    AlreadyValidated,
    NotValidated,
    DuplicateRuleCode,
    TooManyRules,
    UnknownRule,
    NestedRule,
    NoRules,
    NoTaxRule,
    ForeignResidencyCondition,
    TaxpayerNotBorn,
}

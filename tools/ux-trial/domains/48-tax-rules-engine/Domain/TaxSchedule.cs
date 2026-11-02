namespace TaxRules.Domain;

/// <summary>
/// The tax schedule of one jurisdiction for one year: the aggregate root. Rules are added while
/// it is a draft; validation checks the schedule as a whole and freezes it; only a validated
/// schedule evaluates taxpayers. Reopening returns it to draft for amendment.
/// </summary>
public sealed class TaxSchedule
{
    public const int FirstYear = 2000;
    public const int LastYear = 2099;
    public const int MaxRules = 64;

    private readonly List<TaxRule> rules = [];

    public TaxSchedule(JurisdictionCode jurisdiction, int year)
    {
        ArgumentNullException.ThrowIfNull(jurisdiction);
        if (year < FirstYear || year > LastYear)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, $"A schedule covers a year from {FirstYear} to {LastYear}.");
        }

        Jurisdiction = jurisdiction;
        Year = year;
    }

    public JurisdictionCode Jurisdiction { get; }

    public int Year { get; }

    public ScheduleStatus Status { get; private set; } = ScheduleStatus.Draft;

    /// <summary>The top-level rules, in the order they were added.</summary>
    public IReadOnlyList<TaxRule> Rules => rules;

    /// <summary>A rule of the schedule, top-level or nested, by code.</summary>
    public TaxRule? Find(RuleCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return rules.SelfAndDescendantsOfAll().FirstOrDefault(r => r.Code.Equals(code));
    }

    /// <summary>Adds a top-level rule. Every code in its tree must be new to the schedule.</summary>
    public void AddRule(TaxRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ThrowUnlessDraft();
        if (rules.Count == MaxRules)
        {
            throw new TaxScheduleException(ScheduleRefusal.TooManyRules, $"A schedule holds at most {MaxRules} top-level rules.");
        }

        HashSet<RuleCode> existing = rules.SelfAndDescendantsOfAll().Select(r => r.Code).ToHashSet();
        RuleCode? clash = rule.SelfAndDescendants().Select(r => r.Code).FirstOrDefault(existing.Contains);
        if (clash is not null)
        {
            throw new TaxScheduleException(ScheduleRefusal.DuplicateRuleCode, $"Rule code {clash} is already used in the {Jurisdiction} {Year} schedule.");
        }

        rules.Add(rule);
    }

    /// <summary>Removes a top-level rule with everything nested in it.</summary>
    public void RemoveRule(RuleCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        ThrowUnlessDraft();
        TaxRule? rule = rules.FirstOrDefault(r => r.Code.Equals(code));
        if (rule is not null)
        {
            rules.Remove(rule);
            return;
        }

        TaxRule? owner = rules.FirstOrDefault(r => r.SelfAndDescendants().Any(d => d.Code.Equals(code)));
        if (owner is not null)
        {
            throw new TaxScheduleException(ScheduleRefusal.NestedRule, $"Rule {code} is part of {owner.Code}; remove {owner.Code} instead.");
        }

        throw new TaxScheduleException(ScheduleRefusal.UnknownRule, $"No rule {code} in the {Jurisdiction} {Year} schedule.");
    }

    /// <summary>
    /// Checks the schedule as a whole and freezes it: at least one rule, at least one that taxes
    /// (credits alone collect nothing), and residency conditions naming only this country.
    /// </summary>
    public void Validate()
    {
        ThrowUnlessDraft();
        if (rules.Count == 0)
        {
            throw new TaxScheduleException(ScheduleRefusal.NoRules, "An empty schedule cannot be validated.");
        }

        if (rules.All(r => r.Effect == RuleEffect.Credit))
        {
            throw new TaxScheduleException(ScheduleRefusal.NoTaxRule, "A schedule of credits alone collects no tax.");
        }

        foreach (TaxRule rule in rules.SelfAndDescendantsOfAll())
        {
            if (rule.Condition is null)
            {
                continue;
            }

            ResidentOfCondition? foreign = rule.Condition.SelfAndDescendants()
                .OfType<ResidentOfCondition>()
                .FirstOrDefault(c => c.Jurisdiction.Country != Jurisdiction.Country);
            if (foreign is not null)
            {
                throw new TaxScheduleException(
                    ScheduleRefusal.ForeignResidencyCondition,
                    $"Rule {rule.Code} tests residency in {foreign.Jurisdiction}, outside {Jurisdiction.Country}.");
            }
        }

        Status = ScheduleStatus.Validated;
    }

    /// <summary>Returns a validated schedule to draft so that it can be amended.</summary>
    public void Reopen()
    {
        if (Status != ScheduleStatus.Validated)
        {
            throw new TaxScheduleException(ScheduleRefusal.NotValidated, "Only a validated schedule can be reopened.");
        }

        Status = ScheduleStatus.Draft;
    }

    /// <summary>Evaluates every top-level rule for a taxpayer, in order, and sets credits against the tax.</summary>
    public TaxBreakdown Evaluate(Taxpayer taxpayer)
    {
        ArgumentNullException.ThrowIfNull(taxpayer);
        if (Status != ScheduleStatus.Validated)
        {
            throw new TaxScheduleException(ScheduleRefusal.NotValidated, "Validate the schedule before evaluating taxpayers.");
        }

        if (taxpayer.DateOfBirth.Year > Year)
        {
            throw new TaxScheduleException(ScheduleRefusal.TaxpayerNotBorn, $"Taxpayer {taxpayer.Id} was born after {Year}.");
        }

        List<BreakdownLine> lines = rules.Select(r => r.Evaluate(taxpayer, Year)).ToList();
        return new TaxBreakdown(taxpayer.Id, Jurisdiction, Year, taxpayer.TaxableIncome(IncomeKind.All), lines);
    }

    private void ThrowUnlessDraft()
    {
        if (Status != ScheduleStatus.Draft)
        {
            throw new TaxScheduleException(ScheduleRefusal.AlreadyValidated, $"The {Jurisdiction} {Year} schedule is validated; reopen it to amend.");
        }
    }
}

# Tax rules engine — model

Namespace `TaxRules.Domain`. The tax schedule of one jurisdiction for one year, evaluated for taxpayers into a breakdown
explaining which rule produced which amount. Rules form a polymorphic, recursive tree; conditions form a recursive
expression tree. Money is `decimal`, rounded to the cent half away from zero at the end of each rule.

## Value objects
- `Money(amount)` — [0, 999 999 999 999.99], at most 2 decimals (sub-cent refused, not rounded). `RoundedFrom(decimal)` rounds
  half away from zero; `Plus`, `Minus` (refused below zero, `InvalidOperationException`), `MinusOrZero`, `Times(Rate)` (rounded),
  `Times(int ≥ 0)`, `Min`/`Max`, comparable.
- `Rate(fraction)` — [0, 1], at most 4 decimals (a hundredth of a percent). `FromPercent(p)` = p / 100 under the same rules.
- `JurisdictionCode(value)` — `^[A-Z]{2}(-[A-Z0-9]{1,3})?$` (`FR`, `US-CA`), else `FormatException`. `Country`, `IsSubdivision`;
  `Covers(other)`: equal, or this is a country and `other` one of its subdivisions.
- `RuleCode(value)` — length 3-24 (`ArgumentException`), then `^[A-Z][A-Z0-9]*(_[A-Z0-9]+)*$` (`FormatException`): upper-case words
  joined by single underscores, starting with a letter.
- `TaxpayerId(value)` — 9 ASCII digits not starting with 0 (`FormatException`), last digit the Luhn check of the first eight
  (`ArgumentException`); `CheckDigitFor(firstEight)`.
- `TaxBracket(lower, upper?, rate)` — `upper` null = open-ended; **when present, upper > lower**. Band [lower, upper).
  `Contains(income)`, `TaxOn(income)` (unrounded).
- Enums: `IncomeKind` (`[Flags]`: Employment, SelfEmployment, Investment, `All`), `InvestmentKind` (Interest, Dividend),
  `RuleEffect` (Tax, Credit), `LineOutcome` (Applied, NotApplicable, CappedAtMaximum, RaisedToMinimum), `ScheduleStatus`
  (Draft, Validated), `ScheduleRefusal`. A tax base (`IncomeKind`) is never `None` and has no unknown bit.

## Income components — abstract `IncomeComponent(source)`
Source non-blank, ≤ 100 chars. `Kind` (one flag), `TaxableAmount`. Value equality per subclass.
- `EmploymentIncome(employer, gross, expenseDeduction)` — **deduction ≤ gross**; taxable = gross − deduction.
- `SelfEmploymentIncome(activity, turnover, expenses)` — no ordering required; taxable = max(0, turnover − expenses), `Loss` the
  shortfall (not set against other income).
- `InvestmentIncome(payer, gross, investmentKind)` — defined kind; interest taxable in full, dividends at 60 % (rounded).

## Conditions — `ICondition` expression tree
`Depth` (leaf 1; combination 1 + deepest operand), `Operands`, `IsSatisfiedBy(taxpayer, taxYear)`. **`ICondition.MaxDepth` = 8**:
any combination deeper is refused (`ArgumentException`). Value equality (And/Or by operand sequence).
- `AndCondition(operands)` / `OrCondition(operands)` — at least 2 operands, no null.
- `NotCondition(operand)` — **operand cannot itself be a `NotCondition`** (double negation refused).
- `IncomeAboveCondition(kinds, threshold)` — taxable income of `kinds` strictly above `threshold`.
- `ResidentOfCondition(jurisdiction)` — `jurisdiction.Covers(taxpayer.Residence)`.
- `AgeAtLeastCondition(years)` — years in [1, 120]; age on 31 December of the tax year (= tax year − birth year).
- `HouseholdSizeAtLeastCondition(members)` — members in [2, 20].

## Rules — abstract `TaxRule(code, label, condition?)`
Label non-blank, ≤ 80 chars; `condition` null = always applies. `Effect` (Tax, or Credit for credits), `Children`,
`SelfAndDescendants()`. `Evaluate(taxpayer, taxYear)` → `BreakdownLine`: NotApplicable with zero when the condition fails, else
the subclass computes it. Each rule rounds its own amount once.
- `FlatRateRule(code, label, taxBase, rate, condition?)` — rate > 0; amount = base × rate.
- `ProgressiveRule(code, label, taxBase, brackets, condition?)` — 1-20 brackets, no null; **first starts at 0; each starts exactly
  where the previous ends (contiguous, so strictly increasing); only the last is open-ended, and it must be; rates never
  decrease**. Amount = Σ `TaxOn`, rounded once. `MarginalRateAt(income)`.
- `ThresholdExemptionRule(code, label, taxBase, threshold, rate, withdrawalCeiling?, condition?)` — threshold > 0, rate > 0,
  **ceiling > threshold** when set. Taxes the excess over the threshold; a base ≥ ceiling loses the exemption and is taxed in full.
- `CappedRule(code, label, inner, maximum, minimum?, condition?)` — **inner is not a `CappedRule` and not a credit**; maximum > 0;
  **minimum (default 0) ≤ maximum**; its code not used in the inner tree. Amount: inner capped at maximum (CappedAtMaximum); a
  positive inner below minimum is raised to it (RaisedToMinimum); a zero inner stays zero.
- `CompositeRule(code, label, components, condition?)` — 2-16 components, no null, **no credit**, **no rule code twice in the
  whole tree, own code included** (no double counting, no rule containing itself). Amount = Σ component amounts.
- `TaxCreditRule(code, label, baseAmount, perAdditionalMember, condition?)` — **not both zero**; amount = base + per-member ×
  (household − 1). Effect Credit.

## Results
- `BreakdownLine(code, label, effect, outcome, taxableBase?, amount, children?)` — defined enums, no null child, label as above;
  **NotApplicable ⇒ zero amount, no base, no children**. Base is null for caps, composites and credits. `Find(code)` in its subtree.
- `TaxBreakdown(taxpayerId, jurisdiction, year, taxableIncome, lines)` — `GrossTax` = Σ tax lines, `CreditsAvailable` = Σ credit
  lines, `CreditsApplied` = min of the two (non-refundable), `NetTax` = gross − applied, `EffectiveRate` = net / income to 4
  decimals (0 without income), `Find(code)`.

## Entity
- `Taxpayer(id, dateOfBirth, residence, householdSize, incomes)` — birth on/after 1900-01-01; household 1-20; ≤ 30 incomes, no null.
  `AgeAtEndOf(year)` (refused before the birth year), `TaxableIncome(kinds)`.

## Aggregate: `TaxSchedule(jurisdiction, year)`
Year in [2000, 2099]. Starts Draft. Refusals throw `TaxScheduleException : InvalidOperationException` carrying a `ScheduleRefusal`.
- `AddRule(rule)` — Draft only (else AlreadyValidated); ≤ 64 top-level rules (TooManyRules); **no code of the rule's tree may already
  appear anywhere in the schedule's trees** (DuplicateRuleCode).
- `RemoveRule(code)` — Draft only; a top-level rule goes with its tree; a nested code → NestedRule; absent → UnknownRule.
- `Find(code)` — top-level or nested.
- `Validate()` — Draft only (AlreadyValidated); refused with NoRules, NoTaxRule (credits only), ForeignResidencyCondition (any
  `ResidentOfCondition`, at any depth of any rule's condition, naming another country than the schedule's). Then Validated.
- `Reopen()` — Validated only (NotValidated); back to Draft.
- `Evaluate(taxpayer)` — Validated only (NotValidated); taxpayer born after the year → TaxpayerNotBorn. One line per top-level
  rule in order, with `taxableIncome` = all kinds.

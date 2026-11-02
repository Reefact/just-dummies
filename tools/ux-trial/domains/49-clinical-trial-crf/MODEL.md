# Clinical trial CRF — model

Namespace `ClinicalTrial.Domain` (`Domain/`). The electronic case report forms of one subject in a randomised trial.
Absence is first-class: a field is **blank** (`null`, not entered yet), or **resolved** — observed, or missing with a
reason, never both, never neither. Data entry accepts blanks; signing and locking require every mandatory field resolved.

## Value objects
- `Observed<T>(T? value, MissingReason? reason)` (`T : struct`: `int`, `decimal`, `DateOnly`…) — **exactly one** of value
  and reason (both or neither → `ArgumentException`); reason defined. `Of(value)`, `Missing(reason)`, `IsObserved`, `IsMissing(reason)`.
  A blank field is an `Observed<T>?` that is `null`.
- `ScreeningNumber(string)` — `CCsss-nnnn` (2 upper-case letters, 3 digits, `-`, 4 digits; else `FormatException`), e.g. `FR012-0042`;
  site `000` and sequence `0000` refused. Exposes `Country`, `Site`, `Sequence`.
- `Randomisation(number, date, arm)` — number 1-9999, arm defined. `StudyDayOf(date)`: randomisation date is day 1, the day
  before is day -1, **no day 0**; `DateOfStudyDay(day)` (day 0 refused).
- `VisitDefinition(code, targetDay, daysBefore, daysAfter, requiredAnalytes)` — code `^[A-Z][A-Z0-9]{0,7}$` (`FormatException`);
  target day 1-1095; tolerances 0-30 each; **the window cannot open before day 1** (`targetDay - daysBefore ≥ 1`);
  required analytes are analyte codes (see `LabResult`), unique. `EarliestDay`, `LatestDay`, `Contains(studyDay)`.
- `VisitSchedule(visits)` — at least one, no null, unique codes, **strictly increasing target days, and each window opens after
  the previous one closes** (`next.EarliestDay > previous.LatestDay`). `Find(code)`, `IndexOf(code)`, `VisitOn(studyDay)`.
- `VitalSigns(systolic, diastolic, heartRate : Observed<int>?, temperature, weight : Observed<decimal>?, measuredAt : TimeOnly?)` —
  observed values only are checked: systolic 50-300 mmHg, diastolic 20-200 mmHg, **systolic > diastolic** when both observed;
  **blood pressure is a pair**: one side observed while the other carries a missing reason is refused (a blank side is allowed);
  heart rate 20-250 bpm; temperature 30.0-45.0 °C and weight 1.0-400.0 kg, both to one decimal place at most;
  **a measurement time is refused when every sign is resolved and none is observed**.
  `UnresolvedFields()`: each blank sign, plus `MeasuredAt` when a sign is observed without a time.
- `LabResult(analyteCode, result : Observed<decimal>?, unit : string?, lowerLimitOfNormal : decimal?, upperLimitOfNormal : decimal?)` —
  code `^[A-Z][A-Z0-9]{1,9}$` (`FormatException`); unit, when present, 1-20 characters without whitespace; observed value ≥ 0;
  limits ≥ 0, **lower < upper** when both (either may be absent: one-sided range);
  **observed → unit required; missing with a reason → no unit and no range; blank → unit optional; a range needs a unit**.
  `Flag` (`Low`/`Normal`/`High`, `NotEvaluable` without an observed value or any limit), `IsResolved` (result not blank).
- `VisitEntry(visitDate : Observed<DateOnly>, vitals : VitalSigns?, labs)` — no null lab, each analyte once;
  **the visit date is never missing as `NotApplicable`**; **a visit that did not take place carries no vitals and no labs**. `TookPlace`, `Lab(code)`.
- `AdverseEvent(term, onsetDate, onsetTime : TimeOnly?, endDate : DateOnly?, grade, isSerious, relationship : Causality?,
  outcome : AdverseEventOutcome?, comment : string?)` — term non-blank, ≤ 200; grade 1-5 (CTCAE); **end ≥ onset**;
  enums defined; **grade 5 ⇒ serious**; **grade 5 and outcome given ⇒ outcome `Fatal`; `Fatal` ⇒ grade 5**;
  **outcome `Recovered`/`RecoveredWithSequelae`/`Fatal` ⇒ end date required; `Recovering`/`NotRecovered` ⇒ no end date**
  (`Unknown` or absent: either); comment absent or 1-500 characters of non-blank text.
  `IsOngoing`, `IsRelated` (possible/probable/definite), `DurationDays` (both ends included).
  `UnresolvedFields()`: `Relationship` until assessed; `Outcome` of a serious event; `EndDate` of a grade 5 event.
- Enums: `MissingReason` (NotDone, NotApplicable, Unknown, LostToFollowUp, Refused), `TreatmentArm`, `FormStatus`
  (Draft, Submitted, Queried, Signed, Locked), `QueryStatus` (Open, Answered, Closed), `Causality`, `AdverseEventOutcome`,
  `LabFlag`, `CasebookRefusal`.

## Entities
- `Query(number, fieldName, text, raisedOn)` — number ≥ 1; field path `Name(.Name)*`, PascalCase segments (`FormatException`),
  e.g. `Vitals.HeartRateBpm`; text non-blank, ≤ 500. Open → Answered (response non-blank ≤ 500, **answered on ≥ raised on**)
  → Closed (**closed on ≥ answered on**); the date checks throw `ArgumentOutOfRangeException`. Mutated only through the casebook.
- `VisitForm` (created by the casebook) — definition, current entry, `StudyDay` (absent if the visit did not take place),
  `Status`, `Queries`, `IsOutOfWindow`. `UnresolvedFields()`: for a visit that took place, `Vitals` (none entered) or
  `Vitals.X`, `Lab.CODE` (required analyte not reported), `Lab.CODE.Result` (blank result); nothing for a visit that did not take place.

## Aggregate: `SubjectCasebook(screeningNumber, initials : string?, consentDate, schedule)`
Initials absent or 2-3 upper-case letters (`FormatException`). Refusals throw `CasebookRefusedException`
(an `InvalidOperationException` carrying a `CasebookRefusal`); every change on a locked casebook → `CasebookLocked`.
- `Randomise(randomisation)` — `AlreadyRandomised`; date before consent → `RandomisationBeforeConsent`;
  more than 42 days after consent → `ScreeningPeriodExceeded`.
- `WindowOf(code)` — calendar dates of the window (`NotRandomised`, `UnknownVisit`).
- `RecordVisit(code, entry)` — creates or re-enters the form, keeping status and queries. Checked in order: `NotRandomised`;
  `UnknownVisit`; form not Draft/Queried → `FormNotEditable`; visit date before randomisation → `VisitBeforeRandomisation`;
  then against each other recorded visit: after an earlier visit lost to follow-up, only `LostToFollowUp` is accepted, and a visit
  cannot be lost to follow-up when a later one took place → `SubjectLostToFollowUp`; observed dates must follow schedule
  order strictly → `VisitsOutOfOrder`. **Out-of-window visits are accepted** and listed by `OutOfWindowVisits()`.
- `SubmitVisit(code)` — Draft → Submitted; Queried → Submitted once no query is Open (`OpenQueries`); else `InvalidTransition`.
- `RaiseQuery(code, field, text, raisedOn)` — on a Submitted, Queried or **Signed** form (signature lost) → Queried;
  Draft → `InvalidTransition`. Numbered from 1 per form.
- `AnswerQuery(code, n, response, on)` (`QueryNotOpen`), `CloseQuery(code, n, on)` (`QueryNotAnswered`); `UnknownQuery`.
  Any operation on a visit with no form → `VisitNotRecorded`.
- `SignVisit(code)` — only Submitted (`InvalidTransition`), every query Closed (`UnclosedQueries`), no unresolved field (`UnresolvedFields`).
- `ReportAdverseEvent(ae)` → number from 1; `ReviseAdverseEvent(n, ae)` (`UnknownAdverseEvent`). Onset before consent →
  `EventBeforeConsent`; **no event may start after the death date** (earliest end date of a `Fatal` event, `DeathDate`) → `EventAfterDeath`.
- `Lock()` — `NotRandomised`; every scheduled visit recorded and Signed (`FormsNotSigned`); no unresolved adverse-event field
  (`UnresolvedFields`). All forms become Locked.
- `UnresolvedFields()` — `CODE.path` for visit forms, `AEn.Field` for events; `IsLostToFollowUp`, `OpenQueryCount`, `Forms` (schedule order), `Form(code)`.

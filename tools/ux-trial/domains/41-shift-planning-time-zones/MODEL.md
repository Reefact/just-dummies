# Shift planning across time zones — model

Namespace `ShiftPlanning.Domain` (`Domain/`). One company roster over several sites, each in its own IANA zone.
Shifts are **planned in local wall-clock time** (date + time on the site's clock) and **stored as UTC instants**;
every labour rule is measured in **elapsed time**, so daylight-saving transitions change what is allowed.

## Value objects
- `SiteId(string)` — `AAA-nn`: 3 upper-case letters, dash, 2 digits; `-00` refused (`FormatException` on shape, `ArgumentException` on 00).
- `EmployeeId(string)` — `E` + 6 digits, `E000000` refused.
- `SiteZone(ianaId)` — not blank; IANA shape `Area/Location` (one or two `/` parts, first letter upper-case) or `UTC`, else
  `FormatException` (so Windows ids such as `Romance Standard Time` are refused); must resolve with
  `TimeZoneInfo.TryFindSystemTimeZoneById`, else `ArgumentException`. Equality by id.
  `Classify(date, time)` → `Unique`, `Skipped` (spring-forward gap) or `Ambiguous` (fall-back overlap).
  `ToUtc(date, time, choice)`: skipped → `ArgumentException`; ambiguous with `Unspecified` → `ArgumentException`;
  `Earlier` = offset before the transition (first occurrence), `Later` = offset after it; the choice is ignored for a unique time.
  `ToLocal(instant)`, `OffsetAt(instant)`.
- `WorkingHours(opens, closes, days)` — **opens < closes** (no overnight window) and **closes − opens ≥ 1 h**; at least one day,
  each defined, none listed twice. `Covers(localStart, localEnd)`: same local date, a working day, opens ≤ start, end ≤ closes (both ends inclusive).
- `IsoWeek(year, week)` — year 1..9999; **week 1..52 or 53 depending on the year** (`ISOWeek.GetWeeksInYear`).
  `Monday`, `Sunday`, `Of(date)`, `Contains(date)`, ordered.
- `PlanningPeriod(firstWeek, weekCount)` — weekCount 1..13; the period cannot run past `DateOnly.MaxValue`.
  `FirstDay` (Monday), `LastDay` (Sunday), `Contains(date)`, `Weeks()`.
- `ShiftRequest(site, startDate, startTime, endTime, startChoice = Unspecified, endChoice = Unspecified)` — times are whole
  minutes; choices defined; **end ≠ start**; an end at or before the start time means the next day (`IsOvernight`, `EndDate`);
  **wall-clock length (end − start, wrapping midnight) in [1 h, 12 h]**; an overnight request cannot start on `DateOnly.MaxValue`.
  `WallClockLength`. Value equality on all six fields.
- Enums `OffsetChoice` (Unspecified, Earlier, Later), `LocalTimeKind`, `RosterStatus` (Draft, Published), `RefusalReason`.

## Entities
- `Site(id, name, zone, workingHours)` — name not blank, ≤ 80 chars. `IsWithinWorkingHours(start, end)` reads both instants on the site's clock.
- `Employee(id, fullName, homeSite)` — name not blank, ≤ 120 chars, no leading/trailing spaces. The home site's zone decides the ISO week
  a shift counts toward and the working hours for meetings.
- `Shift(number, site, request)` — number ≥ 1; the site's id must equal the request's; both ends resolved with `SiteZone.ToUtc`
  (skipped or unchosen ambiguous ends → `ArgumentException`); **resolved end must be after resolved start**.
  `StartUtc`, `EndUtc`, `LocalStart`/`LocalEnd` (with the offset in force), `ElapsedDuration`, `WallClockLength`,
  `DaylightSavingAdjustment` = elapsed − wall clock (an 8 h night shift: −1 h across spring-forward → 7 h, +1 h across fall-back → 9 h),
  `CrossesTransition`, `AssignedTo`, `Overlaps`, `RestBetween` (elapsed, 0 when overlapping).
- `Meeting(title, start, duration, participants)` — title not blank, ≤ 120 chars; duration [15 min, 4 h], whole minutes;
  start + duration representable; 2..12 participants, no null, no duplicate. Stored as `StartUtc`/`EndUtc`.

## Aggregate: `Roster(period)`
Refusals throw `RosterRefusedException` (an `InvalidOperationException`) carrying a `RefusalReason`; null arguments throw `ArgumentNullException`.
- `RegisterSite(site)` — `DuplicateSite`. `RegisterEmployee(employee)` — home site not registered `UnknownSite`; `DuplicateEmployee`.
- `PlanShift(request)` → unstaffed `Shift` numbered 1, 2, … Checked in order: `RosterPublished`; `UnknownSite`;
  `OutsidePlanningPeriod` (local start date outside the period); `StartTimeDoesNotExist` / `StartTimeAmbiguous` (no choice);
  `EndTimeDoesNotExist` / `EndTimeAmbiguous`; `EndNotAfterStart` (offset choices that invert the shift).
- `CancelShift(number)` — `RosterPublished`, `UnknownShift`.
- `Assign(number, employee)` / `Check(number, employee)` (returns `None` when allowed). Order: `RosterPublished`; `UnknownShift`;
  `UnknownEmployee`; `ShiftAlreadyAssigned`; `OverlapsAnotherShift` (any site); `InsufficientRest` (**< 11 h elapsed** between
  this shift and any of the employee's shifts, exactly 11 h allowed); `WeeklyLimitExceeded` (**> 48 h elapsed** in the ISO week,
  exactly 48 h allowed — a shift counts wholly toward the week of its start, read in the **employee's home-site zone**, whatever site it is at).
- `Unassign(number)` — `RosterPublished`, `UnknownShift`, `ShiftNotAssigned`.
- `Publish()` — `RosterPublished` (already), `NothingToPublish` (no shift), `UnstaffedShift`; then `Status = Published`.
- `ScheduleMeeting(meeting)` — allowed in any status; each participant must be registered (`UnknownEmployee`) and the meeting must
  fall inside **their home site's working hours on its own wall clock** (`OutsideWorkingHours`).
- `MeetingSlots(utcDate, duration, participants)` — start instants every 15 min over the UTC day at which every participant's site
  covers the meeting; duration [15 min, 4 h] (`ArgumentOutOfRangeException`), at least one participant, unknown → `UnknownEmployee`.
- Computations: `ElapsedTimeIn(employee, isoWeek)`, `ShiftsOf(employee)` (by start), `ShortestRest(employee)` (null below two shifts),
  `FindShift(number)`.

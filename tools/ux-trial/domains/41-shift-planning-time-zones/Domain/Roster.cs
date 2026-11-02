namespace ShiftPlanning.Domain;

/// <summary>
/// The company roster for one planning period, across every site and zone. Shifts are planned in local
/// wall-clock time and stored as UTC instants; every labour rule is measured in elapsed time:
/// at least 11 h of rest between two shifts of an employee, at most 48 h per ISO week, the week being
/// read in the employee's home-site zone. Cross-site meetings must fall inside each participant's
/// home-site working hours.
/// </summary>
public sealed class Roster
{
    public static readonly TimeSpan MinimumRest = TimeSpan.FromHours(11);
    public static readonly TimeSpan WeeklyLimit = TimeSpan.FromHours(48);
    public static readonly TimeSpan MeetingSlotStep = TimeSpan.FromMinutes(15);

    private readonly Dictionary<SiteId, Site> _sites = new();
    private readonly Dictionary<EmployeeId, Employee> _employees = new();
    private readonly SortedDictionary<int, Shift> _shifts = new();
    private readonly List<Meeting> _meetings = new();
    private int _nextShiftNumber = 1;

    public Roster(PlanningPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);
        Period = period;
    }

    public PlanningPeriod Period { get; }

    public RosterStatus Status { get; private set; } = RosterStatus.Draft;

    public IReadOnlyCollection<Site> Sites => _sites.Values;

    public IReadOnlyCollection<Employee> Employees => _employees.Values;

    public IReadOnlyCollection<Shift> Shifts => _shifts.Values;

    public IReadOnlyList<Meeting> Meetings => _meetings;

    public void RegisterSite(Site site)
    {
        ArgumentNullException.ThrowIfNull(site);
        if (_sites.ContainsKey(site.Id))
        {
            throw new RosterRefusedException(RefusalReason.DuplicateSite, $"Site {site.Id} is already registered.");
        }

        _sites.Add(site.Id, site);
    }

    public void RegisterEmployee(Employee employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        if (!_sites.ContainsKey(employee.HomeSite.Id))
        {
            throw new RosterRefusedException(RefusalReason.UnknownSite, $"Home site {employee.HomeSite.Id} of {employee.Id} is not registered.");
        }

        if (_employees.ContainsKey(employee.Id))
        {
            throw new RosterRefusedException(RefusalReason.DuplicateEmployee, $"Employee {employee.Id} is already registered.");
        }

        _employees.Add(employee.Id, employee);
    }

    public Shift? FindShift(int number) => _shifts.GetValueOrDefault(number);

    /// <summary>Resolves the request's local times in the site's zone and adds an unstaffed shift.</summary>
    public Shift PlanShift(ShiftRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        RequireDraft();
        if (!_sites.TryGetValue(request.Site, out Site? site))
        {
            throw new RosterRefusedException(RefusalReason.UnknownSite, $"Site {request.Site} is not registered.");
        }

        if (!Period.Contains(request.StartDate))
        {
            throw new RosterRefusedException(RefusalReason.OutsidePlanningPeriod, $"{request.StartDate:yyyy-MM-dd} is outside {Period}.");
        }

        RefuseUnresolvable(site.Zone, request.StartDate, request.StartTime, request.StartChoice, RefusalReason.StartTimeDoesNotExist, RefusalReason.StartTimeAmbiguous);
        RefuseUnresolvable(site.Zone, request.EndDate, request.EndTime, request.EndChoice, RefusalReason.EndTimeDoesNotExist, RefusalReason.EndTimeAmbiguous);

        DateTimeOffset start = site.Zone.ToUtc(request.StartDate, request.StartTime, request.StartChoice);
        DateTimeOffset end = site.Zone.ToUtc(request.EndDate, request.EndTime, request.EndChoice);
        if (end <= start)
        {
            throw new RosterRefusedException(RefusalReason.EndNotAfterStart, $"With the chosen offsets, {request} would end before it starts.");
        }

        Shift shift = new(_nextShiftNumber++, site, request);
        _shifts.Add(shift.Number, shift);
        return shift;
    }

    /// <summary>Removes an unstaffed or staffed shift from the draft.</summary>
    public void CancelShift(int number)
    {
        RequireDraft();
        Shift shift = RequireShift(number);
        _shifts.Remove(shift.Number);
    }

    /// <summary>The rule that would refuse the assignment, or <see cref="RefusalReason.None"/>.</summary>
    public RefusalReason Check(int shiftNumber, EmployeeId employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        return Evaluate(shiftNumber, employee).Reason;
    }

    public void Assign(int shiftNumber, EmployeeId employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        (RefusalReason reason, string message) = Evaluate(shiftNumber, employee);
        if (reason != RefusalReason.None)
        {
            throw new RosterRefusedException(reason, $"Cannot assign {employee} to shift #{shiftNumber}: {message}");
        }

        _shifts[shiftNumber].AssignTo(employee);
    }

    public void Unassign(int shiftNumber)
    {
        RequireDraft();
        Shift shift = RequireShift(shiftNumber);
        if (!shift.IsStaffed)
        {
            throw new RosterRefusedException(RefusalReason.ShiftNotAssigned, $"Shift #{shiftNumber} has nobody assigned.");
        }

        shift.Unassign();
    }

    public IReadOnlyList<Shift> ShiftsOf(EmployeeId employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        return _shifts.Values.Where(s => employee.Equals(s.AssignedTo)).OrderBy(s => s.StartUtc).ToList();
    }

    /// <summary>
    /// Elapsed time worked in the ISO week, read in the employee's home-site zone. A shift counts entirely
    /// toward the week in which it starts on that wall clock.
    /// </summary>
    public TimeSpan ElapsedTimeIn(EmployeeId employee, IsoWeek week)
    {
        ArgumentNullException.ThrowIfNull(employee);
        ArgumentNullException.ThrowIfNull(week);
        Employee known = RequireEmployee(employee);
        return ShiftsOf(employee)
            .Where(s => WeekOf(s, known).Equals(week))
            .Aggregate(TimeSpan.Zero, (sum, s) => sum + s.ElapsedDuration);
    }

    /// <summary>The shortest elapsed rest between two consecutive shifts of the employee; null with fewer than two.</summary>
    public TimeSpan? ShortestRest(EmployeeId employee)
    {
        IReadOnlyList<Shift> shifts = ShiftsOf(employee);
        TimeSpan? shortest = null;
        for (int i = 1; i < shifts.Count; i++)
        {
            TimeSpan rest = shifts[i - 1].RestBetween(shifts[i]);
            if (shortest is null || rest < shortest)
            {
                shortest = rest;
            }
        }

        return shortest;
    }

    /// <summary>Freezes the roster: needs at least one shift, and every shift staffed.</summary>
    public void Publish()
    {
        RequireDraft();
        if (_shifts.Count == 0)
        {
            throw new RosterRefusedException(RefusalReason.NothingToPublish, "The roster has no shift.");
        }

        Shift? unstaffed = _shifts.Values.FirstOrDefault(s => !s.IsStaffed);
        if (unstaffed is not null)
        {
            throw new RosterRefusedException(RefusalReason.UnstaffedShift, $"Shift #{unstaffed.Number} ({unstaffed.Request}) has nobody assigned.");
        }

        Status = RosterStatus.Published;
    }

    /// <summary>Books a meeting; it must fall inside every participant's home-site working hours. Allowed after publication.</summary>
    public void ScheduleMeeting(Meeting meeting)
    {
        ArgumentNullException.ThrowIfNull(meeting);
        foreach (EmployeeId participant in meeting.Participants)
        {
            Employee employee = RequireEmployee(participant);
            Site site = employee.HomeSite;
            if (!site.IsWithinWorkingHours(meeting.StartUtc, meeting.EndUtc))
            {
                DateTimeOffset localStart = site.Zone.ToLocal(meeting.StartUtc);
                throw new RosterRefusedException(
                    RefusalReason.OutsideWorkingHours,
                    $"'{meeting.Title}' starts at {localStart:yyyy-MM-dd HH:mm zzz} for {participant} in {site.Zone}, outside {site.Name}'s working hours.");
            }
        }

        _meetings.Add(meeting);
    }

    /// <summary>
    /// The start instants on the UTC day, every 15 minutes, at which a meeting of that duration falls inside
    /// every participant's home-site working hours. Empty when the sites' hours never overlap that day.
    /// </summary>
    public IReadOnlyList<DateTimeOffset> MeetingSlots(DateOnly utcDate, TimeSpan duration, IEnumerable<EmployeeId> participants)
    {
        ArgumentNullException.ThrowIfNull(participants);
        if (duration < Meeting.MinDuration || duration > Meeting.MaxDuration)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "A meeting lasts between 15 minutes and 4 hours.");
        }

        List<Site> sites = participants.Select(p => RequireEmployee(p).HomeSite).Distinct().ToList();
        if (sites.Count == 0)
        {
            throw new ArgumentException("At least one participant is required.", nameof(participants));
        }

        DateTimeOffset dayStart = new(utcDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        List<DateTimeOffset> slots = new();
        for (DateTimeOffset start = dayStart; start < dayStart.AddDays(1); start += MeetingSlotStep)
        {
            DateTimeOffset end = start + duration;
            if (sites.All(s => s.IsWithinWorkingHours(start, end)))
            {
                slots.Add(start);
            }
        }

        return slots;
    }

    private (RefusalReason Reason, string Message) Evaluate(int shiftNumber, EmployeeId employeeId)
    {
        if (Status == RosterStatus.Published)
        {
            return (RefusalReason.RosterPublished, "the roster is published");
        }

        if (!_shifts.TryGetValue(shiftNumber, out Shift? shift))
        {
            return (RefusalReason.UnknownShift, "no such shift");
        }

        if (!_employees.TryGetValue(employeeId, out Employee? employee))
        {
            return (RefusalReason.UnknownEmployee, "employee is not registered");
        }

        if (shift.IsStaffed)
        {
            return (RefusalReason.ShiftAlreadyAssigned, $"already assigned to {shift.AssignedTo}");
        }

        IReadOnlyList<Shift> others = ShiftsOf(employeeId);
        Shift? overlapping = others.FirstOrDefault(o => o.Overlaps(shift));
        if (overlapping is not null)
        {
            return (RefusalReason.OverlapsAnotherShift, $"it overlaps shift #{overlapping.Number}");
        }

        Shift? tooClose = others.FirstOrDefault(o => o.RestBetween(shift) < MinimumRest);
        if (tooClose is not null)
        {
            return (RefusalReason.InsufficientRest, $"only {tooClose.RestBetween(shift):h\\:mm} elapsed rest next to shift #{tooClose.Number}");
        }

        IsoWeek week = WeekOf(shift, employee);
        TimeSpan worked = others.Where(o => WeekOf(o, employee).Equals(week)).Aggregate(TimeSpan.Zero, (sum, o) => sum + o.ElapsedDuration);
        if (worked + shift.ElapsedDuration > WeeklyLimit)
        {
            return (RefusalReason.WeeklyLimitExceeded, $"{(worked + shift.ElapsedDuration).TotalHours:0.##} h elapsed in {week} over {WeeklyLimit.TotalHours} h");
        }

        return (RefusalReason.None, string.Empty);
    }

    private static IsoWeek WeekOf(Shift shift, Employee employee) =>
        IsoWeek.Of(DateOnly.FromDateTime(employee.HomeSite.Zone.ToLocal(shift.StartUtc).DateTime));

    private static void RefuseUnresolvable(SiteZone zone, DateOnly date, TimeOnly time, OffsetChoice choice, RefusalReason skipped, RefusalReason ambiguous)
    {
        LocalTimeKind kind = zone.Classify(date, time);
        if (kind == LocalTimeKind.Skipped)
        {
            throw new RosterRefusedException(skipped, $"{date:yyyy-MM-dd} {time:HH\\:mm} does not exist in {zone}: the clocks spring forward over it.");
        }

        if (kind == LocalTimeKind.Ambiguous && choice == OffsetChoice.Unspecified)
        {
            throw new RosterRefusedException(ambiguous, $"{date:yyyy-MM-dd} {time:HH\\:mm} occurs twice in {zone}: choose the earlier or later offset.");
        }
    }

    private void RequireDraft()
    {
        if (Status == RosterStatus.Published)
        {
            throw new RosterRefusedException(RefusalReason.RosterPublished, "The roster is published.");
        }
    }

    private Shift RequireShift(int number) =>
        _shifts.TryGetValue(number, out Shift? shift)
            ? shift
            : throw new RosterRefusedException(RefusalReason.UnknownShift, $"There is no shift #{number}.");

    private Employee RequireEmployee(EmployeeId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _employees.TryGetValue(id, out Employee? employee)
            ? employee
            : throw new RosterRefusedException(RefusalReason.UnknownEmployee, $"Employee {id} is not registered.");
    }
}

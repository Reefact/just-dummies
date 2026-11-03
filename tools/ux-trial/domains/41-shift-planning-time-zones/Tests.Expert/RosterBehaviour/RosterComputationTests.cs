using ShiftPlanning.Domain.Tests.Expert.Support;

namespace ShiftPlanning.Domain.Tests.Expert.RosterBehaviour;

public sealed class RosterComputationTests {

    // ---- ShiftsOf ----------------------------------------------------------------------------------------------

    [Fact]
    public void An_employees_shifts_come_by_start_whatever_the_order_they_were_given_in() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        Shift thursday = scene.Staff(employee, site, "2027-01-07", "08:00", "16:00");
        Shift tuesday = scene.Staff(employee, site, "2027-01-05", "08:00", "16:00");
        Shift wednesday = scene.Staff(employee, site, "2027-01-06", "08:00", "16:00");

        // Act
        IReadOnlyList<Shift> shifts = scene.Roster.ShiftsOf(employee.Id);

        // Assert
        Assert.Equal([tuesday, wednesday, thursday], shifts);
    }

    [Fact]
    public void Shifts_on_different_sites_are_ordered_on_the_one_timeline_not_by_the_wall_clock() {
        // Arrange
        Scene scene = new Scene();
        Site tokyo = scene.OpenSite("Asia/Tokyo");
        Site newYork = scene.OpenSite("America/New_York");
        Employee employee = scene.Hire(tokyo);
        Shift inNewYork = scene.Staff(employee, newYork, "2027-01-13", "07:00", "10:00");   // 12:00Z to 15:00Z; 07:00 on its wall clock
        Shift inTokyo = scene.Staff(employee, tokyo, "2027-01-13", "08:00", "10:00");       // 2027-01-12 23:00Z to 01:00Z; 08:00 on its wall clock, later than New York's

        // Act
        IReadOnlyList<Shift> shifts = scene.Roster.ShiftsOf(employee.Id);

        // Assert
        Assert.Equal([inTokyo, inNewYork], shifts);
    }

    [Fact]
    public void Only_the_shifts_an_employee_holds_are_listed() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee holder = scene.Hire(site);
        Employee other = scene.Hire(site);
        Shift mine = scene.PlanAnyShiftAt(site);
        scene.PlanAnyShiftAt(site);   // unstaffed
        scene.Roster.Assign(mine.Number, holder.Id);

        // Assert
        Assert.Same(mine, Assert.Single(scene.Roster.ShiftsOf(holder.Id)));
        Assert.Empty(scene.Roster.ShiftsOf(other.Id));
    }

    // ---- ShortestRest ------------------------------------------------------------------------------------------

    [Fact]
    public void Below_two_shifts_there_is_no_rest_to_measure() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite();
        Employee withNone = scene.Hire(site);
        Employee withOne = scene.Hire(site);
        Shift only = scene.PlanAnyShiftAt(site);
        scene.Roster.Assign(only.Number, withOne.Id);

        // Assert
        Assert.Null(scene.Roster.ShortestRest(withNone.Id));
        Assert.Null(scene.Roster.ShortestRest(withOne.Id));
    }

    [Fact]
    public void The_shortest_rest_is_the_smallest_gap_between_consecutive_shifts() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-05", "08:00", "16:00");
        scene.Staff(employee, site, "2027-01-06", "08:00", "16:00");        // 16 h after the first
        scene.Staff(employee, site, "2027-01-07", "03:00", "11:00");        // 11 h after the second
        scene.Staff(employee, site, "2027-01-08", "08:00", "16:00");        // 21 h after the third

        // Act
        TimeSpan? shortest = scene.Roster.ShortestRest(employee.Id);

        // Assert
        Assert.Equal(TimeSpan.FromHours(11), shortest);
    }

    [Fact]
    public void The_shortest_rest_is_measured_in_elapsed_time() {
        // Arrange
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 12), 1));
        Site paris = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-03-27", "10:00", "18:00");        // ends 17:00Z
        scene.Staff(employee, paris, "2027-03-28", "06:00", "14:00");        // starts 04:00Z: eleven hours later, though the clock says twelve

        // Act
        TimeSpan? shortest = scene.Roster.ShortestRest(employee.Id);

        // Assert
        Assert.Equal(TimeSpan.FromHours(11), shortest);
    }

    // ---- ElapsedTimeIn -----------------------------------------------------------------------------------------

    [Fact]
    public void Elapsed_time_in_a_week_adds_up_the_elapsed_lengths_of_the_shifts_starting_in_it() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-04", "08:00", "16:00");     // 8 h
        scene.Staff(employee, site, "2027-01-06", "08:00", "12:30");     // 4 h 30
        scene.Staff(employee, site, "2027-01-11", "08:00", "20:00");     // the week after

        // Act
        TimeSpan firstWeek = scene.Roster.ElapsedTimeIn(employee.Id, new IsoWeek(2027, 1));
        TimeSpan secondWeek = scene.Roster.ElapsedTimeIn(employee.Id, new IsoWeek(2027, 2));
        TimeSpan thirdWeek = scene.Roster.ElapsedTimeIn(employee.Id, new IsoWeek(2027, 3));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(12 * 60 + 30), firstWeek);
        Assert.Equal(TimeSpan.FromHours(12), secondWeek);
        Assert.Equal(TimeSpan.Zero, thirdWeek);
    }

    [Fact]
    public void A_shift_counts_wholly_toward_the_week_it_starts_in() {
        // Arrange
        Scene scene = new Scene();
        Site site = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(site);
        scene.Staff(employee, site, "2027-01-10", "22:00", "06:00");    // Sunday night into Monday

        // Act
        TimeSpan startingWeek = scene.Roster.ElapsedTimeIn(employee.Id, new IsoWeek(2027, 1));
        TimeSpan followingWeek = scene.Roster.ElapsedTimeIn(employee.Id, new IsoWeek(2027, 2));

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), startingWeek);
        Assert.Equal(TimeSpan.Zero, followingWeek);
    }

    [Fact]
    public void A_transition_night_adds_its_elapsed_length_to_the_week() {
        // Arrange
        Scene scene = new Scene(new PlanningPeriod(new IsoWeek(2027, 12), 1));
        Site paris = scene.OpenSite("Europe/Paris");
        Employee employee = scene.Hire(paris);
        scene.Staff(employee, paris, "2027-03-27", "22:00", "06:00");   // spring forward

        // Act
        TimeSpan elapsed = scene.Roster.ElapsedTimeIn(employee.Id, new IsoWeek(2027, 12));

        // Assert
        Assert.Equal(TimeSpan.FromHours(7), elapsed);
    }

    [Fact]
    public void The_week_is_read_on_the_employees_home_site_clock_whatever_the_site_of_the_shift() {
        // Arrange
        Scene scene = new Scene();
        Site paris = scene.OpenSite("Europe/Paris");
        Site tokyo = scene.OpenSite("Asia/Tokyo");
        Employee parisian = scene.Hire(paris);
        Employee tokyoite = scene.Hire(tokyo);
        // Sunday 2027-01-10 22:00 in Paris is 21:00Z, which is Monday 2027-01-11 06:00 in Tokyo.
        Shift sundayNight = scene.Plan(paris, "2027-01-10", "22:00", "06:00");
        scene.Roster.Assign(sundayNight.Number, parisian.Id);
        Shift sameNightForTheOther = scene.Plan(paris, "2027-01-10", "22:00", "06:00");
        scene.Roster.Assign(sameNightForTheOther.Number, tokyoite.Id);

        // Act
        TimeSpan parisianInWeek1 = scene.Roster.ElapsedTimeIn(parisian.Id, new IsoWeek(2027, 1));
        TimeSpan tokyoiteInWeek1 = scene.Roster.ElapsedTimeIn(tokyoite.Id, new IsoWeek(2027, 1));
        TimeSpan tokyoiteInWeek2 = scene.Roster.ElapsedTimeIn(tokyoite.Id, new IsoWeek(2027, 2));

        // Assert
        Assert.Equal(TimeSpan.FromHours(8), parisianInWeek1);
        Assert.Equal(TimeSpan.Zero, tokyoiteInWeek1);
        Assert.Equal(TimeSpan.FromHours(8), tokyoiteInWeek2);
    }

    [Fact]
    public void The_weekly_limit_follows_the_home_site_week() {
        // Arrange
        Scene scene = new Scene();
        Site paris = scene.OpenSite("Europe/Paris");
        Site tokyo = scene.OpenSite("Asia/Tokyo");
        Employee parisian = scene.Hire(paris);
        Employee tokyoite = scene.Hire(tokyo);
        foreach (Employee employee in new[] { parisian, tokyoite }) {
            scene.Staff(employee, paris, "2027-01-04", "08:00", "20:00");   // 48 hours in week 1 on either clock
            scene.Staff(employee, paris, "2027-01-05", "08:00", "20:00");
            scene.Staff(employee, paris, "2027-01-06", "08:00", "20:00");
            scene.Staff(employee, paris, "2027-01-07", "08:00", "20:00");
        }
        Shift sundayNight = scene.Plan(paris, "2027-01-10", "22:00", "06:00");   // week 1 in Paris, week 2 in Tokyo

        // Act
        RefusalReason forParisian = scene.Roster.Check(sundayNight.Number, parisian.Id);
        RefusalReason forTokyoite = scene.Roster.Check(sundayNight.Number, tokyoite.Id);

        // Assert
        Assert.Equal(RefusalReason.WeeklyLimitExceeded, forParisian);
        Assert.Equal(RefusalReason.None, forTokyoite);
    }

    [Fact]
    public void Elapsed_time_for_an_unregistered_employee_is_refused() {
        // Arrange
        Scene scene = new Scene();
        EmployeeId stranger = Any.EmployeeId().Generate();

        // Act
        Action elapsed = () => scene.Roster.ElapsedTimeIn(stranger, new IsoWeek(2027, 1));

        // Assert
        Assert.Equal(RefusalReason.UnknownEmployee, Refusal.Of(elapsed));
    }

    [Fact]
    public void Elapsed_time_needs_an_employee_and_a_week() {
        // Arrange
        Scene scene = new Scene();
        EmployeeId anyEmployee = Any.EmployeeId().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>("employee", () => scene.Roster.ElapsedTimeIn(null!, new IsoWeek(2027, 1)));
        Assert.Throws<ArgumentNullException>("week", () => scene.Roster.ElapsedTimeIn(anyEmployee, null!));
    }

}

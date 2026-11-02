namespace ShiftPlanning.Domain;

/// <summary>
/// A member of staff. The home site's zone decides which ISO week a shift counts toward and
/// which working hours apply to the employee in a meeting.
/// </summary>
public sealed class Employee
{
    public Employee(EmployeeId id, string fullName, Site homeSite)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(homeSite);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        if (fullName.Length > 120)
        {
            throw new ArgumentException("A full name has at most 120 characters.", nameof(fullName));
        }

        if (fullName.Trim() != fullName)
        {
            throw new ArgumentException("A full name has no leading or trailing spaces.", nameof(fullName));
        }

        Id = id;
        FullName = fullName;
        HomeSite = homeSite;
    }

    public EmployeeId Id { get; }

    public string FullName { get; }

    public Site HomeSite { get; }
}

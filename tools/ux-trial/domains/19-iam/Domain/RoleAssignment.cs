namespace Identity.Domain;

/// <summary>A role granted to a subject over a date range; an open-ended grant has no end date.</summary>
public sealed class RoleAssignment {

    public RoleAssignment(Guid subjectId, Role role, DateOnly validFrom, DateOnly? validUntil) {
        if (subjectId == Guid.Empty) {
            throw new ArgumentException("A role is granted to a known subject.", nameof(subjectId));
        }

        if (!Enum.IsDefined(role)) {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        if (validUntil is DateOnly end && end < validFrom) {
            throw new ArgumentException("A grant cannot end before it starts.", nameof(validUntil));
        }

        SubjectId  = subjectId;
        Role       = role;
        ValidFrom  = validFrom;
        ValidUntil = validUntil;
    }

    public Guid SubjectId { get; }

    public Role Role { get; }

    public DateOnly ValidFrom { get; }

    public DateOnly? ValidUntil { get; }

    public bool IsActiveOn(DateOnly day) => day >= ValidFrom && (ValidUntil is null || day <= ValidUntil.Value);

}

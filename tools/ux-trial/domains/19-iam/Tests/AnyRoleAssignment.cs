// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RoleAssignment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RoleAssignment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoleAssignment : IAny<RoleAssignment> {

    private readonly IAny<Guid>      _subjectId;
    private readonly IAny<Role>      _role;
    private readonly IAny<DateOnly>  _validFrom;
    private readonly IAny<DateOnly?> _validUntil;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoleAssignment()
        : this(subjectId:  AnyValidSubjectId(),
               role:       AnyValidRole(),
               validFrom:  AnyValidValidFrom(),
               validUntil: AnyValidValidUntil().AsNullable()) { }

    private static AnyGuid AnyValidSubjectId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyEnum<Role> AnyValidRole() {
        return Any.Enum<Role>();
    }

    // validUntil may not precede validFrom: two adjacent windows keep every pair ordered.
    private static AnyDateOnly AnyValidValidFrom() {
        return Any.DateOnly().Between(new DateOnly(2020, 1, 1), new DateOnly(2025, 12, 31));
    }

    private static AnyDateOnly AnyValidValidUntil() {
        return Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2030, 12, 31));
    }

    private AnyRoleAssignment(IAny<Guid>      subjectId,
                              IAny<Role>      role,
                              IAny<DateOnly>  validFrom,
                              IAny<DateOnly?> validUntil) {
        _subjectId  = subjectId;
        _role       = role;
        _validFrom  = validFrom;
        _validUntil = validUntil;
    }

    /// <summary>Pins <c>subjectId</c> to a fixed value.</summary>
    public AnyRoleAssignment WithSubjectId(Guid value) {
        return new AnyRoleAssignment(new FixedValue<Guid>(value), _role, _validFrom, _validUntil);
    }

    /// <summary>Draws <c>subjectId</c> from <c>AnyValidSubjectId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoleAssignment WithSubjectId(Func<AnyGuid, AnyGuid> configure) {
        return new AnyRoleAssignment(configure(AnyValidSubjectId()), _role, _validFrom, _validUntil);
    }

    /// <summary>Pins <c>role</c> to a fixed value.</summary>
    public AnyRoleAssignment WithRole(Role value) {
        return new AnyRoleAssignment(_subjectId, new FixedValue<Role>(value), _validFrom, _validUntil);
    }

    /// <summary>Draws <c>role</c> from <c>AnyValidRole()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoleAssignment WithRole(Func<AnyEnum<Role>, AnyEnum<Role>> configure) {
        return new AnyRoleAssignment(_subjectId, configure(AnyValidRole()), _validFrom, _validUntil);
    }

    /// <summary>Pins <c>validFrom</c> to a fixed value.</summary>
    public AnyRoleAssignment WithValidFrom(DateOnly value) {
        return new AnyRoleAssignment(_subjectId, _role, new FixedValue<DateOnly>(value), _validUntil);
    }

    /// <summary>Draws <c>validFrom</c> from <c>AnyValidValidFrom()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoleAssignment WithValidFrom(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyRoleAssignment(_subjectId, _role, configure(AnyValidValidFrom()), _validUntil);
    }

    /// <summary>Pins <c>validUntil</c> to a fixed value.</summary>
    public AnyRoleAssignment WithValidUntil(DateOnly? value) {
        return new AnyRoleAssignment(_subjectId, _role, _validFrom, new FixedValue<DateOnly?>(value));
    }

    /// <summary>Draws <c>validUntil</c> from <c>AnyValidValidUntil()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoleAssignment WithValidUntil(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyRoleAssignment(_subjectId, _role, _validFrom, configure(AnyValidValidUntil()).AsNullable());
    }

    /// <summary>Produces one arbitrary <see cref="RoleAssignment" />.</summary>
    public RoleAssignment Generate() {
        return new RoleAssignment(_subjectId.Generate(),
                                  _role.Generate(),
                                  _validFrom.Generate(),
                                  _validUntil.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}

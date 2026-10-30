// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RoleSet --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RoleSet" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoleSet : IAny<RoleSet> {

    private readonly IAny<IReadOnlyCollection<Role>> _roles;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoleSet()
        : this(roles: AnyValidRoles()) { }

    private static AnyList<Role> AnyValidRoles() {
        return Any.ListOf(Any.Enum<Role>());
    }

    private AnyRoleSet(IAny<IReadOnlyCollection<Role>> roles) {
        _roles = roles;
    }

    /// <summary>Pins <c>roles</c> to a fixed value.</summary>
    public AnyRoleSet WithRoles(IReadOnlyCollection<Role> value) {
        return new AnyRoleSet(new FixedValue<IReadOnlyCollection<Role>>(value));
    }

    /// <summary>Draws <c>roles</c> from <c>AnyValidRoles()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoleSet WithRoles(Func<AnyList<Role>, AnyList<Role>> configure) {
        return new AnyRoleSet(configure(AnyValidRoles()));
    }

    /// <summary>Produces one arbitrary <see cref="RoleSet" />.</summary>
    public RoleSet Generate() {
        return new RoleSet(_roles.Generate());
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

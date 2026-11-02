// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RoleAssignment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using Identity.Domain;
using JustDummies;

namespace Identity.Domain.Tests.Expert.Generators;

/// <summary>
///     A generator of arbitrary <see cref="RoleAssignment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoleAssignment : IAny<RoleAssignment> {

    private IAny<Guid>      _subjectId;
    private IAny<Role>      _role;
    private IAny<DateOnly>  _validFrom;
    private IAny<int?>      _lastingDays;
    private bool            _validUntilPinned;
    private DateOnly?       _validUntilPin;

    private AnyGuid       _subjectIdRecipe;
    private AnyEnum<Role> _roleRecipe;
    private AnyDateOnly   _validFromRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoleAssignment()
        : this(subjectId:  AnyValidSubjectId(),
               role:       AnyValidRole(),
               validFrom:  AnyValidValidFrom(),
               lastingDays: AnyValidLastingDays()) { }

    private static AnyGuid AnyValidSubjectId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyEnum<Role> AnyValidRole() {
        return Any.Enum<Role>();
    }

    private static AnyDateOnly AnyValidValidFrom() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2040, 12, 31));
    }

    // validUntil is not drawn on its own: the constructor ties it to validFrom, so it is drawn as a
    // number of days a grant lasts (a one-day grant is 0), or as no end at all.
    private static AnyInt32 AnyValidLastingDays() {
        return Any.Int32().Between(0, 3650);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRoleAssignment(AnyGuid       subjectId,
                              AnyEnum<Role> role,
                              AnyDateOnly   validFrom,
                              AnyInt32      lastingDays) {
        _subjectId        = subjectId;
        _subjectIdRecipe  = subjectId;
        _role             = role;
        _roleRecipe       = role;
        _validFrom        = validFrom;
        _validFromRecipe  = validFrom;
        _lastingDays      = lastingDays.OrNull();
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRoleAssignment(AnyRoleAssignment from) {
        _subjectId        = from._subjectId;
        _subjectIdRecipe  = from._subjectIdRecipe;
        _role             = from._role;
        _roleRecipe       = from._roleRecipe;
        _validFrom        = from._validFrom;
        _validFromRecipe  = from._validFromRecipe;
        _lastingDays      = from._lastingDays;
        _validUntilPinned = from._validUntilPinned;
        _validUntilPin    = from._validUntilPin;
    }

    /// <summary>Pins <c>subjectId</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoleAssignment WithSubjectId(Guid value) {
        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._subjectId = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>subjectId</c> is drawn from — <c>AnyValidSubjectId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyGuid, AnyGuid&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoleAssignment WithSubjectId(Func<AnyGuid, AnyGuid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._subjectIdRecipe = configure(_subjectIdRecipe);
        copy._subjectId       = copy._subjectIdRecipe;

        return copy;
    }

    /// <summary>Pins <c>role</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoleAssignment WithRole(Role value) {
        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._role = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>role</c> is drawn from — <c>AnyValidRole()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEnum&lt;Role&gt;, AnyEnum&lt;Role&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoleAssignment WithRole(Func<AnyEnum<Role>, AnyEnum<Role>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._roleRecipe = configure(_roleRecipe);
        copy._role       = copy._roleRecipe;

        return copy;
    }

    /// <summary>Pins <c>validFrom</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoleAssignment WithValidFrom(DateOnly value) {
        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._validFrom = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>validFrom</c> is drawn from — <c>AnyValidValidFrom()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoleAssignment WithValidFrom(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._validFromRecipe = configure(_validFromRecipe);
        copy._validFrom       = copy._validFromRecipe;

        return copy;
    }

    /// <summary>Pins <c>validUntil</c> to <paramref name="value" />. The constructor ties it to <c>validFrom</c>: pin <c>validFrom</c> too, or the order depends on the seed.</summary>
    public AnyRoleAssignment WithValidUntil(DateOnly? value) {
        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._validUntilPinned = true;
        copy._validUntilPin    = value;

        return copy;
    }

    /// <summary>A grant that ends <paramref name="days" /> days after it starts (0 is a one-day grant).</summary>
    public AnyRoleAssignment LastingDays(int days) {
        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._validUntilPinned = false;
        copy._lastingDays      = Any.Fixed<int?>(days);

        return copy;
    }

    /// <summary>A grant with no end date.</summary>
    public AnyRoleAssignment WithoutValidUntil() {
        return WithValidUntil(null);
    }

    /// <summary>Produces one arbitrary <see cref="RoleAssignment" />.</summary>
    public RoleAssignment Generate() {
        DateOnly  validFrom  = _validFrom.Generate();
        int?      days       = _lastingDays.Generate();
        DateOnly? validUntil = _validUntilPinned ? _validUntilPin : days is { } d ? validFrom.AddDays(d) : null;

        return new RoleAssignment(_subjectId.Generate(),
                                  _role.Generate(),
                                  validFrom,
                                  validUntil);
    }

}

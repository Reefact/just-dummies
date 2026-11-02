// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate RoleAssignment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Identity.Domain;

/// <summary>
///     A generator of arbitrary <see cref="RoleAssignment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoleAssignment : IAny<RoleAssignment> {

    private IAny<Guid>      _subjectId;
    private IAny<Role>      _role;
    private IAny<DateOnly>  _validFrom;
    private IAny<DateOnly?> _validUntil;

    private AnyGuid       _subjectIdRecipe;
    private AnyEnum<Role> _roleRecipe;
    private AnyDateOnly   _validFromRecipe;
    private AnyDateOnly   _validUntilRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoleAssignment()
        : this(subjectId:  AnyValidSubjectId(),
               role:       AnyValidRole(),
               validFrom:  AnyValidValidFrom(),
               validUntil: AnyValidValidUntil()) { }

    private static AnyGuid AnyValidSubjectId() {
        return Any.Guid().NonEmpty();
    }

    private static AnyEnum<Role> AnyValidRole() {
        return Any.Enum<Role>();
    }

    private static AnyDateOnly AnyValidValidFrom() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 1, 1));
    }

    private static AnyDateOnly AnyValidValidUntil() {
        return Any.DateOnly();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyRoleAssignment(AnyGuid       subjectId,
                              AnyEnum<Role> role,
                              AnyDateOnly   validFrom,
                              AnyDateOnly   validUntil) {
        _subjectId        = subjectId;
        _subjectIdRecipe  = subjectId;
        _role             = role;
        _roleRecipe       = role;
        _validFrom        = validFrom;
        _validFromRecipe  = validFrom;
        _validUntil       = validUntil.OrNull();
        _validUntilRecipe = validUntil;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyRoleAssignment(AnyRoleAssignment from) {
        _subjectId        = from._subjectId;
        _subjectIdRecipe  = from._subjectIdRecipe;
        _role             = from._role;
        _roleRecipe       = from._roleRecipe;
        _validFrom        = from._validFrom;
        _validFromRecipe  = from._validFromRecipe;
        _validUntil       = from._validUntil;
        _validUntilRecipe = from._validUntilRecipe;
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

    /// <summary>Pins <c>validUntil</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyRoleAssignment WithValidUntil(DateOnly? value) {
        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._validUntil = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>validUntil</c> is drawn from — <c>AnyValidValidUntil()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyRoleAssignment WithValidUntil(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._validUntilRecipe = configure(_validUntilRecipe);
        copy._validUntil       = copy._validUntilRecipe.AsNullable();

        return copy;
    }

    /// <summary>Pins <c>validUntil</c> to <c>null</c>; its recipe is kept for a later refinement.</summary>
    public AnyRoleAssignment WithoutValidUntil() {
        AnyRoleAssignment copy = new AnyRoleAssignment(this);

        copy._validUntil = Any.Fixed<DateOnly?>(null);

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="RoleAssignment" />.</summary>
    public RoleAssignment Generate() {
        DateOnly  validFrom  = _validFrom.Generate();
        DateOnly? validUntil = _validUntilRecipe.AfterOrEqualTo(validFrom).OrNull().Generate();

        return new RoleAssignment(_subjectId.Generate(),
                                  _role.Generate(),
                                  validFrom,
                                  validUntil);
    }

}

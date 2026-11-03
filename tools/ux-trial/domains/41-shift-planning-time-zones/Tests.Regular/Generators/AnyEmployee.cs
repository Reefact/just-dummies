// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Employee --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace ShiftPlanning.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Employee" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEmployee : IAny<Employee> {

    private IAny<EmployeeId> _id;
    private IAny<string>     _fullName;
    private IAny<Site>       _homeSite;

    private AnyEmployeeId _idRecipe;
    private AnyString     _fullNameRecipe;
    private AnySite       _homeSiteRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEmployee()
        : this(id:       AnyValidId(),
               fullName: AnyValidFullName(),
               homeSite: AnyValidHomeSite()) { }

    private static AnyEmployeeId AnyValidId() {
        return new AnyEmployeeId();
    }

    private static AnyString AnyValidFullName() {
        return Any.String().Alpha().WithLengthBetween(1, 120);
    }

    private static AnySite AnyValidHomeSite() {
        return new AnySite();
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEmployee(AnyEmployeeId id,
                        AnyString     fullName,
                        AnySite       homeSite) {
        _id             = id;
        _idRecipe       = id;
        _fullName       = fullName;
        _fullNameRecipe = fullName;
        _homeSite       = homeSite;
        _homeSiteRecipe = homeSite;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEmployee(AnyEmployee from) {
        _id             = from._id;
        _idRecipe       = from._idRecipe;
        _fullName       = from._fullName;
        _fullNameRecipe = from._fullNameRecipe;
        _homeSite       = from._homeSite;
        _homeSiteRecipe = from._homeSiteRecipe;
    }

    /// <summary>Pins <c>id</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmployee WithId(EmployeeId value) {
        AnyEmployee copy = new AnyEmployee(this);

        copy._id = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>id</c> is drawn from — <c>AnyValidId()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyEmployeeId, AnyEmployeeId&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmployee WithId(Func<AnyEmployeeId, AnyEmployeeId> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmployee copy = new AnyEmployee(this);

        copy._idRecipe = configure(_idRecipe);
        copy._id       = copy._idRecipe;

        return copy;
    }

    /// <summary>Pins <c>fullName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmployee WithFullName(string value) {
        AnyEmployee copy = new AnyEmployee(this);

        copy._fullName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fullName</c> is drawn from — <c>AnyValidFullName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmployee WithFullName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmployee copy = new AnyEmployee(this);

        copy._fullNameRecipe = configure(_fullNameRecipe);
        copy._fullName       = copy._fullNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>homeSite</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEmployee WithHomeSite(Site value) {
        AnyEmployee copy = new AnyEmployee(this);

        copy._homeSite = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>homeSite</c> is drawn from — <c>AnyValidHomeSite()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySite, AnySite&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEmployee WithHomeSite(Func<AnySite, AnySite> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEmployee copy = new AnyEmployee(this);

        copy._homeSiteRecipe = configure(_homeSiteRecipe);
        copy._homeSite       = copy._homeSiteRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Employee" />.</summary>
    public Employee Generate() {
        return new Employee(_id.Generate(),
                            _fullName.Generate(),
                            _homeSite.Generate());
    }

}

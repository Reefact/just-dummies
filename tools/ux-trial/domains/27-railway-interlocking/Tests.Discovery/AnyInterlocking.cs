// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Interlocking --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Interlocking" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyInterlocking : IAny<Interlocking> {

    private IAny<string>                    _areaName;
    private IAny<IEnumerable<TrackSection>> _sections;
    private IAny<IEnumerable<Point>>        _points;
    private IAny<IEnumerable<Route>>        _routes;

    private AnyString                 _areaNameRecipe;
    private AnySequence<TrackSection> _sectionsRecipe;
    private AnySequence<Point>        _pointsRecipe;
    private AnySequence<Route>        _routesRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyInterlocking()
        : this(areaName: AnyValidAreaName(),
               sections: AnyValidSections(),
               points:   AnyValidPoints(),
               routes:   AnyValidRoutes()) { }

    private static AnyString AnyValidAreaName() {
        return Any.String().NotBlank();
    }

    private static AnySequence<TrackSection> AnyValidSections() {
        return Any.SequenceOf(new AnyTrackSection()).WithCountBetween(2, 8).DistinctBy(section => section.Id);
    }

    private static AnySequence<Point> AnyValidPoints() {
        return Any.SequenceOf(new AnyPoint()).WithCountBetween(0, 3).DistinctBy(point => point.Name);
    }

    private static AnySequence<Route> AnyValidRoutes() {
        return Any.SequenceOf(new AnyRoute()).WithCountBetween(1, 3).DistinctBy(route => route.Name);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyInterlocking(AnyString                 areaName,
                            AnySequence<TrackSection> sections,
                            AnySequence<Point>        points,
                            AnySequence<Route>        routes) {
        _areaName       = areaName;
        _areaNameRecipe = areaName;
        _sections       = sections;
        _sectionsRecipe = sections;
        _points         = points;
        _pointsRecipe   = points;
        _routes         = routes;
        _routesRecipe   = routes;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyInterlocking(AnyInterlocking from) {
        _areaName       = from._areaName;
        _areaNameRecipe = from._areaNameRecipe;
        _sections       = from._sections;
        _sectionsRecipe = from._sectionsRecipe;
        _points         = from._points;
        _pointsRecipe   = from._pointsRecipe;
        _routes         = from._routes;
        _routesRecipe   = from._routesRecipe;
    }

    /// <summary>Pins <c>areaName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInterlocking WithAreaName(string value) {
        AnyInterlocking copy = new AnyInterlocking(this);

        copy._areaName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>areaName</c> is drawn from — <c>AnyValidAreaName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInterlocking WithAreaName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInterlocking copy = new AnyInterlocking(this);

        copy._areaNameRecipe = configure(_areaNameRecipe);
        copy._areaName       = copy._areaNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>sections</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInterlocking WithSections(IEnumerable<TrackSection> value) {
        AnyInterlocking copy = new AnyInterlocking(this);

        copy._sections = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>sections</c> is drawn from — <c>AnyValidSections()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;TrackSection&gt;, AnySequence&lt;TrackSection&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInterlocking WithSections(Func<AnySequence<TrackSection>, AnySequence<TrackSection>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInterlocking copy = new AnyInterlocking(this);

        copy._sectionsRecipe = configure(_sectionsRecipe);
        copy._sections       = copy._sectionsRecipe;

        return copy;
    }

    /// <summary>Pins <c>points</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInterlocking WithPoints(IEnumerable<Point> value) {
        AnyInterlocking copy = new AnyInterlocking(this);

        copy._points = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>points</c> is drawn from — <c>AnyValidPoints()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;Point&gt;, AnySequence&lt;Point&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInterlocking WithPoints(Func<AnySequence<Point>, AnySequence<Point>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInterlocking copy = new AnyInterlocking(this);

        copy._pointsRecipe = configure(_pointsRecipe);
        copy._points       = copy._pointsRecipe;

        return copy;
    }

    /// <summary>Pins <c>routes</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyInterlocking WithRoutes(IEnumerable<Route> value) {
        AnyInterlocking copy = new AnyInterlocking(this);

        copy._routes = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>routes</c> is drawn from — <c>AnyValidRoutes()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySequence&lt;Route&gt;, AnySequence&lt;Route&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyInterlocking WithRoutes(Func<AnySequence<Route>, AnySequence<Route>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyInterlocking copy = new AnyInterlocking(this);

        copy._routesRecipe = configure(_routesRecipe);
        copy._routes       = copy._routesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Interlocking" />.</summary>
    public Interlocking Generate() {
        // The three collections refer to one another (a point lies in a declared section, a route traverses
        // declared sections and sets declared points lying in them), which no per-parameter recipe can say:
        // the sections are drawn first, then points and routes are re-anchored on them.
        List<TrackSection> sections = _sections.Generate().ToList();
        List<SectionId>    sectionIds = sections.Select(section => section.Id).ToList();

        List<Point> points = _points.Generate()
                                    .Select(point => new Point(point.Name, Any.ElementOf(sectionIds).Generate(), point.Position))
                                    .ToList();

        List<Route> routes = new List<Route>();
        foreach (Route drawn in _routes.Generate()) {
            List<SectionId> routeSections = Any.SetOf(Any.ElementOf(sectionIds)).WithCountBetween(1, sectionIds.Count).Generate().ToList();
            List<PointSetting> settings = points.Where(point => routeSections.Contains(point.Section))
                                                .Select(point => new PointSetting(point.Name, Any.Enum<PointPosition>().Generate()))
                                                .ToList();
            routes.Add(new Route(drawn.Name, drawn.EntrySignal, drawn.ExitSignal, routeSections, settings));
        }

        return new Interlocking(_areaName.Generate(), sections, points, routes);
    }

}

// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Route --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace RailwayInterlocking.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Route" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyRoute : IAny<Route> {

    private readonly IAny<string>                      _name;
    private readonly IAny<string>                      _entrySignal;
    private readonly IAny<string>                      _exitSignal;
    private readonly IAny<IReadOnlyList<SectionId>>    _sections;
    private readonly IAny<IReadOnlyList<PointSetting>> _pointSettings;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoute()
        : this(name:          AnyValidName(),
               entrySignal:   AnyValidEntrySignal(),
               exitSignal:    AnyValidExitSignal(),
               sections:      AnyValidSections(),
               pointSettings: AnyValidPointSettings()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidEntrySignal() {
        // Route.cs: entry and exit differ; that rule lives between two parameters, so Generate() enforces it.
        return Any.String().NotBlank();
    }

    private static AnyString AnyValidExitSignal() {
        // Route.cs: entry and exit differ; that rule lives between two parameters, so Generate() enforces it.
        return Any.String().NotBlank();
    }

    private static AnyList<SectionId> AnyValidSections() {
        // Route.cs: at least one section, none null, none twice.
        return Any.ListOf(new AnySectionId()).NonEmpty().Distinct();
    }

    private static AnyList<PointSetting> AnyValidPointSettings() {
        // Route.cs: no null setting, no point set twice.
        return Any.ListOf(new AnyPointSetting()).Distinct(PointNameComparer.Instance);
    }

    private AnyRoute(IAny<string>                      name,
                     IAny<string>                      entrySignal,
                     IAny<string>                      exitSignal,
                     IAny<IReadOnlyList<SectionId>>    sections,
                     IAny<IReadOnlyList<PointSetting>> pointSettings) {
        _name          = name;
        _entrySignal   = entrySignal;
        _exitSignal    = exitSignal;
        _sections      = sections;
        _pointSettings = pointSettings;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyRoute WithName(string value) {
        return new AnyRoute(new FixedValue<string>(value), _entrySignal, _exitSignal, _sections, _pointSettings);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithName(Func<AnyString, AnyString> configure) {
        return new AnyRoute(configure(AnyValidName()), _entrySignal, _exitSignal, _sections, _pointSettings);
    }

    /// <summary>Pins <c>entrySignal</c> to a fixed value.</summary>
    public AnyRoute WithEntrySignal(string value) {
        return new AnyRoute(_name, new FixedValue<string>(value), _exitSignal, _sections, _pointSettings);
    }

    /// <summary>Draws <c>entrySignal</c> from <c>AnyValidEntrySignal()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithEntrySignal(Func<AnyString, AnyString> configure) {
        return new AnyRoute(_name, configure(AnyValidEntrySignal()), _exitSignal, _sections, _pointSettings);
    }

    /// <summary>Pins <c>exitSignal</c> to a fixed value.</summary>
    public AnyRoute WithExitSignal(string value) {
        return new AnyRoute(_name, _entrySignal, new FixedValue<string>(value), _sections, _pointSettings);
    }

    /// <summary>Draws <c>exitSignal</c> from <c>AnyValidExitSignal()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithExitSignal(Func<AnyString, AnyString> configure) {
        return new AnyRoute(_name, _entrySignal, configure(AnyValidExitSignal()), _sections, _pointSettings);
    }

    /// <summary>Pins <c>sections</c> to a fixed value.</summary>
    public AnyRoute WithSections(IReadOnlyList<SectionId> value) {
        return new AnyRoute(_name, _entrySignal, _exitSignal, new FixedValue<IReadOnlyList<SectionId>>(value), _pointSettings);
    }

    /// <summary>Draws <c>sections</c> from <c>AnyValidSections()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithSections(Func<AnyList<SectionId>, AnyList<SectionId>> configure) {
        return new AnyRoute(_name, _entrySignal, _exitSignal, configure(AnyValidSections()), _pointSettings);
    }

    /// <summary>Pins <c>pointSettings</c> to a fixed value.</summary>
    public AnyRoute WithPointSettings(IReadOnlyList<PointSetting> value) {
        return new AnyRoute(_name, _entrySignal, _exitSignal, _sections, new FixedValue<IReadOnlyList<PointSetting>>(value));
    }

    /// <summary>Draws <c>pointSettings</c> from <c>AnyValidPointSettings()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithPointSettings(Func<AnyList<PointSetting>, AnyList<PointSetting>> configure) {
        return new AnyRoute(_name, _entrySignal, _exitSignal, _sections, configure(AnyValidPointSettings()));
    }

    /// <summary>Produces one arbitrary <see cref="Route" />.</summary>
    public Route Generate() {
        string entrySignal = _entrySignal.Generate();
        string exitSignal  = _exitSignal.Generate();
        // A route never starts and ends at the same signal: a bounded redraw, then the constructor refuses.
        for (int attempt = 0; attempt < 10 && string.Equals(entrySignal, exitSignal, StringComparison.Ordinal); attempt++) {
            exitSignal = _exitSignal.Generate();
        }

        return new Route(_name.Generate(),
                         entrySignal,
                         exitSignal,
                         _sections.Generate(),
                         _pointSettings.Generate());
    }

    private sealed class PointNameComparer : IEqualityComparer<PointSetting> {

        public static readonly PointNameComparer Instance = new PointNameComparer();

        public bool Equals(PointSetting? x, PointSetting? y) {
            return string.Equals(x?.PointName, y?.PointName, StringComparison.Ordinal);
        }

        public int GetHashCode(PointSetting obj) {
            return StringComparer.Ordinal.GetHashCode(obj.PointName);
        }

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

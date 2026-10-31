// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Route --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
//
// Edited by hand: the scaffolder drew entrySignal and exitSignal independently, while Route refuses
// a route that starts and ends at the same signal. That rule lives between two parameters, so the
// two signals are drawn together, as two distinct names, by construction.

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

    private readonly IAny<string>                         _name;
    private readonly IAny<(string Entry, string Exit)>    _signals;
    private readonly IAny<IReadOnlyList<SectionId>>       _sections;
    private readonly IAny<IReadOnlyList<PointSetting>>    _pointSettings;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyRoute()
        : this(name:          AnyValidName(),
               signals:       AnyValidSignals(),
               sections:      AnyValidSections(),
               pointSettings: AnyValidPointSettings()) { }

    private static AnyString AnyValidName() {
        return Any.String().NotBlank().WithMaxLength(16);
    }

    private static IAny<(string Entry, string Exit)> AnyValidSignals() {
        // A route runs from one signal to another: two distinct signal names, drawn together.
        return Any.ListOf(Any.StringMatching("[A-Z]{1,2}[0-9]{1,3}"))
                  .WithCount(2)
                  .Distinct()
                  .As(signals => (signals[0], signals[1]));
    }

    private static AnyList<SectionId> AnyValidSections() {
        // A route traverses at least one section and never the same one twice.
        return Any.ListOf(new AnySectionId()).NonEmpty().Distinct();
    }

    private static AnyList<PointSetting> AnyValidPointSettings() {
        // A route sets each point at most once: settings are distinct by point name.
        return Any.ListOf(new AnyPointSetting()).Distinct(PointSettingByPointName.Instance);
    }

    private AnyRoute(IAny<string>                      name,
                     IAny<(string Entry, string Exit)> signals,
                     IAny<IReadOnlyList<SectionId>>    sections,
                     IAny<IReadOnlyList<PointSetting>> pointSettings) {
        _name          = name;
        _signals       = signals;
        _sections      = sections;
        _pointSettings = pointSettings;
    }

    /// <summary>Pins <c>name</c> to a fixed value.</summary>
    public AnyRoute WithName(string value) {
        return new AnyRoute(new FixedValue<string>(value), _signals, _sections, _pointSettings);
    }

    /// <summary>Draws <c>name</c> from <c>AnyValidName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithName(Func<AnyString, AnyString> configure) {
        return new AnyRoute(configure(AnyValidName()), _signals, _sections, _pointSettings);
    }

    /// <summary>Pins both signals of the route.</summary>
    public AnyRoute WithSignals(string entrySignal, string exitSignal) {
        return new AnyRoute(_name, new FixedValue<(string, string)>((entrySignal, exitSignal)), _sections, _pointSettings);
    }

    /// <summary>Pins <c>sections</c> to a fixed value.</summary>
    public AnyRoute WithSections(IReadOnlyList<SectionId> value) {
        return new AnyRoute(_name, _signals, new FixedValue<IReadOnlyList<SectionId>>(value), _pointSettings);
    }

    /// <summary>Draws <c>sections</c> from <c>AnyValidSections()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithSections(Func<AnyList<SectionId>, AnyList<SectionId>> configure) {
        return new AnyRoute(_name, _signals, configure(AnyValidSections()), _pointSettings);
    }

    /// <summary>Pins <c>pointSettings</c> to a fixed value.</summary>
    public AnyRoute WithPointSettings(IReadOnlyList<PointSetting> value) {
        return new AnyRoute(_name, _signals, _sections, new FixedValue<IReadOnlyList<PointSetting>>(value));
    }

    /// <summary>Draws <c>pointSettings</c> from <c>AnyValidPointSettings()</c>, refined by <paramref name="configure" />.</summary>
    public AnyRoute WithPointSettings(Func<AnyList<PointSetting>, AnyList<PointSetting>> configure) {
        return new AnyRoute(_name, _signals, _sections, configure(AnyValidPointSettings()));
    }

    /// <summary>Produces one arbitrary <see cref="Route" />.</summary>
    public Route Generate() {
        (string entrySignal, string exitSignal) = _signals.Generate();

        return new Route(_name.Generate(),
                         entrySignal,
                         exitSignal,
                         _sections.Generate(),
                         _pointSettings.Generate());
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

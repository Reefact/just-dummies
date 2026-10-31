// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CatastropheModel --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CatastropheModel" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCatastropheModel : IAny<CatastropheModel> {

    private readonly IAny<IReadOnlyList<CatastropheEvent>> _catalogue;
    private readonly IAny<double>                          _severityVolatility;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCatastropheModel()
        : this(catalogue:          AnyValidCatalogue(),
               severityVolatility: AnyValidSeverityVolatility()) { }

    private static AnyList<CatastropheEvent> AnyValidCatalogue() {
        // A catalogue never holds the same event id twice.
        return Any.ListOf(new AnyCatastropheEvent())
                  .WithCountBetween(1, 5)
                  .Distinct(EqualityComparer<CatastropheEvent>.Create((left, right) => left?.Id == right?.Id,
                                                                     catastrophe => catastrophe.Id.GetHashCode()));
    }

    private static AnyDouble AnyValidSeverityVolatility() {
        return Any.Double().GreaterThanOrEqualTo(0.0).LessThanOrEqualTo(CatastropheModel.MaxSeverityVolatility);
    }

    private AnyCatastropheModel(IAny<IReadOnlyList<CatastropheEvent>> catalogue,
                                IAny<double>                          severityVolatility) {
        _catalogue          = catalogue;
        _severityVolatility = severityVolatility;
    }

    /// <summary>Pins <c>catalogue</c> to a fixed value.</summary>
    public AnyCatastropheModel WithCatalogue(IReadOnlyList<CatastropheEvent> value) {
        return new AnyCatastropheModel(new FixedValue<IReadOnlyList<CatastropheEvent>>(value), _severityVolatility);
    }

    /// <summary>Draws <c>catalogue</c> from <c>AnyValidCatalogue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCatastropheModel WithCatalogue(Func<AnyList<CatastropheEvent>, AnyList<CatastropheEvent>> configure) {
        return new AnyCatastropheModel(configure(AnyValidCatalogue()), _severityVolatility);
    }

    /// <summary>Pins <c>severityVolatility</c> to a fixed value.</summary>
    public AnyCatastropheModel WithSeverityVolatility(double value) {
        return new AnyCatastropheModel(_catalogue, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>severityVolatility</c> from <c>AnyValidSeverityVolatility()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCatastropheModel WithSeverityVolatility(Func<AnyDouble, AnyDouble> configure) {
        return new AnyCatastropheModel(_catalogue, configure(AnyValidSeverityVolatility()));
    }

    /// <summary>Produces one arbitrary <see cref="CatastropheModel" />.</summary>
    public CatastropheModel Generate() {
        return new CatastropheModel(_catalogue.Generate(),
                                    _severityVolatility.Generate());
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

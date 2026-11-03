// Scaffolded by `any` (JustDummies), then edited by hand. The scaffold marked `series` and `index` "to verify":
// the valid index range depends on the drawn series (E6 has 6 values per decade, E96 has 96), a tie between two
// parameters that `any` cannot express per parameter. The index recipe is therefore a function of the series.

using System;

using JustDummies;
using PassiveComponents.Domain;

namespace PassiveComponents.Domain.Tests.Expert.Generators;

public sealed partial class AnyPreferredValue : IAny<PreferredValue> {

    private IAny<ESeries>                _series;
    private IAny<int>                    _decade;
    private Func<ESeries, IAny<int>>     _index;

    private AnyEnum<ESeries>             _seriesRecipe;
    private AnyInt32                     _decadeRecipe;
    private Func<ESeries, AnyInt32>      _indexRecipe;

    public AnyPreferredValue() {
        _seriesRecipe = Any.Enum<ESeries>();
        _decadeRecipe = Any.Int32().Between(PreferredValue.MinDecade, PreferredValue.MaxDecade);
        _indexRecipe  = series => Any.Int32().Between(0, PreferredValue.ValuesPerDecade(series) - 1);
        _series       = _seriesRecipe;
        _decade       = _decadeRecipe;
        _index        = _indexRecipe;
    }

    private AnyPreferredValue(AnyPreferredValue from) {
        _series       = from._series;
        _seriesRecipe = from._seriesRecipe;
        _decade       = from._decade;
        _decadeRecipe = from._decadeRecipe;
        _index        = from._index;
        _indexRecipe  = from._indexRecipe;
    }

    public AnyPreferredValue WithSeries(ESeries value) {
        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._series = Any.Fixed(value);

        return copy;
    }

    public AnyPreferredValue WithSeries(Func<AnyEnum<ESeries>, AnyEnum<ESeries>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._seriesRecipe = configure(_seriesRecipe);
        copy._series       = copy._seriesRecipe;

        return copy;
    }

    public AnyPreferredValue WithDecade(int value) {
        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._decade = Any.Fixed(value);

        return copy;
    }

    public AnyPreferredValue WithDecade(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._decadeRecipe = configure(_decadeRecipe);
        copy._decade       = copy._decadeRecipe;

        return copy;
    }

    /// <summary>Pins the index: it must exist in the series that is drawn, or the constructor refuses it on every run.</summary>
    public AnyPreferredValue WithIndex(int value) {
        AnyPreferredValue copy = new AnyPreferredValue(this);

        copy._index = _ => Any.Fixed(value);

        return copy;
    }

    public AnyPreferredValue WithIndex(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyPreferredValue copy           = new AnyPreferredValue(this);
        Func<ESeries, AnyInt32> previous = _indexRecipe;

        copy._indexRecipe = series => configure(previous(series));
        copy._index       = series => copy._indexRecipe(series);

        return copy;
    }

    public PreferredValue Generate() {
        ESeries series = _series.Generate();

        return new PreferredValue(series, _decade.Generate(), _index(series).Generate());
    }

}

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     Hand-written (the scaffold marked every parameter "to verify": the rules of a network are between its
///     parts, not on them). Draws a radial network: a random tree of lines over buses of one voltage level,
///     exactly one slack bus, plus a few transformers each feeding one more, lower-voltage bus. Every rule of
///     <see cref="GridNetwork" /> holds by construction.
/// </summary>
public sealed class AnyGridNetwork : IAny<GridNetwork> {

    private IAny<string>              _name         = Any.String().NotBlank();
    private IAny<double>              _baseMva      = Any.Double().Between(1d, GridNetwork.MaximumBaseMva);
    private IAny<int>                 _lineBusCount = Any.Int32().Between(2, 8);
    private IAny<int>                 _transformerCount = Any.Int32().Between(0, 2);
    private Func<AnyBus, AnyBus>      _buses        = bus => bus;
    private Func<AnyLine, AnyLine>    _lines        = line => line;

    public AnyGridNetwork() { }

    private AnyGridNetwork(AnyGridNetwork from) {
        _name             = from._name;
        _baseMva          = from._baseMva;
        _lineBusCount     = from._lineBusCount;
        _transformerCount = from._transformerCount;
        _buses            = from._buses;
        _lines            = from._lines;
    }

    public AnyGridNetwork WithName(string value) {
        AnyGridNetwork copy = new AnyGridNetwork(this);

        copy._name = Any.Fixed(value);

        return copy;
    }

    public AnyGridNetwork WithBaseMva(double value) {
        AnyGridNetwork copy = new AnyGridNetwork(this);

        copy._baseMva = Any.Fixed(value);

        return copy;
    }

    /// <summary>The number of buses joined by lines, transformers' low-voltage buses not counted.</summary>
    public AnyGridNetwork WithLineBusCount(int value) {
        AnyGridNetwork copy = new AnyGridNetwork(this);

        copy._lineBusCount = Any.Fixed(value);

        return copy;
    }

    public AnyGridNetwork WithTransformerCount(int value) {
        AnyGridNetwork copy = new AnyGridNetwork(this);

        copy._transformerCount = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines every bus drawn, after the network has tied its id, voltage level and type.</summary>
    public AnyGridNetwork WithBuses(Func<AnyBus, AnyBus> configure) {
        AnyGridNetwork copy = new AnyGridNetwork(this);

        copy._buses = bus => configure(_buses(bus));

        return copy;
    }

    /// <summary>Refines every line drawn, after the network has tied its id and its two ends.</summary>
    public AnyGridNetwork WithLines(Func<AnyLine, AnyLine> configure) {
        AnyGridNetwork copy = new AnyGridNetwork(this);

        copy._lines = line => configure(_lines(line));

        return copy;
    }

    public GridNetwork Generate() {
        int    lineBusCount     = _lineBusCount.Generate();
        int    transformerCount = _transformerCount.Generate();
        int    busCount         = lineBusCount + transformerCount;
        double highKv           = Any.Double().Between(10d, Bus.MaximumBaseKv).Generate();
        double lowKv            = Any.Double().Between(0.1d, highKv * 0.9d).Generate();

        List<BusId> ids       = Any.ListOf(new AnyBusId()).WithCount(busCount).Distinct().Generate();
        int         slackIndex = Any.Int32().Between(0, lineBusCount - 1).Generate();

        List<Bus> buses = new();
        for (int i = 0; i < busCount; i++) {
            AnyBus bus = Any.Bus().WithId(ids[i]).WithBaseKv(i < lineBusCount ? highKv : lowKv);
            bus = i == slackIndex ? bus.WithType(BusType.Slack) : bus.WithType(type => type.Except(BusType.Slack));
            buses.Add(_buses(bus).Generate());
        }

        List<string> branchIds = Any.ListOf(Any.String().Alpha().WithLengthBetween(1, 32)).WithCount(busCount - 1).Distinct().Generate();

        List<Line> lines = new();
        for (int i = 1; i < lineBusCount; i++) {
            int parent = Any.Int32().Between(0, i - 1).Generate();
            lines.Add(_lines(Any.Line().WithId(branchIds[i - 1]).WithFromBus(ids[parent]).WithToBus(ids[i])).Generate());
        }

        List<Transformer> transformers = new();
        for (int t = 0; t < transformerCount; t++) {
            int hv = Any.Int32().Between(0, lineBusCount - 1).Generate();
            transformers.Add(Any.Transformer().WithId(branchIds[lineBusCount - 1 + t]).WithHighVoltageBus(ids[hv]).WithLowVoltageBus(ids[lineBusCount + t]).Generate());
        }

        return new GridNetwork(_name.Generate(), _baseMva.Generate(), buses, lines, transformers);
    }

}

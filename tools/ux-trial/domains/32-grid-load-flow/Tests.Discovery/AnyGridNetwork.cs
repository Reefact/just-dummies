using System;
using System.Collections.Generic;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary connected <see cref="GridNetwork" /> values: one common base voltage, one slack bus,
///     and a spanning tree of lines, so every bus is reachable. Written by hand: the buses, lines and transformers
///     of a network tie too many rules together for a per-parameter recipe.
/// </summary>
public sealed partial class AnyGridNetwork : IAny<GridNetwork> {

    private IAny<string> _name;
    private IAny<double> _baseMva;
    private IAny<int>    _busCount;

    public AnyGridNetwork() {
        _name     = Any.String().NotBlank();
        _baseMva  = Any.Double().Positive().LessThanOrEqualTo(GridNetwork.MaximumBaseMva);
        _busCount = Any.Int32().Between(2, 6);
    }

    private AnyGridNetwork(AnyGridNetwork from) {
        _name     = from._name;
        _baseMva  = from._baseMva;
        _busCount = from._busCount;
    }

    public AnyGridNetwork WithBaseMva(double value) {
        AnyGridNetwork copy = new AnyGridNetwork(this);

        copy._baseMva = Any.Fixed(value);

        return copy;
    }

    public AnyGridNetwork WithBusCount(int value) {
        AnyGridNetwork copy = new AnyGridNetwork(this);

        copy._busCount = Any.Fixed(value);

        return copy;
    }

    public GridNetwork Generate() {
        int          count  = _busCount.Generate();
        double       baseKv = Any.Double().Positive().LessThanOrEqualTo(Bus.MaximumBaseKv).Generate();
        List<BusId>  ids    = Any.ListOf(new AnyBusId()).WithCount(count).Distinct().Generate();

        List<Bus> buses = new List<Bus>();
        for (int i = 0; i < count; i++) {
            AnyBus bus = new AnyBus().WithId(ids[i]).WithBaseKv(baseKv);
            bus = i == 0 ? bus.WithType(BusType.Slack) : bus.WithType(t => t.Except(BusType.Slack));
            buses.Add(bus.Generate());
        }

        List<Line> lines = new List<Line>();
        for (int i = 1; i < count; i++) {
            int parent = Any.Int32().Between(0, i - 1).Generate();
            lines.Add(new AnyLine().WithId("L" + i).WithFromBus(ids[parent]).WithToBus(ids[i]).Generate());
        }

        return new GridNetwork(_name.Generate(), _baseMva.Generate(), buses, lines, new List<Transformer>());
    }

}

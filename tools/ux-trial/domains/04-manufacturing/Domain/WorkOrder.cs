namespace Manufacturing.Domain;

/// <summary>A manufacturing execution work order: produce a quantity of a product as one batch, within a shift.</summary>
public sealed class WorkOrder {

    private readonly List<string> _operators;

    public WorkOrder(BatchCode batch, string productSku, int plannedQuantity, Cadence cadence, Tolerance tolerance, ShiftWindow window, IReadOnlyList<string> operators) {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentException.ThrowIfNullOrEmpty(productSku);
        if (productSku.Length > 20) {
            throw new ArgumentException("A SKU holds 20 characters at most.", nameof(productSku));
        }
        if (plannedQuantity is < 1 or > 100_000) {
            throw new ArgumentOutOfRangeException(nameof(plannedQuantity), plannedQuantity, "A work order plans 1 to 100000 units.");
        }
        ArgumentNullException.ThrowIfNull(cadence);
        ArgumentNullException.ThrowIfNull(tolerance);
        ArgumentNullException.ThrowIfNull(window);
        if (operators.Count == 0) {
            throw new ArgumentException("A work order needs at least one operator.", nameof(operators));
        }

        Batch           = batch;
        ProductSku      = productSku;
        PlannedQuantity = plannedQuantity;
        Cadence         = cadence;
        Tolerance       = tolerance;
        Window          = window;
        _operators      = operators.ToList();
    }

    public BatchCode   Batch           { get; }
    public string      ProductSku      { get; }
    public int         PlannedQuantity { get; }
    public Cadence     Cadence         { get; }
    public Tolerance   Tolerance       { get; }
    public ShiftWindow Window          { get; }
    public IReadOnlyList<string> Operators => _operators;

    public int GoodCount  { get; private set; }
    public int ScrapCount { get; private set; }

    public TimeSpan EstimatedDuration => Cadence.TimeToProduce(PlannedQuantity);

    public bool FitsInWindow => EstimatedDuration <= Window.Length;

    public bool IsComplete => GoodCount >= PlannedQuantity;

    /// <summary>Records one inspected part: good when the measurement lies within tolerance, scrap otherwise. Requires a running machine.</summary>
    public void RecordPart(Machine machine, decimal measuredMm) {
        if (!machine.CanProduce) {
            throw new InvalidOperationException($"Machine {machine.MachineId} is {machine.State}; it cannot produce.");
        }
        if (Tolerance.Accepts(measuredMm)) {
            GoodCount++;
        } else {
            ScrapCount++;
        }
    }

    /// <summary>First-pass yield in percent, rounded to one decimal place; zero before any part is recorded.</summary>
    public decimal YieldPercent {
        get {
            int inspected = GoodCount + ScrapCount;

            return inspected == 0 ? 0m : Math.Round(100m * GoodCount / inspected, 1, MidpointRounding.AwayFromZero);
        }
    }

}

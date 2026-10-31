namespace VesselStowage.Domain;

/// <summary>
/// The loading plan of one vessel over one rotation. Every assignment is checked against the
/// boxes already stowed: support, stacking weight, handling sequence, hatch access, dangerous-goods
/// segregation, and the ship's stability with every planned box aboard (the conservative case).
/// </summary>
public sealed class StowagePlan
{
    private readonly Dictionary<SlotPosition, StowageAssignment> _bySlot = new();
    private readonly Dictionary<ContainerId, StowageAssignment> _byContainer = new();

    public StowagePlan(VesselProfile vessel, Rotation rotation)
    {
        ArgumentNullException.ThrowIfNull(vessel);
        ArgumentNullException.ThrowIfNull(rotation);
        Vessel = vessel;
        Rotation = rotation;
    }

    public VesselProfile Vessel { get; }

    public Rotation Rotation { get; }

    public IReadOnlyCollection<StowageAssignment> Assignments => _bySlot.Values;

    public decimal DisplacementTonnes => Vessel.LightshipTonnes + _bySlot.Values.Sum(a => a.Container.GrossMassTonnes);

    public double VcgMetres => ComputeVcg(_bySlot.Values, Vessel);

    public double TcgMetres => ComputeTcg(_bySlot.Values, Vessel);

    public StowageAssignment? At(SlotPosition slot)
    {
        ArgumentNullException.ThrowIfNull(slot);
        return _bySlot.GetValueOrDefault(slot);
    }

    public StowageAssignment? Find(ContainerId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _byContainer.GetValueOrDefault(id);
    }

    public StowageViolation Check(Container container, SlotPosition slot)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(slot);
        return Evaluate(container, slot).Violation;
    }

    public StowageAssignment Assign(Container container, SlotPosition slot)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(slot);
        (StowageViolation violation, string reason) = Evaluate(container, slot);
        if (violation != StowageViolation.None)
        {
            throw new StowageRefusedException(violation, $"Cannot stow {container.Id} in {slot}: {reason}");
        }

        StowageAssignment assignment = new(container, slot);
        _bySlot.Add(slot, assignment);
        _byContainer.Add(container.Id, assignment);
        return assignment;
    }

    /// <summary>Takes a box off the plan; refused while another box stands on it.</summary>
    public void Remove(ContainerId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        if (!_byContainer.TryGetValue(id, out StowageAssignment? assignment))
        {
            throw new StowageRefusedException(StowageViolation.NotStowed, $"{id} is not in the plan.");
        }

        SlotPosition slot = assignment.Slot;
        SlotPosition? above = slot.Level + 1 < (slot.IsOnDeck ? Vessel.DeckTiers : Vessel.HoldTiers)
            ? new SlotPosition(slot.Bay, slot.Row, slot.Tier + 2)
            : null;
        if (above is not null && _bySlot.ContainsKey(above))
        {
            throw new StowageRefusedException(StowageViolation.ContainerStackedAbove, $"{id} has a container stacked on it in {above}.");
        }

        _bySlot.Remove(slot);
        _byContainer.Remove(id);
    }

    /// <summary>Every slot where the container could go now, given what is already planned.</summary>
    public IReadOnlyList<SlotPosition> UsableSlotsFor(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return Vessel.AllSlots().Where(s => Evaluate(container, s).Violation == StowageViolation.None).ToList();
    }

    /// <summary>The boxes to lift at a port, in crane order: deck before hold, top tier first.</summary>
    public IReadOnlyList<StowageAssignment> DischargeSequence(PortCode port)
    {
        ArgumentNullException.ThrowIfNull(port);
        if (!Rotation.Calls(port))
        {
            throw new ArgumentException($"Port {port} is not in the rotation.", nameof(port));
        }

        return _bySlot.Values
            .Where(a => a.Container.PortOfDischarge.Equals(port))
            .OrderBy(a => a.Slot.Bay)
            .ThenByDescending(a => a.Slot.IsOnDeck)
            .ThenByDescending(a => a.Slot.Tier)
            .ThenBy(a => a.Slot.Row)
            .ToList();
    }

    private (StowageViolation Violation, string Reason) Evaluate(Container container, SlotPosition slot)
    {
        if (!Vessel.Contains(slot))
        {
            return (StowageViolation.SlotOutsideVessel, $"slot is outside {Vessel.Name}'s geometry");
        }

        if (slot.AcceptedSize != container.Size)
        {
            return (StowageViolation.BayDoesNotFitSize, $"bay {slot.Bay} takes {slot.AcceptedSize} containers");
        }

        if (_byContainer.ContainsKey(container.Id))
        {
            return (StowageViolation.AlreadyStowed, "container is already in the plan");
        }

        if (_bySlot.ContainsKey(slot))
        {
            return (StowageViolation.SlotOccupied, "slot is occupied");
        }

        if (!Rotation.Calls(container.PortOfLoading) || !Rotation.Calls(container.PortOfDischarge)
            || Rotation.IndexOf(container.PortOfLoading) >= Rotation.IndexOf(container.PortOfDischarge))
        {
            return (StowageViolation.PortNotInRotation, "its loading and discharge ports are not called in that order");
        }

        if (container.Hazard == HazardClass.Explosives && !slot.IsOnDeck)
        {
            return (StowageViolation.ExplosivesUnderDeck, "explosives are stowed on deck only");
        }

        SlotPosition? belowSlot = slot.Below();
        StowageAssignment? below = belowSlot is null ? null : _bySlot.GetValueOrDefault(belowSlot);
        if (belowSlot is not null && below is null)
        {
            return (StowageViolation.NoSupportBelow, $"nothing stands in {belowSlot}");
        }

        if (below is not null && container.GrossMassTonnes > below.Container.GrossMassTonnes)
        {
            return (StowageViolation.HeavierThanContainerBelow, "a box cannot be heavier than the one it stands on");
        }

        decimal stackWeight = container.GrossMassTonnes + _bySlot.Values
            .Where(a => a.Slot.Bay == slot.Bay && a.Slot.Row == slot.Row && a.Slot.IsOnDeck == slot.IsOnDeck)
            .Sum(a => a.Container.GrossMassTonnes);
        if (stackWeight > Vessel.MaxStackWeightTonnes)
        {
            return (StowageViolation.StackWeightExceeded, $"stack would weigh {stackWeight} t over a limit of {Vessel.MaxStackWeightTonnes} t");
        }

        foreach (StowageAssignment other in _bySlot.Values.Where(a => a.Slot.Bay == slot.Bay && a.Slot.Row == slot.Row && a.Slot.IsOnDeck == slot.IsOnDeck))
        {
            if (other.Slot.Level < slot.Level && !Sequenced(container, other.Container))
            {
                return (StowageViolation.Overstow, $"it would block {other.Container.Id} below it");
            }
        }

        foreach (StowageAssignment other in _bySlot.Values.Where(a => a.Slot.Bay == slot.Bay && a.Slot.IsOnDeck != slot.IsOnDeck))
        {
            bool blocked = slot.IsOnDeck ? !Sequenced(container, other.Container) : !Sequenced(other.Container, container);
            if (blocked)
            {
                return (StowageViolation.HatchAccessBlocked, $"the hatch cover of bay {slot.Bay} would have to come off before {other.Container.Id}'s port");
            }
        }

        foreach (StowageAssignment other in _bySlot.Values.Where(a => a.Slot.IsAdjacentTo(slot)))
        {
            if (!container.CanNeighbour(other.Container))
            {
                return (StowageViolation.SegregationConflict, $"{container.Hazard} cannot stand next to {other.Container.Hazard} ({other.Container.Id})");
            }
        }

        List<StowageAssignment> after = _bySlot.Values.Append(new StowageAssignment(container, slot)).ToList();
        decimal displacement = DisplacementTonnes + container.GrossMassTonnes;
        if (displacement > Vessel.MaxDisplacementTonnes)
        {
            return (StowageViolation.DisplacementExceeded, $"displacement would reach {displacement} t");
        }

        double vcg = ComputeVcg(after, Vessel);
        if (vcg > Vessel.MaxVcgMetres)
        {
            return (StowageViolation.VerticalStabilityExceeded, $"VCG would rise to {vcg:F2} m over {Vessel.MaxVcgMetres:F2} m");
        }

        double tcg = ComputeTcg(after, Vessel);
        if (Math.Abs(tcg) > Vessel.MaxTcgMetres)
        {
            return (StowageViolation.ListExceeded, $"TCG would reach {tcg:F3} m over {Vessel.MaxTcgMetres:F3} m");
        }

        return (StowageViolation.None, string.Empty);
    }

    /// <summary>True when <paramref name="upper"/> can sit over <paramref name="lower"/>: loaded no earlier, discharged no later.</summary>
    private bool Sequenced(Container upper, Container lower) =>
        Rotation.IndexOf(upper.PortOfLoading) >= Rotation.IndexOf(lower.PortOfLoading)
        && Rotation.IndexOf(upper.PortOfDischarge) <= Rotation.IndexOf(lower.PortOfDischarge);

    private static double ComputeVcg(IEnumerable<StowageAssignment> assignments, VesselProfile vessel)
    {
        double mass = (double)vessel.LightshipTonnes;
        double moment = mass * vessel.LightshipVcgMetres;
        foreach (StowageAssignment a in assignments)
        {
            double m = (double)a.Container.GrossMassTonnes;
            mass += m;
            moment += m * a.Slot.CentreHeightMetres(vessel.HoldTiers);
        }

        return moment / mass;
    }

    private static double ComputeTcg(IEnumerable<StowageAssignment> assignments, VesselProfile vessel)
    {
        double mass = (double)vessel.LightshipTonnes;
        double moment = 0;
        foreach (StowageAssignment a in assignments)
        {
            double m = (double)a.Container.GrossMassTonnes;
            mass += m;
            moment += m * a.Slot.TransverseOffsetMetres;
        }

        return moment / mass;
    }
}

namespace CaptiveBreeding.Domain;

public sealed partial class AnyIndividual {

    /// <summary>An individual with both parents known.</summary>
    public AnyIndividual ChildOf(StudbookId sireId, StudbookId damId) {
        return WithSireId(sireId).WithDamId(damId);
    }

}

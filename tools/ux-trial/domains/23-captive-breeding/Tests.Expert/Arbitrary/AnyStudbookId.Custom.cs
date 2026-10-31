using JustDummies;

namespace CaptiveBreeding.Domain;

// Written by hand, beside the scaffolded file: `any generate StudbookId --force` never touches it.
public sealed partial class AnyStudbookId {

    /// <summary>Any studbook id but those already taken — a studbook never assigns one twice.</summary>
    public AnyStudbookId Except(IEnumerable<StudbookId> taken) {
        string[] takenValues = taken.Select(id => id.Value).ToArray();

        // Workaround: Except() refuses an empty list ("At least one value is required."), although
        // excluding nothing from a computed, possibly empty, set is a no-op rather than a mistake.
        if (takenValues.Length == 0) {
            return this;
        }

        return WithValue(value => value.Except(takenValues));
    }

}

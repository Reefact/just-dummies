using System;

using JustDummies;

namespace OrbitalMicrobiology.Domain;

// Written by hand, beside the scaffolded file (which `any generate --force` rewrites).
public sealed partial class AnySpecies {

    /// <summary>
    ///     Pins the three cardinal temperatures together: they are one rule (min &lt; optimum &lt; max), so they are pinned as
    ///     one. Done through the three recipes the generated copy constructor already carries, because a field added here
    ///     would not survive the next <c>With…</c> call.
    /// </summary>
    public AnySpecies WithCardinalTemperatures(double min, double optimum, double max) {
        AnySpecies copy = new AnySpecies(this);

        copy._minTemperatureCelsiusRecipe     = Any.Double().Between(min, min);
        copy._optimalTemperatureCelsiusRecipe = Any.Double().Between(optimum, optimum);
        copy._maxTemperatureCelsiusRecipe     = Any.Double().Between(max, max);

        return copy;
    }

}

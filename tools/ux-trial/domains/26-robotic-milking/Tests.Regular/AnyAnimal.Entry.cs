// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Animal --entry-point any --force` overwrites it.
// It needs C# 14: a static extension member is what reaches this spelling without touching the library.

using JustDummies;

namespace RoboticMilking.Domain;

/// <summary>Hangs <c>Any.Animal()</c> off the library's own entry point.</summary>
public static class AnyAnimalEntry {

    extension(Any) {

        /// <summary>Starts an arbitrary <c>Animal</c>: constrain it through <c>With…</c>, then <c>Generate()</c>.</summary>
        public static AnyAnimal Animal() {
            return new AnyAnimal();
        }

    }

}

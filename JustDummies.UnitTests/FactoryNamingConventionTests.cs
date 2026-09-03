#region Usings declarations

using System.Reflection;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Locks the factory-naming rule recorded in ADR-0010: every parameterless, type-named scalar factory
///     on <see cref="Dummy" /> is named after the CLR type it produces — which is also the name of its
///     <c>Dummy{ClrType}</c> builder. This is the guard that would have caught the <c>Bool</c>/<c>AnyBool</c>
///     deviation before release. The <see cref="Dummy" />↔<see cref="DummyContext" /> mirror itself is guarded
///     separately by <c>SurfaceParityTests</c>.
/// </summary>
public sealed class FactoryNamingConventionTests {

    // The type-named scalar factories are exactly Dummy's public, static, non-generic, parameterless methods
    // whose return type is a builder (implements IDummy<T>). StringMatching (parameters), Enum<T> (generic),
    // the collection/composition factories (generic, parameterized) and WithSeed/Reproducibly (not builders)
    // fall out by construction, so no hand-maintained allow-list can drift out of sync with the surface.
    private static IEnumerable<MethodInfo> ScalarFactories() {
        return typeof(Dummy).GetMethods(BindingFlags.Public | BindingFlags.Static)
                          .Where(method => !method.IsGenericMethod
                                        && method.GetParameters().Length == 0
                                        && ElementTypeOf(method.ReturnType) is not null);
    }

    // The T of the single IDummy<T> a builder implements, or null when the type is not a builder.
    private static Type? ElementTypeOf(Type builder) {
        return builder.GetInterfaces()
                      .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IDummy<>))
                      ?.GetGenericArguments()[0];
    }

    [Fact(DisplayName = "Every type-named scalar factory, and its builder, is named after the CLR type it produces.")]
    public void FactoriesAreNamedAfterTheirClrType() {
        List<MethodInfo> factories = ScalarFactories().ToList();

        // Guards the reflection itself: were the query ever to match nothing, every assertion below would pass vacuously.
        Check.That(factories.Count).IsStrictlyGreaterThan(15);

        foreach (MethodInfo factory in factories) {
            Type   builder = factory.ReturnType;
            string clrName = ElementTypeOf(builder)!.Name;

            Check.WithCustomMessage($"Dummy.{factory.Name}() returns {builder.Name} (IDummy<{clrName}>); the factory must be named '{clrName}', after the CLR type it produces.")
                 .That(factory.Name).IsEqualTo(clrName);
            Check.WithCustomMessage($"The builder for {clrName} is named '{builder.Name}'; it must be 'Dummy{clrName}' to match the CLR type.")
                 .That(builder.Name).IsEqualTo("Dummy" + clrName);
        }
    }

}

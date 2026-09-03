using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     The naming function of §11.3, which every emitted type name goes through.
/// </summary>
public sealed class TypeNamingTests {

    private const string Source = """
                                  namespace Shop.Domain {
                                      public sealed class Order {
                                          public sealed class Line { }
                                      }
                                  }
                                  """;

    [Fact(DisplayName = "A type is named by the pattern: Order becomes DummyOrder.")]
    public void ATypeIsNamedByThePattern() {
        ITypeSymbol order = TypeIn(Source, "Shop.Domain.Order");

        Check.That(TypeNaming.GeneratorNameFor(order, NamingOptions.Default)).IsEqualTo("DummyOrder");
    }

    // §3.2: a nested type scaffolds to a TOP-LEVEL generator named after the nested type alone, so
    // `dum generate Order.Line` writes DummyLine and not DummyOrderLine.
    [Fact(DisplayName = "A nested type is named after itself alone: Order.Line becomes DummyLine.")]
    public void ANestedTypeIsNamedAfterItselfAlone() {
        ITypeSymbol line = TypeIn(Source, "Shop.Domain.Order+Line");

        Check.That(TypeNaming.GeneratorNameFor(line, NamingOptions.Default)).IsEqualTo("DummyLine");
    }

    [Fact(DisplayName = "The v1.0 pattern is Dummy{Type}.")]
    public void TheDefaultPatternIsTheOneVersionOneDotZeroOffers() {
        Check.That(NamingOptions.Default.Pattern).IsEqualTo("Dummy" + NamingOptions.TypePlaceholder);
    }

    // A compilation with no references: the target type is declared in this source, so the symbol resolves
    // even though System.Object does not. Nothing here needs a full reference set.
    private static ITypeSymbol TypeIn(string source, string metadataName) {
        CSharpCompilation compilation = CSharpCompilation.Create("Naming", [CSharpSyntaxTree.ParseText(source)]);
        INamedTypeSymbol? type        = compilation.GetTypeByMetadataName(metadataName);

        Check.WithCustomMessage($"The fixture does not declare {metadataName}.").That(type).IsNotNull();

        return type!;
    }

}

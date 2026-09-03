using Microsoft.CodeAnalysis;

namespace JustDummies.Analyzers;

/// <summary>
///     Resolves the types an analyzer matches against, by metadata name. Analyzers never reference the JustDummies
///     assembly directly — they are loaded into the consumer's compiler — so a null field simply means the type is not
///     part of the analyzed compilation, in which case the rule that needs it stays silent.
/// </summary>
/// <remarks>
///     The xUnit and adapter lookups are what let a rule reason about a test's lifecycle without JustDummies ever
///     depending on either: a consumer who uses neither gets a compilation where those fields are null, and the rules
///     that need them never register.
/// </remarks>
internal sealed class KnownSymbols {

    public const string DummyMetadataName        = "JustDummies.Dummy";
    public const string IDummyMetadataName       = "JustDummies.IDummy`1";
    public const string DummyContextMetadataName = "JustDummies.DummyContext";

    public const string ReproducibleAttributeMetadataName = "JustDummies.Xunit.ReproducibleAttribute";
    public const string FactAttributeMetadataName         = "Xunit.v3.IFactAttribute";
    public const string MemberDataAttributeMetadataName   = "Xunit.MemberDataAttribute";

    private KnownSymbols(Compilation compilation) {
        Dummy                   = compilation.GetTypeByMetadataName(DummyMetadataName);
        IDummy                  = compilation.GetTypeByMetadataName(IDummyMetadataName);
        DummyContext            = compilation.GetTypeByMetadataName(DummyContextMetadataName);
        ReproducibleAttribute = compilation.GetTypeByMetadataName(ReproducibleAttributeMetadataName);
        FactAttribute         = compilation.GetTypeByMetadataName(FactAttributeMetadataName);
        MemberDataAttribute   = compilation.GetTypeByMetadataName(MemberDataAttributeMetadataName);
    }

    /// <summary>The <c>JustDummies.Dummy</c> façade, or <c>null</c> when the compilation does not reference JustDummies.</summary>
    public INamedTypeSymbol? Dummy { get; }

    /// <summary>The unbound <c>JustDummies.IDummy&lt;T&gt;</c> generator interface, or <c>null</c> as above.</summary>
    public INamedTypeSymbol? IDummy { get; }

    /// <summary>The isolated <c>JustDummies.DummyContext</c>, whose draws are unaffected by the ambient seed scope.</summary>
    public INamedTypeSymbol? DummyContext { get; }

    /// <summary>The xUnit adapter's <c>[Reproducible]</c>, or <c>null</c> when the adapter is not referenced.</summary>
    public INamedTypeSymbol? ReproducibleAttribute { get; }

    /// <summary>The interface every xUnit test attribute implements — <c>[Fact]</c>, <c>[Theory]</c> and derivatives.</summary>
    public INamedTypeSymbol? FactAttribute { get; }

    /// <summary>xUnit's <c>[MemberData]</c>, which names the member producing a theory's cases.</summary>
    public INamedTypeSymbol? MemberDataAttribute { get; }

    public static KnownSymbols From(Compilation compilation) {
        return new KnownSymbols(compilation);
    }

}

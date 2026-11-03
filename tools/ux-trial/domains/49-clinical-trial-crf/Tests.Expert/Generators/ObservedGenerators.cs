// Hand-written: `any generate Observed` refuses a generic type, and the closing it would need is the test's to choose.

using JustDummies;

namespace ClinicalTrial.Domain;

/// <summary>Dummies for the resolved field <c>Observed&lt;T&gt;</c>: observed, missing for a reason, or either.</summary>
public static class AnyObservedOf {

    /// <summary>A field observed with a value drawn from <paramref name="values" />.</summary>
    public static IAny<Observed<T>> OfValues<T>(IAny<T> values) where T : struct {
        return values.As(Observed<T>.Of);
    }

    /// <summary>A field missing for any reason.</summary>
    public static IAny<Observed<T>> Missing<T>() where T : struct {
        return Any.Enum<MissingReason>().As(Observed<T>.Missing);
    }

    /// <summary>A field missing for any reason but the ones in <paramref name="except" />.</summary>
    public static IAny<Observed<T>> MissingExcept<T>(params MissingReason[] except) where T : struct {
        return Any.Enum<MissingReason>().Except(except).As(Observed<T>.Missing);
    }

    /// <summary>A resolved field: observed or missing, nothing blank.</summary>
    public static IAny<Observed<T>> Resolved<T>(IAny<T> values) where T : struct {
        return Any.Boolean().Then(isObserved => isObserved ? OfValues(values) : Missing<T>());
    }

    /// <summary>A field that may be blank (<c>null</c>) one draw in ten, resolved otherwise.</summary>
    public static IAny<Observed<T>?> MaybeBlank<T>(IAny<T> values) where T : struct {
        return Resolved(values).OrNull();
    }

}

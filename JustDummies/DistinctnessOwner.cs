namespace JustDummies;

/// <summary>
///     Why a <see cref="CollectionState{T}" /> must be distinct — which constraint a caller meeting an
///     unbuildable distinctness should be shown, since the three shapes name it differently: a redeclarable
///     <c>Distinct()</c> call for the list-shaped generators, <c>SetOf(...)</c> itself for a set, and the key
///     generator of <c>DictionaryOf(...)</c> for a dictionary's keys.
/// </summary>
internal enum DistinctnessOwner {

    /// <summary>Distinctness was declared with a redeclarable <c>Distinct()</c> call.</summary>
    DeclaredCall,

    /// <summary>Distinctness is inherent to a set — there is no call to blame or to remove.</summary>
    SetIdentity,

    /// <summary>Distinctness is inherent to a dictionary's keys — there is no call to blame or to remove.</summary>
    DictionaryKeys

}

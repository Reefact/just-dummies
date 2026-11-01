namespace JustDummies;

/// <summary>
///     The comparer <c>DistinctBy</c> hands a collection: two elements are the same when their keys are, under the key
///     type's default equality. A <c>null</c> key is a key like any other.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
internal sealed class KeyEqualityComparer<T, TKey> : IEqualityComparer<T> {

    #region Fields declarations

    private readonly Func<T, TKey> _keySelector;

    #endregion

    internal KeyEqualityComparer(Func<T, TKey> keySelector) {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    /// <inheritdoc />
    public bool Equals(T? x, T? y) {
        return EqualityComparer<TKey>.Default.Equals(_keySelector(x!), _keySelector(y!));
    }

    /// <inheritdoc />
    public int GetHashCode(T obj) {
        TKey key = _keySelector(obj);

        return key is null ? 0 : EqualityComparer<TKey>.Default.GetHashCode(key);
    }

}

#if NETSTANDARD2_0
namespace GSoft.Extensions.MediatR;

internal static class DictionaryExtensions
{
    public static void TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
        }
    }
}
#endif
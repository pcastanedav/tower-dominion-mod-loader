namespace TDModLoader.Handlers.Utils;
using System.Collections.Generic;

public class Bijection<T1, T2>
    where T1 : notnull
    where T2 : notnull
{
    private readonly Dictionary<T1, T2> _forward = new();
    private readonly Dictionary<T2, T1> _inverse = new();

    public void Add(T1 key, T2 value)
    {
        _forward[key] = value;
        _inverse[value] = key;
    }

    public T2 this[T1 key] => _forward[key];
    public T1 this[T2 key] => _inverse[key];

    public bool ContainsKey(T1 key) => _forward.ContainsKey(key);
    public bool ContainsValue(T2 key) => _inverse.ContainsKey(key);

    public bool Remove(T1 key)
    {
        return _forward.ContainsKey(key) && _inverse.Remove(_forward[key]) && _forward.Remove(key);
    }
}
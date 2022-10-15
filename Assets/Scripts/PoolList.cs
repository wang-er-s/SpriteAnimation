using System;
using System.Collections.Generic;

public class PoolList<T> : List<T> , IDisposable
{
    private bool disposed = false;
    
    private PoolList()
    {
    }

    private PoolList(int count) : base(count)
    {
    }

    private static List<PoolList<T>> cache = new();

    public static PoolList<T> Create(int count = 6)
    {
        PoolList<T> result = null;
        if (cache.Count > 0)
        {
            for (int i = 0; i < cache.Count; i++)
            {
                if (cache[i].Capacity == count)
                {
                    result = cache[i];
                    cache.RemoveAt(i);
                    break;
                }
            }

            if (result == null)
            {
                result = cache[cache.Count - 1];
                cache.RemoveAt(cache.Count - 1);
            }
        }

        if (result == null)
            result = new PoolList<T>(count);

        result.disposed = false;
        return result;
    }


    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        this.Clear();
        cache.Add(this);
    }
}

public class PoolDic<TKey,TValue> : Dictionary<TKey, TValue>, IDisposable
{
    private PoolDic()
    {
    }
    private static Queue<PoolDic<TKey, TValue>> cache = new();

    public static PoolDic<TKey,TValue> Create()
    {
        PoolDic<TKey,TValue> result = null;
        if (cache.Count > 0)
        {
            result = cache.Dequeue();
        }
        if (result == null)
            result = new PoolDic<TKey, TValue>(); 
        return result;
    }

    public void Dispose()
    {
        this.Clear();
        cache.Enqueue(this);
    }
}
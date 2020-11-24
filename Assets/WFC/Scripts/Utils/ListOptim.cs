using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ListOptim<T> where T : class
{
    public T[] data;
    public int size;

    public void Init(int sizeMax)
    {
        data = new T[sizeMax];
    }

    public void Add(T t)
    {
        data[size] = t;
        size++;
    }

    public void Remove(T t)
    {
        for (int i = size - 1; i >= 0; i--) {
            if (data[i] == t)
                RemoveAt(i);
        }
    }

    public void RemoveAt(int i)
    {
        data[i] = data[size-1];
        size--;
    }

    //public T this[int i]
    //{
    //    get => data[i];
    //    set => data[i] = value;
    //}

    public void Clear()
    {
        size = 0;
    }
}

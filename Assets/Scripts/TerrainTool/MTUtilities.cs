using System;
using System.Diagnostics.Contracts;
using UnityEngine;

public static class MTLog
{
    public static void Log(object message)
    {
        Debug.Log(message);
    }
    public static void LogError(object message)
    {
        Debug.LogError(message);
    }
}

//TODO:数据需要维护一个发生变化的MeshID Array，减少不必要的遍历
public class MTArray<T>
{
    public T[] Data;
    public int Length { get; private set; }
    public MTArray(int len)
    {
        Reallocate(len);
    }
    public void Reallocate(int len)
    {
        if (Data != null && len < Data.Length)
            return;
        Data = new T[len];
        Length = 0;
    }
    public void Reset()
    {
        for (int i = 0; i < Length; i++)
        {
            Data[i] = default(T);
        }
        Length = 0;
    }
    public void Add(T item)
    {
        if (Data == null || Length >= Data.Length)
        {
            MTLog.LogError("MTArray overflow : " + typeof(T));
        }
        Data[Length] = item;
        ++Length;
    }
}
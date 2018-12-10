using System;
using System.Diagnostics;
using ABCat.Shared;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
public static class Context
{
    private static readonly object SyncObject = new object();

    private static volatile IContext _i;

    [NotNull]
    public static IContext I
    {
        get => _i;

        set
        {
            if (_i == null)
            {
                Debug.Assert(SyncObject != null, "SyncObject != null");

                lock (SyncObject)
                {
                    if (_i == null)
                    {
                        _i = value;
                    }
                    else
                    {
                        throw new Exception("Shared context instance was already initialized");
                    }
                }
            }
            else
            {
                throw new Exception("Shared context instance was already initialized");
            }
        }
    }
}
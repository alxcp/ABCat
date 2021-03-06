﻿using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Shared.Everywhere;

// ReSharper disable once CheckNamespace
public static class SharedContext
{
    private static readonly object SyncObject = new object();

    private static volatile ISharedContext _i;

    [NotNull]
    public static ISharedContext I
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
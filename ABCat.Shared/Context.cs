using ABCat.Shared;

// ReSharper disable once CheckNamespace
public static class Context
{
    public static IContext I => (IContext)SharedContext.I;
}
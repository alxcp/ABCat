namespace ABCat.Shared.Plugins.Catalog
{
    public interface ISymbolicDistanceResult
    {
        string RusOriginal { get; }
        string LatOriginal { get; }
        double Cost { get; }
        int LanguageType { get; }
    }
}

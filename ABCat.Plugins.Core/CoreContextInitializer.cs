using ABCat.Shared;

namespace ABCat.Core
{
    public static class CoreContextInitializer
    {
        public static IContext Initialize()
        {
            return CoreContext.I;
        }
    }
}
using ABCat.Shared;

namespace ABCat.Core
{
    public static class ContextInitializer
    {
        public static IContext Init()
        {
            return new CoreContext();
        }
    }
}
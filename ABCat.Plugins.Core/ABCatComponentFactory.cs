using Component.Infrastructure;

namespace ABCat.Core
{
    public class AbCatComponentFactory : ComponentFactoryBase
    {
        public AbCatComponentFactory(string componentFolder)
            : base(componentFolder)
        {
        }

        protected override string ComponentAssemblyNamePattern => "ABCat.Plugins.*";
    }
}
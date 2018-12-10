using System;

namespace Component.Infrastructure.Factory
{
    public class SingletoneComponentCreator : ComponentCreatorBase
    {
        private readonly Lazy<IExtComponent> _instance;

        public SingletoneComponentCreator(
            Type pluginType,
            ComponentInfoAttribute pluginInfoAttribute)
            : base(pluginType, pluginInfoAttribute)
        {
            _instance = new Lazy<IExtComponent>(CreateNewInstance, true);
        }

        public override T GetInstance<T>()
        {
            return (T) _instance.Value;
        }
    }
}
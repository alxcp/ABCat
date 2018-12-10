using System;

namespace Component.Infrastructure.Factory
{
    public class PerCallComponentCreator : ComponentCreatorBase
    {
        public PerCallComponentCreator(Type pluginType, ComponentInfoAttribute pluginInfoAttribute)
            : base(pluginType, pluginInfoAttribute)
        {
        }

        public override T GetInstance<T>()
        {
            return (T) CreateNewInstance();
        }
    }
}
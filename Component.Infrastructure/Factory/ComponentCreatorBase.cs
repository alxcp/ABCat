using System;

namespace Component.Infrastructure.Factory
{
    public abstract class ComponentCreatorBase
    {
        protected ComponentCreatorBase(Type pluginType, ComponentInfoAttribute pluginInfoAttribute)
        {
            ComponentType = pluginType;
            ComponentInfoAttribute = pluginInfoAttribute;
        }

        public ComponentInfoAttribute ComponentInfoAttribute { get; }

        public Type ComponentType { get; }

        public abstract T GetInstance<T>() where T : IExtComponent;

        protected IExtComponent CreateNewInstance()
        {
            var result = (IExtComponent) Activator.CreateInstance(ComponentType);
            result.CheckForConfig(false, out _);
            return result;
        }
    }
}
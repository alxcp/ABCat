using System;
using System.Collections.Generic;

namespace Component.Infrastructure.Factory
{
    public interface IComponentFactory
    {
        T CreateActual<T>() where T : IExtComponent;

        ComponentCreatorBase GetActualCreator<T>() where T : IExtComponent;

        IEnumerable<ComponentCreatorBase> GetCreators<T>()
            where T : IExtComponent;

        void Init();

        IEnumerable<Type> GetConfigAttributes();
    }
}
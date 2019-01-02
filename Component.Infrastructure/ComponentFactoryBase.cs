using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Component.Infrastructure.Factory;

namespace Component.Infrastructure
{
    public abstract class ComponentFactoryBase : IComponentFactory
    {
        private readonly List<ComponentCreatorBase> _creators = new List<ComponentCreatorBase>();

        private readonly HashSet<Type> _pluginConfigTypes = new HashSet<Type>();

        protected ComponentFactoryBase(string componentsFolder)
        {
            ComponentsFolderPath = componentsFolder;
        }

        protected abstract string ComponentAssemblyNamePattern { get; }

        public T CreateActual<T>() where T : IExtComponent
        {
            try
            {
                var creator = GetActualCreator<T>();
                if (creator == null)
                {
                    throw new Exception($"Creator of Type {typeof(T).Name} not found");
                }

                return creator.GetInstance<T>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception during creation component of type {typeof(T).Name}", ex);
            }
        }

        public IEnumerable<Type> GetConfigAttributes()
        {
            return _pluginConfigTypes.ToArray();
        }

        public string ComponentsFolderPath { get; }

        public ComponentCreatorBase GetActualCreator<T>() where T : IExtComponent
        {
            return
                GetCreators<T>()
                    .OrderByDescending(item => new Version(item.ComponentInfoAttribute.Version))
                    .FirstOrDefault();
        }

        public IEnumerable<ComponentCreatorBase> GetCreators<T>()
            where T : IExtComponent
        {
            return _creators.Where(IsAcceptableCreator<T>);
        }

        public IReadOnlyCollection<T> CreateAll<T>() where T : IExtComponent
        {
            return GetCreators<T>().Select(item => item.GetInstance<T>()).ToArray();
        }

        public void Init()
        {
            LoadComponents(ComponentsFolderPath, ComponentAssemblyNamePattern + ".dll");
            LoadComponents(ComponentsFolderPath, ComponentAssemblyNamePattern + ".exe");
            InitInternal();
        }

        public virtual void Dispose()
        {
        }

        protected virtual bool AssemblyNameFilter(string assemblyName)
        {
            return assemblyName.IndexOf("vshost.exe", StringComparison.OrdinalIgnoreCase) == -1;
        }

        protected virtual bool ComponentInfoAttributeFilter(ComponentInfoAttribute componentInfoAttribute)
        {
            return componentInfoAttribute.IsEnabled;
        }

        protected virtual void InitInternal()
        {
        }

        private IEnumerable<ComponentInfoAttribute> GetComponentInfoAttributes(Type type)
        {
            return
                type.GetCustomAttributes(typeof(ComponentInfoAttribute))
                    .Cast<ComponentInfoAttribute>()
                    .Where(ComponentInfoAttributeFilter);
        }

        private bool IsAcceptableCreator<T>(
            ComponentCreatorBase creator) where T : IExtComponent
        {
            if (!creator.ComponentType.GetInterfaces().Contains(typeof(T)))
            {
                return false;
            }

            return true;
        }

        private void LoadComponents(string componentsFolder, string filePattern)
        {
            var assemblies = Directory.GetFiles(componentsFolder, filePattern, SearchOption.AllDirectories);

            foreach (var dll in assemblies.Where(AssemblyNameFilter))
            {
                try
                {
                    var assembly = Assembly.LoadFile(dll);

                    foreach (var type in assembly.GetTypes())
                    {
                        foreach (var infoAttribute in GetComponentInfoAttributes(type))
                        {
                            Register(type, infoAttribute);
                        }

                        if (type.InheritsFrom(typeof(Config)))
                        {
                            _pluginConfigTypes.Add(type);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"An error ocurred on trying to load plugins from '{dll}'.", ex);
                }
            }
        }

        private void Register(Type componentType, ComponentInfoAttribute componentInfoAttribute)
        {
            switch (componentInfoAttribute.InstanceMode)
            {
                case InstanceModeEnum.PerCall:
                    _creators.Add(new PerCallComponentCreator(componentType, componentInfoAttribute));
                    break;
                case InstanceModeEnum.Singletone:
                    _creators.Add(new SingletoneComponentCreator(componentType, componentInfoAttribute));
                    break;
            }
        }
    }
}
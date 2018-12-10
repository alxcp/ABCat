using System;

namespace Component.Infrastructure.Factory
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class ComponentInfoAttribute : Attribute
    {
        protected ComponentInfoAttribute(
            InstanceModeEnum workMode,
            string version,
            bool isEnabled = true)
        {
            Version = version;

            IsEnabled = isEnabled;
            InstanceMode = workMode;
        }

        public InstanceModeEnum InstanceMode { get; }

        public bool IsEnabled { get; set; }

        public string Version { get; }
    }
}
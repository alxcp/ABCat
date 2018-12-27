using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Component.Infrastructure
{
    public abstract class Config : Saveable
    {
        protected static readonly string LayoutFolderPath;

        public static readonly string ConfigFolderPath;

        static Config()
        {
            ConfigFolderPath = SharedContext.I.GetAppDataFolderPath("Configs");
            Directory.CreateDirectory(ConfigFolderPath);
            LayoutFolderPath = SharedContext.I.GetAppDataFolderPath("Layouts");
            Directory.CreateDirectory(LayoutFolderPath);
        }

        [Browsable(false)] public abstract string DisplayName { get; }

        [Browsable(false)] [XmlIgnore] public bool IsChanged { get; private set; }

        public override string SaveGroupId => GetPluginConfigFilePath(GetType());

        public static bool CheckDirectory(string path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }

        public static bool CheckFile(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public static Config CreateDefaultInstance(Type configType)
        {
            Config result;
            var contextCtor = configType.GetConstructor(new Type[0]);
            if (contextCtor != null && contextCtor.IsPublic)
                result = (Config) Activator.CreateInstance(configType);
            else
            {
                var defaultCtor = configType.GetConstructor(new Type[0]);
                if (defaultCtor != null && defaultCtor.IsPublic) result = (Config) Activator.CreateInstance(configType);
                else
                    throw new Exception(
                        $"Configuration class '{configType.Assembly.FullName}' '{configType.FullName}' has no supportable constructors");
            }

            return result;
        }

        public static T Load<T>()
            where T : Config
        {
            return (T) Load(typeof(T));
        }

        public static Config Load(Type configType)
        {
            if (!TryLoad(configType, out var result))
            {
                result = CreateDefaultInstance(configType);
            }

            result.IsChanged = false;

            return result;
        }

        public static void SaveLayout(string layoutName, byte[] layout)
        {
            File.WriteAllBytes(GetLayoutFileName(layoutName), layout);
        }

        public static bool TryLoad(Type configType, out Config config)
        {
            var file = GetPluginConfigFilePath(configType);
            if (File.Exists(file))
            {
                using (var fs = File.OpenRead(file))
                {
                    var xs = new XmlSerializer(configType);
                    config = (Config) xs.Deserialize(fs);
                    return true;
                }
            }

            config = null;
            return false;
        }

        public static bool TryLoadLayout(string layoutName, out byte[] layout)
        {
            layout = null;
            var layoutFileName = GetLayoutFileName(layoutName);
            if (File.Exists(layoutFileName))
            {
                layout = File.ReadAllBytes(layoutFileName);
                return true;
            }

            return false;
        }

        public abstract bool Check(bool correct);

        public override void Save()
        {
            this.SerializeToXmlFile(GetPluginConfigFilePath(GetType()));
            IsChanged = false;
        }

        public override void SaveGroup(IEnumerable<Saveable> group)
        {
            foreach (var saveable in group)
            {
                saveable.Save();
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }

        protected override void OnPropertyChanged(string propertyName = null)
        {
            IsChanged = true;
            base.OnPropertyChanged(propertyName);
        }

        private static string GetLayoutFileName(string layoutName)
        {
            var invalid = new string(Path.GetInvalidFileNameChars());
            if (invalid.Any(layoutName.Contains))
                throw new Exception($"Name of the layout '{layoutName}' contains invalid characters.");
            return Path.Combine(LayoutFolderPath, layoutName);
        }

        private static string GetPluginConfigFilePath(Type configType)
        {
            return Path.Combine(ConfigFolderPath, $"{configType.Name}.xml");
        }
    }
}
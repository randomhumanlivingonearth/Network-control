using System;
using SFS.IO;
using UITools;

namespace NetworkControlMod
{
    public class Config : ModSettings<ConfigData>
    {
        private static Config instance;

        protected override FilePath SettingsFile => Main.modFolder.ExtendToFile("Config.txt");

        public static void Load()
        {
            instance = new Config();
            instance.Initialize();
        }

        protected override void RegisterOnVariableChange(Action onChange)
        {
            // Use the static settings from the base class
            var s = ModSettings<ConfigData>.settings;
            s.maxLines.OnChange += onChange;
            s.showFullPath.OnChange += onChange;
        }
    }
}
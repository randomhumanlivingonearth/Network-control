using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using System.Collections.Generic;
using UnityEngine;

namespace NetworkControlMod
{
    public class Main : Mod
    {
        public override string ModNameID => "NetworkControlMod";
        public override string DisplayName => "Network Control";
        public override string Author => "randomhumanlivingonearth";
        public override string MinimumGameVersionNecessary => "1.5.9.8";
        public override string ModVersion => "v1.0.0f";
        public override string Description => "Adds network range and line‑of‑sight to probe cores.";

        public override Dictionary<string, string> Dependencies { get; } = new Dictionary<string, string> { { "UITools", "1.0" } };

        public static FolderPath modFolder;
        private static Harmony patcher;

        public override void Early_Load()
        {
            modFolder = new FolderPath(ModFolder);
            Config.Load();
            patcher = new Harmony("com.yourname.networkcontrol");
            patcher.PatchAll();
        }

        public override void Load()
        {
            GameObject handlerObj = new GameObject("NetworkKeyHandler");
            Object.DontDestroyOnLoad(handlerObj);
            handlerObj.AddComponent<NetworkKeyHandler>();
        }
    }
}
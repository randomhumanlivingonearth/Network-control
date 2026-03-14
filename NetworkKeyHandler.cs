using SFS.Input;
using UITools;
using UnityEngine;

namespace NetworkControlMod
{
    public class NetworkKeyHandler : MonoBehaviour
    {
        private static KeybindingsPC.Key increaseKey;
        private static KeybindingsPC.Key decreaseKey;
        private static KeybindingsPC.Key togglePathKey;

        public static void RegisterKeys(KeybindingsPC.Key inc, KeybindingsPC.Key dec, KeybindingsPC.Key tog)
        {
            increaseKey = inc;
            decreaseKey = dec;
            togglePathKey = tog;
        }

        public static void SaveKeys()
        {
            // This method is called when keys are changed via UI
            // No additional saving needed; base class handles it
        }

        void Update()
        {
            if (increaseKey != null && (increaseKey as I_Key).IsKeyDown())
            {
                ModSettings<ConfigData>.settings.maxLines.Value++;
            }
            if (decreaseKey != null && (decreaseKey as I_Key).IsKeyDown())
            {
                if (ModSettings<ConfigData>.settings.maxLines.Value > 0)
                    ModSettings<ConfigData>.settings.maxLines.Value--;
            }
            if (togglePathKey != null && (togglePathKey as I_Key).IsKeyDown())
            {
                ModSettings<ConfigData>.settings.showFullPath.Value ^= true;
            }
        }
    }
}
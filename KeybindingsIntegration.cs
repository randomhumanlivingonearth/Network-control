using HarmonyLib;
using SFS.Input;
using SFS.UI.ModGUI;
using UITools;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Type = SFS.UI.ModGUI.Type;
using System.Collections.Generic;

namespace NetworkControlMod
{
    [HarmonyPatch(typeof(KeybindingsPC), "Awake")]
    public static class KeybindingsIntegration
    {
        private static bool added = false;

        static void Postfix(KeybindingsPC __instance)
        {
            if (added) return;
            added = true;

            Transform holder = __instance.keybindingsHolder;
            if (holder == null) return;

            __instance.CreateSpace();

            // Section title
            GameObject titleObj = Object.Instantiate(__instance.textPrefab, holder);
            TMP_Text tmp = titleObj.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = "Network Control";

            // Max Lines label
            Builder.CreateLabel(holder, 380, 30, 0, 0, "Max Lines (0 = unlimited)");

            // Button row with horizontal layout
            GameObject buttonRow = new GameObject("ButtonRow");
            buttonRow.transform.SetParent(holder, false);
            var rowLayout = buttonRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.spacing = 10;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;

            // +1 button
            var btnPlus = Builder.CreateButton(buttonRow.transform, 110, 50, 0, 0, () =>
            {
                ModSettings<ConfigData>.settings.maxLines.Value++;
            }, "+1");
            btnPlus.gameObject.AddComponent<LayoutElement>().minWidth = 110;

            // -1 button
            var btnMinus = Builder.CreateButton(buttonRow.transform, 110, 50, 0, 0, () =>
            {
                if (ModSettings<ConfigData>.settings.maxLines.Value > 0)
                    ModSettings<ConfigData>.settings.maxLines.Value--;
            }, "-1");
            btnMinus.gameObject.AddComponent<LayoutElement>().minWidth = 110;

            // Reset button
            var btnReset = Builder.CreateButton(buttonRow.transform, 110, 50, 0, 0, () =>
            {
                ModSettings<ConfigData>.settings.maxLines.Value = 0;
            }, "Reset");
            btnReset.gameObject.AddComponent<LayoutElement>().minWidth = 110;

            // Toggle for path mode
            Builder.CreateToggleWithLabel(holder, 380, 30,
                () => ModSettings<ConfigData>.settings.showFullPath.Value,
                () => ModSettings<ConfigData>.settings.showFullPath.Value ^= true,
                0, 0, "Show path for active probe");

            // Add custom keybindings
            AddKeybindings(__instance, holder);

            __instance.CreateSpace();
            LayoutRebuilder.ForceRebuildLayoutImmediate(holder as RectTransform);
        }

        private static void AddKeybindings(KeybindingsPC __instance, Transform holder)
        {
            var increaseKey = new KeybindingsPC.Key { key = KeyCode.None, ctrl = false };
            var decreaseKey = new KeybindingsPC.Key { key = KeyCode.None, ctrl = false };
            var togglePathKey = new KeybindingsPC.Key { key = KeyCode.None, ctrl = false };

            NetworkKeyHandler.RegisterKeys(increaseKey, decreaseKey, togglePathKey);

            __instance.CreateSpace();
            Builder.CreateLabel(holder, 380, 30, 0, 0, "Network Keybindings");

            __instance.Create(new KeybindingsPC.Key[] { increaseKey }, new KeybindingsPC.Key[] { new KeybindingsPC.Key { key = KeyCode.None, ctrl = false } }, "Increase Max Lines", NetworkKeyHandler.SaveKeys);
            __instance.Create(new KeybindingsPC.Key[] { decreaseKey }, new KeybindingsPC.Key[] { new KeybindingsPC.Key { key = KeyCode.None, ctrl = false } }, "Decrease Max Lines", NetworkKeyHandler.SaveKeys);
            __instance.Create(new KeybindingsPC.Key[] { togglePathKey }, new KeybindingsPC.Key[] { new KeybindingsPC.Key { key = KeyCode.None, ctrl = false } }, "Toggle Path Mode", NetworkKeyHandler.SaveKeys);
        }
    }
}
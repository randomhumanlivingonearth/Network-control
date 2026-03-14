using HarmonyLib;
using SFS.Parts;
using SFS.Parts.Modules;
using UnityEngine;
using System;

namespace NetworkControlMod
{
    [HarmonyPatch(typeof(PartsLoader), "CreatePart", new[] { typeof(Part), typeof(string), typeof(Action<Part>), typeof(Action<Part>) })]
    public static class PartCreationPatch
    {
        static void Postfix(Part __result)
        {
            // Only proceed if the part has a ControlModule (i.e., it's a controllable part)
            if (__result.GetComponent<ControlModule>() == null)
                return;

            // Exclude parts whose name suggests they are crewed capsules.
            string partName = __result.name.ToLowerInvariant();
            if (partName.Contains("capsule") || partName.Contains("crew"))
                return;

            // This part is a probe – add the network control component.
            __result.gameObject.AddComponent<NetworkControl>();
        }
    }
}
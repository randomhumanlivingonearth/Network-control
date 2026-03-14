This is not a simple mod which adds a network for probes, basically you cant control your probes if it isnt connected to it.



It adds network control module by following script:
```
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
```
you can ask me to add more names for your custom part pack, if you need it.


Please leave credits to me if you use the source code.

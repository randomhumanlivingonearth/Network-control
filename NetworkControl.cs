using System.Collections.Generic;
using SFS;
using SFS.Parts.Modules;
using SFS.World;
using SFS.World.Maps;
using SFS.WorldBase;
using UITools;
using UnityEngine;

namespace NetworkControlMod
{
    public class NetworkControl : MonoBehaviour
    {
        private static HashSet<NetworkControl> allProbes = new HashSet<NetworkControl>();
        private static bool mapOpen = false;
        private static bool subscriptionsAdded = false;
        private static Dictionary<NetworkControl, NetworkControl> parentMap = new Dictionary<NetworkControl, NetworkControl>();
        private static HashSet<NetworkControl> connectedSet = new HashSet<NetworkControl>();

        private Rocket rocket;
        private ControlModule controlModule;
        private LineRenderer mapLine;
        private bool isConnected;
        private Location baseLocation;
        private double worldTime => WorldTime.main.worldTime;

        private Double2 cachedWorldPos;

        private const float lineScreenWidth = 0.0025f;
        private const float minLineWidth = 0.001f;
        private const double eps = 1e-2;

        void Start()
        {
            if (!subscriptionsAdded && Map.manager != null)
            {
                Map.manager.mapMode.OnChange += OnGlobalMapModeChanged;
                subscriptionsAdded = true;
                mapOpen = Map.manager.mapMode.Value;
            }

            rocket = GetComponentInParent<Rocket>();
            if (rocket == null) { enabled = false; return; }

            controlModule = GetComponent<ControlModule>();
            if (controlModule == null) { enabled = false; return; }

            SpaceCenterData spaceCenter = Base.planetLoader.spaceCenter;
            if (spaceCenter?.LaunchPadLocation == null)
            {
                Debug.LogError("NetworkControl: Space center data missing.");
                enabled = false;
                return;
            }
            baseLocation = spaceCenter.LaunchPadLocation;

            controlModule.hasControl.Value = false;

            allProbes.Add(this);
            CreateMapLine();

            if (Map.view != null)
                Map.view.view.distance.OnChange += UpdateLineWidth;
        }

        void OnDestroy()
        {
            allProbes.Remove(this);
            parentMap.Remove(this);
            connectedSet.Remove(this);
            if (Map.view != null)
                Map.view.view.distance.OnChange -= UpdateLineWidth;
            if (mapLine != null)
                Destroy(mapLine.gameObject);
        }

        void Update()
        {
            if (rocket == null || controlModule == null)
                return;

            cachedWorldPos = rocket.location.Value.GetSolarSystemPosition(worldTime);
        }

        private static bool globalUpdateScheduled = false;
        private void LateUpdate()
        {
            if (!globalUpdateScheduled)
            {
                globalUpdateScheduled = true;
                PerformGlobalNetworkUpdate();
                globalUpdateScheduled = false;
            }
        }

        private static void PerformGlobalNetworkUpdate()
        {
            double currentTime = WorldTime.main.worldTime;
            Double2 basePos = Base.planetLoader.spaceCenter.LaunchPadLocation.GetSolarSystemPosition(currentTime);

            parentMap.Clear();
            connectedSet.Clear();

            Queue<NetworkControl> queue = new Queue<NetworkControl>();

            foreach (var probe in allProbes)
            {
                if (HasLineOfSight(basePos, probe.cachedWorldPos, currentTime))
                {
                    parentMap[probe] = null;
                    connectedSet.Add(probe);
                    queue.Enqueue(probe);
                }
            }

            while (queue.Count > 0)
            {
                NetworkControl current = queue.Dequeue();

                foreach (var probe in allProbes)
                {
                    if (probe == current || parentMap.ContainsKey(probe))
                        continue;

                    if (HasLineOfSight(current.cachedWorldPos, probe.cachedWorldPos, currentTime))
                    {
                        parentMap[probe] = current;
                        connectedSet.Add(probe);
                        queue.Enqueue(probe);
                    }
                }
            }

            foreach (var probe in allProbes)
            {
                bool connected = connectedSet.Contains(probe);
                probe.isConnected = connected;
                probe.controlModule.hasControl.Value = connected;
            }

            if (!mapOpen) return;

            var settings = ModSettings<ConfigData>.settings;
            int maxLines = settings.maxLines.Value;
            bool pathMode = settings.showFullPath.Value;

            Debug.Log($"[NetworkControl] maxLines = {maxLines}, pathMode = {pathMode}, total connected = {connectedSet.Count}");

            List<NetworkControl> toShow = new List<NetworkControl>();

            if (pathMode)
            {
                // Path mode: only active probe's chain
                Player activePlayer = PlayerController.main.player.Value;
                if (activePlayer is Rocket activeRocket)
                {
                    NetworkControl activeProbe = activeRocket.GetComponentInChildren<NetworkControl>();
                    if (activeProbe != null && connectedSet.Contains(activeProbe))
                    {
                        NetworkControl current = activeProbe;
                        while (current != null)
                        {
                            toShow.Add(current);
                            if (!parentMap.TryGetValue(current, out current))
                                break;
                        }
                        Debug.Log($"[NetworkControl] Path mode: chain length = {toShow.Count}");
                    }
                }
            }
            else
            {
                // Full network mode – prioritize active probe's chain
                List<NetworkControl> allConnected = new List<NetworkControl>(connectedSet);
                List<NetworkControl> chain = new List<NetworkControl>();

                // Get active probe's chain
                Player activePlayer = PlayerController.main.player.Value;
                if (activePlayer is Rocket activeRocket)
                {
                    NetworkControl activeProbe = activeRocket.GetComponentInChildren<NetworkControl>();
                    if (activeProbe != null && connectedSet.Contains(activeProbe))
                    {
                        NetworkControl current = activeProbe;
                        while (current != null)
                        {
                            chain.Add(current);
                            if (!parentMap.TryGetValue(current, out current))
                                break;
                        }
                    }
                }

                // Apply maxLines limit, ensuring chain is included first
                if (maxLines > 0)
                {
                    // First add the chain (up to maxLines)
                    toShow = new List<NetworkControl>(chain);
                    if (toShow.Count > maxLines)
                        toShow = toShow.GetRange(0, maxLines);
                    else
                    {
                        // Fill remaining slots with other probes (excluding those already in chain)
                        List<NetworkControl> others = allConnected.FindAll(p => !chain.Contains(p));
                        int remaining = maxLines - toShow.Count;
                        if (remaining > 0 && others.Count > 0)
                            toShow.AddRange(others.GetRange(0, Mathf.Min(remaining, others.Count)));
                    }
                    Debug.Log($"[NetworkControl] Priority: chain included, selected {toShow.Count} probes");
                }
                else
                {
                    toShow = allConnected;
                }
            }

            // Disable all lines first
            int disabled = 0;
            foreach (var probe in connectedSet)
            {
                if (probe.mapLine != null && probe.mapLine.gameObject.activeSelf)
                {
                    probe.mapLine.gameObject.SetActive(false);
                    disabled++;
                }
            }
            if (disabled > 0) Debug.Log($"[NetworkControl] Disabled {disabled} lines");

            // Enable and update only the selected probes
            int enabled = 0;
            foreach (var probe in toShow)
            {
                if (probe.mapLine == null) continue;
                probe.mapLine.gameObject.SetActive(true);
                probe.UpdateMapLinePosition();
                enabled++;
            }
            Debug.Log($"[NetworkControl] Enabled {enabled} lines");
        }

        private static bool HasLineOfSight(Double2 A, Double2 B, double time)
        {
            Double2 dir = B - A;
            double lenSq = dir.sqrMagnitude;

            foreach (Planet planet in Base.planetLoader.planets.Values)
            {
                Double2 C = planet.GetSolarSystemPosition(time);
                double r = planet.Radius;

                Double2 AC = A - C;
                double a = Double2.Dot(dir, dir);
                double b = 2.0 * Double2.Dot(dir, AC);
                double c = Double2.Dot(AC, AC) - r * r;

                double discriminant = b * b - 4.0 * a * c;
                if (discriminant < 0)
                    continue;

                double sqrtD = System.Math.Sqrt(discriminant);
                double t1 = (-b - sqrtD) / (2.0 * a);
                double t2 = (-b + sqrtD) / (2.0 * a);

                const double tEps = 1e-5;
                if ((t1 > tEps && t1 < 1.0 - tEps) || (t2 > tEps && t2 < 1.0 - tEps))
                    return false;

                if ((A - C).sqrMagnitude < r * r - eps || (B - C).sqrMagnitude < r * r - eps)
                    return false;
            }
            return true;
        }

        private void CreateMapLine()
        {
            GameObject lineObj = new GameObject("NetworkMapLine_" + rocket.name);
            lineObj.layer = LayerMask.NameToLayer("Map");

            if (Map.manager != null)
                lineObj.transform.SetParent(Map.manager.transform, false);

            mapLine = lineObj.AddComponent<LineRenderer>();

            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.white;
            mapLine.material = mat;

            mapLine.positionCount = 2;
            mapLine.useWorldSpace = true;
            mapLine.sortingLayerName = "Map";
            mapLine.sortingOrder = 5;

            mapLine.startWidth = lineScreenWidth;
            mapLine.endWidth = lineScreenWidth;

            mapLine.startColor = new Color(0, 1, 0, 0.7f);
            mapLine.endColor = new Color(0, 1, 0, 0.7f);

            UpdateLineWidth();
        }

        private void UpdateLineWidth()
        {
            if (mapLine == null || Map.view == null) return;
            float width = Map.view.ToConstantSize(lineScreenWidth);
            width = Mathf.Max(width, minLineWidth);
            mapLine.startWidth = width;
            mapLine.endWidth = width;
        }

        private void UpdateMapLinePosition()
        {
            if (rocket == null) return;

            Vector2 relative = rocket.location.Value.position.ToVector2;
            Vector3 startMapPos = rocket.location.Value.planet.mapHolder.position + new Vector3(relative.x, relative.y, 0f) / 1000f;

            Vector3 endMapPos;
            if (!parentMap.TryGetValue(this, out NetworkControl parent) || parent == null)
            {
                endMapPos = MapDrawer.GetPosition(baseLocation);
            }
            else
            {
                Vector2 parentRelative = parent.rocket.location.Value.position.ToVector2;
                endMapPos = parent.rocket.location.Value.planet.mapHolder.position + new Vector3(parentRelative.x, parentRelative.y, 0f) / 1000f;
            }

            mapLine.SetPosition(0, startMapPos);
            mapLine.SetPosition(1, endMapPos);
        }

        private static void OnGlobalMapModeChanged()
        {
            mapOpen = Map.manager.mapMode.Value;
            if (!mapOpen)
            {
                foreach (var probe in allProbes)
                {
                    if (probe.mapLine != null && probe.mapLine.gameObject.activeSelf)
                        probe.mapLine.gameObject.SetActive(false);
                }
            }
        }
    }
}
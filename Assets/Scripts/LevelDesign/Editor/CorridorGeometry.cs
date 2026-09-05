using System.Collections.Generic;
using UnityEngine;

namespace Echo.LevelDesign.Editor
{
    // Clockwise outline viewed from above; each portal is an edge left open in the wall shell.
    public sealed class CorridorOutline
    {
        public readonly List<Vector3> Inner = new();
        public readonly List<Vector3> Outer = new();
        public readonly HashSet<int> OpenEdges = new();
        public Vector3[] PortPositions;
        public float[] PortYaws;
        public Vector3 Center;
    }

    public static class CorridorGeometry
    {
        public static Vector3 Direction(float yaw) => new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad), 0f, Mathf.Cos(yaw * Mathf.Deg2Rad));

        public static CorridorOutline Create(CorridorModule module)
        {
            if (!module.TryValidate(out string error)) throw new System.ArgumentException(error);
            var result = new CorridorOutline { PortYaws = module.GetPortYaws() };
            result.PortPositions = new Vector3[result.PortYaws.Length];
            float h = module.Width * 0.5f;
            float outer = h + module.Thickness;
            if (module.Kind == CorridorKind.Straight)
            {
                float length = module.Length;
                result.Center = Vector3.forward * length * 0.5f;
                result.Inner.AddRange(new[] { new Vector3(-h, 0, 0), new Vector3(-h, 0, length), new Vector3(h, 0, length), new Vector3(h, 0, 0) });
                result.Outer.AddRange(new[] { new Vector3(-outer, 0, 0), new Vector3(-outer, 0, length), new Vector3(outer, 0, length), new Vector3(outer, 0, 0) });
                result.OpenEdges.UnionWith(new[] { 1, 3 });
                result.PortPositions[1] = Vector3.forward * length;
                return result;
            }
            if (module.Kind == CorridorKind.EndCap) return result;

            var order = new List<int>();
            for (int i = 0; i < result.PortYaws.Length; i++) order.Add(i);
            order.Sort((a, b) => result.PortYaws[a].CompareTo(result.PortYaws[b]));
            float minimum = 180f;
            for (int i = 0; i < order.Count; i++)
                for (int j = i + 1; j < order.Count; j++)
                    minimum = Mathf.Min(minimum, Mathf.Abs(Mathf.DeltaAngle(result.PortYaws[i], result.PortYaws[j])));
            // Leave the requested straight arm beyond even the OUTER wall's furthest miter.
            float radius = outer / Mathf.Tan(minimum * 0.5f * Mathf.Deg2Rad) + module.ArmLength;
            foreach (int index in order)
            {
                int next = order[(order.IndexOf(index) + 1) % order.Count];
                float yaw = result.PortYaws[index];
                float gap = Mathf.Repeat(result.PortYaws[next] - yaw, 360f);
                Vector3 direction = Direction(yaw);
                Vector3 right = Direction(yaw + 90f);
                Vector3 port = direction * radius;
                result.PortPositions[index] = port;
                result.OpenEdges.Add(result.Inner.Count);
                result.Inner.Add(port - right * h);
                result.Inner.Add(port + right * h);
                result.Outer.Add(port - right * outer);
                result.Outer.Add(port + right * outer);
                Vector3 bisector = Direction(yaw + gap * 0.5f) / Mathf.Sin(gap * 0.5f * Mathf.Deg2Rad);
                result.Inner.Add(bisector * h);
                result.Outer.Add(bisector * outer);
            }
            return result;
        }
    }
}

using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class NeighborFanStrategy : IFillRuleStrategy
{
    public string Name => "Neighbor Fan";
    public string Description => "Neighbor fan - each triangle uses adjacent vertices from different edges";

    public List<int[]>? Triangulate(
        int[] sortedIndices,
        Point3D[] sorted3D,
        Point3D centroid,
        double nx, double ny, double nz,
        Action<string>? log = null)
    {
        int n = sortedIndices.Length;
        var triangles = new List<int[]>();

        if (n == 3)
        {
            triangles.Add(new[] { 0, 1, 2 });
            log?.Invoke("  NeighborFan: single triangle");
            return triangles;
        }

        var faceSet = new HashSet<int>(sortedIndices);
        var neighbors = new List<int>[n];
        for (int i = 0; i < n; i++) neighbors[i] = new List<int>();

        foreach (var edge in _edgeGetter?.Invoke() ?? Array.Empty<int[]>())
        {
            int v1 = edge[0], v2 = edge[1];
            int p1 = -1, p2 = -1;
            for (int i = 0; i < n; i++)
            {
                if (sortedIndices[i] == v1) p1 = i;
                if (sortedIndices[i] == v2) p2 = i;
            }
            if (p1 >= 0 && p2 >= 0)
            {
                neighbors[p1].Add(p2);
                neighbors[p2].Add(p1);
            }
        }

        for (int i = 0; i < n; i++)
        {
            neighbors[i].Sort((a, b) => {
                double angA = Math.Atan2(sorted3D[a].Y - centroid.Y, sorted3D[a].X - centroid.X);
                double angB = Math.Atan2(sorted3D[b].Y - centroid.Y, sorted3D[b].X - centroid.X);
                return angA.CompareTo(angB);
            });
        }

        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            var nbrs = neighbors[next];
            foreach (int candidate in nbrs)
            {
                if (candidate != i && candidate != next)
                {
                    triangles.Add(new[] { i, next, candidate });
                    break;
                }
            }
        }

        if (triangles.Count == 0)
        {
            log?.Invoke("  NeighborFan: no neighbors found, falling back to ear-clip");
            var earClip = new EarClipTriangulationStrategy();
            return earClip.Triangulate(sortedIndices, sorted3D, centroid, nx, ny, nz, log);
        }

        log?.Invoke($"  NeighborFan: {triangles.Count} triangles");
        return triangles;
    }

    public Model3DGroup? RenderFaces(
        int[][] faces,
        Point3D[] vertices,
        int[][] edges,
        Point3D centroid,
        Point3D cameraPosition,
        Vector3D cameraLookDirection,
        double alpha,
        Func<Point3D, Point3D> transform,
        Action<string>? log = null) => null;

    private static Func<int[][]>? _edgeGetter;
    public static void SetEdgeGetter(Func<int[][]>? getter) => _edgeGetter = getter;
}

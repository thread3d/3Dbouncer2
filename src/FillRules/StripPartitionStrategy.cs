using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class StripPartitionStrategy : IFillRuleStrategy
{
    public string Name => "Strip Partition";
    public string Description => "Strip decomposition - partitions polygon into monotone strips before triangulating";

    public List<int[]>? Triangulate(
        int[] sortedIndices,
        Point3D[] sorted3D,
        Point3D centroid,
        double nx, double ny, double nz,
        Action<string>? log = null)
    {
        int n = sortedIndices.Length;
        log?.Invoke($"  StripPartition: starting with {n} vertices");

        if (n == 3)
        {
            var result = new List<int[]> { new[] { 0, 1, 2 } };
            log?.Invoke("  StripPartition: single triangle");
            return result;
        }

        int split = -1;
        for (int i = 0; i < n; i++)
        {
            int prev = (i - 1 + n) % n;
            int next = (i + 1) % n;
            double cross = Cross2D(sorted3D[prev], sorted3D[i], sorted3D[next]);
            if (cross < 0)
            {
                split = i;
                break;
            }
        }

        if (split < 2 || split > n - 3)
        {
            log?.Invoke("  StripPartition: no reflex vertex, falling back to ear-clip");
            var earClip = new EarClipTriangulationStrategy();
            return earClip.Triangulate(sortedIndices, sorted3D, centroid, nx, ny, nz, log);
        }

        var triangles = new List<int[]>();

        int part1Count = split + 1;
        for (int i = 0; i < part1Count - 2; i++)
        {
            triangles.Add(new[] { 0, i + 1, i + 2 });
        }

        int part2Count = n - split;
        for (int i = 0; i < part2Count - 2; i++)
        {
            triangles.Add(new[] { split, split + 1 + i, split + 2 + i });
        }

        log?.Invoke($"  StripPartition: {triangles.Count} triangles ({part1Count - 2} from part1, {part2Count - 2} from part2)");
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

    private double Cross2D(Point3D a, Point3D b, Point3D c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
}

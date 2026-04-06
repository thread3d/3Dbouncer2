using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class AlternatingFanStrategy : IFillRuleStrategy
{
    public string Name => "Alternating Fan";
    public string Description => "Alternates between two fan centers - creates zigzag triangulation pattern";

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
            if (log != null) log("  AlternatingFan: single triangle");
            return triangles;
        }

        int fan1 = 0;
        int fan2 = n / 2;

        if (log != null) log("  AlternatingFan: fan1=" + fan1 + ", fan2=" + fan2);

        int i = fan1;
        int j = fan2;

        for (int step = 0; step < n - 2; step++)
        {
            int nextI = (i + 1) % n;
            int nextJ = (j - 1 + n) % n;

            if (nextI == j || nextJ == i) break;

            if (step % 2 == 0)
            {
                triangles.Add(new[] { i, nextI, nextJ });
                j = nextJ;
            }
            else
            {
                triangles.Add(new[] { j, nextJ, nextI });
                i = nextI;
            }
        }

        if (triangles.Count < n - 2)
        {
            var fan = new FanTriangulationStrategy();
            return fan.Triangulate(sortedIndices, sorted3D, centroid, nx, ny, nz, log);
        }

        if (log != null) log("  AlternatingFan: " + triangles.Count + " triangles");
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
}

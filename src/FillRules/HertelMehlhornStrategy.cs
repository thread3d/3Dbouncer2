using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class HertelMehlhornStrategy : IFillRuleStrategy
{
    public string Name => "Star Bipartition";
    public string Description => "Centroid hub + two vertex groups - distinctive star-shaped triangulation";

    public List<int[]>? Triangulate(
        int[] sortedIndices,
        Point3D[] sorted3D,
        Point3D centroid,
        double nx, double ny, double nz,
        Action<string>? log = null)
    {
        int n = sortedIndices.Length;
        if (log != null) log("  StarBi: starting with " + n + " vertices");

        if (n == 3)
        {
            var result = new List<int[]> { new[] { 0, 1, 2 } };
            if (log != null) log("  StarBi: single triangle");
            return result;
        }

        var pts2D = new (int idx, double angle, double dist)[n];
        for (int i = 0; i < n; i++)
        {
            var v = sorted3D[i];
            double dx = v.X - centroid.X;
            double dy = v.Y - centroid.Y;
            double angle = Math.Atan2(dy, dx);
            double dist = Math.Sqrt(dx * dx + dy * dy);
            pts2D[i] = (i, angle, dist);
        }

        Array.Sort(pts2D, (a, b) => a.angle.CompareTo(b.angle));

        int half = n / 2;
        var groupA = new List<int>();
        var groupB = new List<int>();
        for (int i = 0; i < n; i++)
        {
            if (i < half) groupA.Add(pts2D[i].idx);
            else groupB.Add(pts2D[i].idx);
        }

        var triangles = new List<int[]>();

        for (int i = 0; i < groupA.Count - 1; i++)
        {
            triangles.Add(new[] { n, groupA[i], groupA[i + 1] });
        }

        for (int i = 0; i < groupB.Count - 1; i++)
        {
            triangles.Add(new[] { n, groupB[i], groupB[i + 1] });
        }

        if (log != null) log("  StarBi: " + triangles.Count + " triangles (" + groupA.Count + " + " + groupB.Count + " vertices)");
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

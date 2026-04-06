using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class ConvexHullStrategy : IFillRuleStrategy
{
    public string Name => "Convex Hull (Non-Zero)";
    public string Description => "Convex hull + ear-clip fallback - non-zero winding fill rule";

    public List<int[]>? Triangulate(
        int[] sortedIndices,
        Point3D[] sorted3D,
        Point3D centroid,
        double nx, double ny, double nz,
        Action<string>? log = null)
    {
        int n = sortedIndices.Length;
        if (log != null) log("  ConvexHull: starting with " + n + " vertices");

        if (n == 3)
        {
            var result = new List<int[]> { new[] { 0, 1, 2 } };
            if (log != null) log("  ConvexHull: single triangle");
            return result;
        }

        var pts2D = new Point3D[n];
        for (int i = 0; i < n; i++)
            pts2D[i] = To2D(sorted3D[i], nx, ny, nz);

        var hull = ComputeHull(pts2D);
        if (log != null) log("  ConvexHull: hull has " + hull.Count + " vertices");

        if (hull.Count == n)
        {
            if (log != null) log("  ConvexHull: polygon is convex, fan from centroid is correct");
            var triangles = new List<int[]>();
            for (int i = 0; i < n; i++)
            {
                triangles.Add(new[] { n, i, (i + 1) % n });
            }
            return triangles;
        }

        if (log != null) log("  ConvexHull: polygon is concave, using ear-clip fallback");
        var earClip = new EarClipTriangulationStrategy();
        return earClip.Triangulate(sortedIndices, sorted3D, centroid, nx, ny, nz, log);
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

    private Point3D To2D(Point3D v, double nx, double ny, double nz)
    {
        if (Math.Abs(nz) >= Math.Abs(nx) && Math.Abs(nz) >= Math.Abs(ny))
            return new Point3D(v.X, v.Y, 0);
        else if (Math.Abs(ny) >= Math.Abs(nx) && Math.Abs(ny) >= Math.Abs(nz))
            return new Point3D(v.X, v.Z, 0);
        else
            return new Point3D(v.Y, v.Z, 0);
    }

    private List<int> ComputeHull(Point3D[] pts)
    {
        int n = pts.Length;
        if (n <= 1) return new List<int> { 0 };

        var order = Enumerable.Range(0, n).OrderBy(i => pts[i].X).ThenBy(i => pts[i].Y).ToArray();

        Point3D[] sortedPts = new Point3D[n];
        for (int i = 0; i < n; i++)
            sortedPts[i] = pts[order[i]];

        var lower = new List<int>();
        for (int i = 0; i < n; i++)
        {
            while (lower.Count >= 2 && Cross(sortedPts[lower[lower.Count - 2]], sortedPts[lower[lower.Count - 1]], sortedPts[i]) <= 0)
                lower.RemoveAt(lower.Count - 1);
            lower.Add(i);
        }

        var upper = new List<int>();
        for (int i = n - 1; i >= 0; i--)
        {
            while (upper.Count >= 2 && Cross(sortedPts[upper[upper.Count - 2]], sortedPts[upper[upper.Count - 1]], sortedPts[i]) <= 0)
                upper.RemoveAt(upper.Count - 1);
            upper.Add(i);
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);

        var result = new List<int>();
        foreach (int idx in lower) result.Add(order[idx]);
        foreach (int idx in upper) result.Add(order[idx]);

        return result;
    }

    private double Cross(Point3D a, Point3D b, Point3D c)
        => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
}

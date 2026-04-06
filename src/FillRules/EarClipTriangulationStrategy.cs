using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Ear-clipping triangulation using the even-odd fill rule.
/// Correctly handles both convex and concave (non-self-intersecting) polygons.
/// Returns triangles as triplets of POSITION indices [0..n-1] for face vertices,
/// with [n] reserved as the centroid sentinel (matching FanTriangulationStrategy).
/// </summary>
public class EarClipTriangulationStrategy : IFillRuleStrategy
{
    public string Name => "Ear Clipping (Even-Odd)";
    public string Description => "Ear clipping triangulation - correct for any simple polygon (convex or concave)";

    public List<int[]>? Triangulate(
        int[] sortedIndices,
        Point3D[] sorted3D,
        Point3D centroid,
        double nx, double ny, double nz,
        Action<string>? log = null)
    {
        int n = sortedIndices.Length;
        log?.Invoke($"  EarClip: starting with {n} vertices");

        // Build working list of position indices [0, 1, ..., n-1]
        var indices = Enumerable.Range(0, n).ToList();
        var triangles = new List<int[]>();

        int iteration = 0;
        while (indices.Count > 3)
        {
            iteration++;
            bool earFound = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                bool isEar = IsEar(indices, prev, curr, next, sorted3D, nx, ny, nz);

                if (isEar)
                {
                    // Use position indices (0..n-1), matching fan convention
                    triangles.Add(new[] { prev, curr, next });
                    indices.RemoveAt(i);
                    earFound = true;
                    log?.Invoke($"    iter {iteration}: ear at {curr}, remaining={indices.Count}");
                    break;
                }
            }

            if (!earFound)
            {
                log?.Invoke($"  WARNING: No ear found after {iteration} iterations! Falling back to fan from centroid.");
                // Fall back to fan from centroid using position indices
                for (int i = 0; i < indices.Count; i++)
                {
                    triangles.Add(new[] { n, indices[i], indices[(i + 1) % indices.Count] });
                }
                break;
            }
        }

        if (indices.Count == 3)
        {
            triangles.Add(new[] { indices[0], indices[1], indices[2] });
        }

        log?.Invoke($"  EarClip: finished with {triangles.Count} triangles");
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

    private bool IsEar(List<int> indices, int prev, int curr, int next,
        Point3D[] sorted3D, double nx, double ny, double nz)
    {
        var a = To2D(sorted3D[prev], nx, ny, nz);
        var b = To2D(sorted3D[curr], nx, ny, nz);
        var c = To2D(sorted3D[next], nx, ny, nz);

        if (Cross2D(Subtract(b, a), Subtract(c, b)) <= 0)
            return false;

        foreach (int idx in indices)
        {
            if (idx == prev || idx == curr || idx == next)
                continue;

            var p = To2D(sorted3D[idx], nx, ny, nz);
            if (PointInTriangle(a, b, c, p))
                return false;
        }

        return true;
    }

    private Point3D To2D(Point3D v, double nx, double ny, double nz)
    {
        if (Math.Abs(nz) >= Math.Abs(nx) && Math.Abs(nz) >= Math.Abs(ny))
            return new Point3D(v.X, v.Y, 0);
        else if (Math.Abs(ny) >= Math.Abs(nx) && Math.Abs(ny) >= Math.Abs(nz))
            return new Point3D(v.X, v.Z, 0);
        else
            return new Point3D(v.Y, v.Z, 0);
    }

    private static Point3D Subtract(Point3D a, Point3D b)
        => new Point3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    private double Cross2D(Point3D a, Point3D b)
        => a.X * b.Y - a.Y * b.X;

    private bool PointInTriangle(Point3D a, Point3D b, Point3D c, Point3D p)
    {
        double det = Cross2D(Subtract(b, a), Subtract(c, a));
        if (Math.Abs(det) < 1e-10) return false;

        double u = Cross2D(Subtract(b, a), Subtract(p, a)) / det;
        double v = Cross2D(Subtract(c, a), Subtract(p, a)) / det;

        return u >= 0 && v >= 0 && u + v <= 1;
    }
}

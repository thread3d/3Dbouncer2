using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class DialStrategy : IFillRuleStrategy
{
    public string Name => "Dial (Min Angle)";
    public string Description => "Fan from smallest-angle vertex - produces thin elongated triangles";

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
            if (log != null) log("  Dial: single triangle");
            return triangles;
        }

        double minAngle = double.MaxValue;
        int dialVertex = 0;

        for (int i = 0; i < n; i++)
        {
            int prev = (i - 1 + n) % n;
            int next = (i + 1) % n;

            double angle = InteriorAngle(sorted3D[prev], sorted3D[i], sorted3D[next]);
            if (angle < minAngle)
            {
                minAngle = angle;
                dialVertex = i;
            }
        }

        if (log != null) log("  Dial: vertex " + dialVertex + " with angle " + (minAngle * 180 / Math.PI).ToString("F1") + " deg");

        for (int i = 1; i < n - 1; i++)
        {
            int i0 = dialVertex;
            int i1 = (dialVertex + i) % n;
            int i2 = (dialVertex + i + 1) % n;
            triangles.Add(new[] { i0, i1, i2 });
        }

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

    private double InteriorAngle(Point3D a, Point3D b, Point3D c)
    {
        double ux = a.X - b.X, uy = a.Y - b.Y, uz = a.Z - b.Z;
        double vx = c.X - b.X, vy = c.Y - b.Y, vz = c.Z - b.Z;

        double dot = ux * vx + uy * vy + uz * vz;
        double lu = Math.Sqrt(ux * ux + uy * uy + uz * uz);
        double lv = Math.Sqrt(vx * vx + vy * vy + vz * vz);

        if (lu < 1e-10 || lv < 1e-10) return 0;
        double cosAngle = Math.Max(-1, Math.Min(1, dot / (lu * lv)));
        return Math.Acos(cosAngle);
    }
}

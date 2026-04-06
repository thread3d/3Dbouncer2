using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class KirkpatrickStrategy : IFillRuleStrategy
{
    public string Name => "Kirkpatrick Fan";
    public string Description => "Fan from highest-degree vertex - different triangulation than centroid fan";

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
            log?.Invoke("  Kirkpatrick: single triangle");
            return triangles;
        }

        double maxAngle = 0;
        int fanRoot = 0;
        for (int i = 0; i < n; i++)
        {
            int prev = (i - 1 + n) % n;
            int next = (i + 1) % n;
            var a = sorted3D[prev];
            var b = sorted3D[i];
            var c = sorted3D[next];

            double angle1 = Math.Atan2(a.Y - b.Y, a.X - b.X);
            double angle2 = Math.Atan2(c.Y - b.Y, c.X - b.X);
            double angleDiff = Math.Abs(angle2 - angle1);
            if (angleDiff > Math.PI) angleDiff = 2 * Math.PI - angleDiff;

            if (angleDiff > maxAngle)
            {
                maxAngle = angleDiff;
                fanRoot = i;
            }
        }

        log?.Invoke($"  Kirkpatrick: fan root at position {fanRoot}");

        for (int i = 1; i < n - 1; i++)
        {
            int i0 = fanRoot;
            int i1 = (fanRoot + i) % n;
            int i2 = (fanRoot + i + 1) % n;
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
}

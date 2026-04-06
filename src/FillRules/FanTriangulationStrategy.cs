using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Fan triangulation from centroid.
/// Works correctly for convex polygons; may produce incorrect results for concave polygons.
/// </summary>
public class FanTriangulationStrategy : IFillRuleStrategy
{
    public string Name => "Fan from Centroid";
    public string Description => "Simple fan from centroid - fast but only correct for convex faces";

    public List<int[]>? Triangulate(int[] sortedIndices, Point3D[] sorted3D, Point3D centroid,
        double nx, double ny, double nz, Action<string>? log = null)
    {
        int n = sortedIndices.Length;
        var triangles = new List<int[]>();

        if (n == 3)
        {
            triangles.Add(new[] { 0, 1, 2 });
            log?.Invoke("  Fan: single triangle, no triangulation needed");
        }
        else
        {
            for (int i = 0; i < n; i++)
            {
                triangles.Add(new[] { n, i, (i + 1) % n }); // centroid index = n
            }
            log?.Invoke($"  Fan: {triangles.Count} triangles from centroid");
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

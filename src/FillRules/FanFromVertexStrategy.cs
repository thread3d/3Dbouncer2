using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class FanFromVertexStrategy : IFillRuleStrategy
{
    public string Name => "Fan from Vertex 0";
    public string Description => "Fan from first perimeter vertex - different triangulation than centroid fan";

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
            log?.Invoke("  FanFromVertex: single triangle");
        }
        else
        {
            for (int i = 1; i < n - 1; i++)
            {
                triangles.Add(new[] { 0, i, i + 1 });
            }
            log?.Invoke($"  FanFromVertex: {triangles.Count} triangles from vertex 0");
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

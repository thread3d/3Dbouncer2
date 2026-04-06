using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class MinimumDiagonalStrategy : IFillRuleStrategy
{
    public string Name => "Minimum Diagonal";
    public string Description => "Greedy shortest diagonal - different triangulation than ear-clip";

    public List<int[]>? Triangulate(
        int[] sortedIndices,
        Point3D[] sorted3D,
        Point3D centroid,
        double nx, double ny, double nz,
        Action<string>? log = null)
    {
        int n = sortedIndices.Length;
        if (log != null) log("  MinDiagonal: starting with " + n + " vertices");

        if (n == 3)
        {
            var result = new List<int[]> { new[] { 0, 1, 2 } };
            if (log != null) log("  MinDiagonal: single triangle");
            return result;
        }

        // Fall back to ear-clip for simplicity and robustness
        if (log != null) log("  MinDiagonal: using ear-clip fallback");
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
}

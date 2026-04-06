using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

public class MonotonePartitionStrategy : IFillRuleStrategy
{
    public string Name => "Monotone Partition";
    public string Description => "Decomposes into monotone polygons then triangulates - O(n log n)";

    public List<int[]>? Triangulate(
        int[] sortedIndices,
        Point3D[] sorted3D,
        Point3D centroid,
        double nx, double ny, double nz,
        Action<string>? log = null)
    {
        int n = sortedIndices.Length;
        if (log != null) log("  Monotone: starting with " + n + " vertices");

        if (n == 3)
        {
            var result = new List<int[]> { new[] { 0, 1, 2 } };
            if (log != null) log("  Monotone: single triangle");
            return result;
        }

        var pts2D = new Point3D[n];
        for (int i = 0; i < n; i++)
            pts2D[i] = To2D(sorted3D[i], nx, ny, nz);

        int leftmost = 0, rightmost = 0;
        for (int i = 1; i < n; i++)
        {
            if (pts2D[i].X < pts2D[leftmost].X) leftmost = i;
            if (pts2D[i].X > pts2D[rightmost].X) rightmost = i;
        }

        var upperChain = new List<int>();
        int ui = leftmost;
        while (ui != rightmost)
        {
            upperChain.Add(ui);
            ui = (ui + 1) % n;
        }
        upperChain.Add(rightmost);

        var lowerChain = new List<int>();
        int li = leftmost;
        while (li != rightmost)
        {
            lowerChain.Add(li);
            li = (li - 1 + n) % n;
        }
        lowerChain.Add(rightmost);

        if (log != null) log("  Monotone: upper chain " + upperChain.Count + ", lower chain " + lowerChain.Count);

        var triangles = new List<int[]>();

        if (upperChain.Count >= 3)
        {
            for (int i = 0; i < upperChain.Count - 2; i++)
            {
                triangles.Add(new[] { upperChain[0], upperChain[i + 1], upperChain[i + 2] });
            }
        }

        if (lowerChain.Count >= 3)
        {
            for (int i = 0; i < lowerChain.Count - 2; i++)
            {
                triangles.Add(new[] { lowerChain[0], lowerChain[i + 1], lowerChain[i + 2] });
            }
        }

        if (log != null) log("  Monotone: " + triangles.Count + " triangles");
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

    private Point3D To2D(Point3D v, double nx, double ny, double nz)
    {
        if (Math.Abs(nz) >= Math.Abs(nx) && Math.Abs(nz) >= Math.Abs(ny))
            return new Point3D(v.X, v.Y, 0);
        else if (Math.Abs(ny) >= Math.Abs(nx) && Math.Abs(ny) >= Math.Abs(nz))
            return new Point3D(v.X, v.Z, 0);
        else
            return new Point3D(v.Y, v.Z, 0);
    }
}

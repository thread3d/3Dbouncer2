using System.Windows.Media;
using Color = System.Windows.Media.Color;
using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Alternating Opacity: renders adjacent faces with alternating opacity levels,
/// creating a checkerboard transparency effect that reveals depth structure.
/// </summary>
public class AlternatingOpacityRule : IFillRuleStrategy
{
    public string Name => "Alternating Opacity";
    public string Description => "Adjacent faces alternate opacity - reveals depth via transparency pattern";

    public List<int[]>? Triangulate(int[] sortedIndices, Point3D[] sorted3D, Point3D centroid,
        double nx, double ny, double nz, Action<string>? log = null) => null;

    public Model3DGroup RenderFaces(
        int[][] faces,
        Point3D[] vertices,
        int[][] edges,
        Point3D centroid,
        Point3D cameraPosition,
        Vector3D cameraLookDirection,
        double alpha,
        Func<Point3D, Point3D> transform,
        Action<string>? log = null)
    {
        var modelGroup = new Model3DGroup();

        var visited = new bool[faces.Length];

        for (int start = 0; start < faces.Length; start++)
        {
            if (visited[start]) continue;

            var stack = new Stack<int>();
            stack.Push(start);
            var parity = new int[faces.Length];
            var component = new List<int>();

            while (stack.Count > 0)
            {
                int fi = stack.Pop();
                if (visited[fi]) continue;
                visited[fi] = true;
                component.Add(fi);

                for (int other = 0; other < faces.Length; other++)
                {
                    if (visited[other]) continue;
                    if (SharesEdge(faces[fi], faces[other]))
                    {
                        parity[other] = 1 - parity[fi];
                        stack.Push(other);
                    }
                }
            }

            foreach (int fi in component)
            {
                if (faces[fi].Length < 3) continue;
                double opacity = parity[fi] == 0 ? alpha : alpha * 0.3;
                byte faceAlpha = (byte)(opacity * 255);

                var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(faceAlpha, 255, 255, 255)));
                var mesh = BuildFaceMesh(faces[fi], vertices, centroid, transform);
                var gm = new GeometryModel3D(mesh, material);
                gm.BackMaterial = material;
                modelGroup.Children.Add(gm);
            }
        }

        log?.Invoke($"  AltOpacity: {faces.Length} faces");
        return modelGroup;
    }

    private bool SharesEdge(int[] a, int[] b)
    {
        var setB = new HashSet<int>(b);
        int shared = 0;
        foreach (int v in a)
            if (setB.Contains(v)) shared++;
        return shared >= 2;
    }

    private Point3D ComputeFaceCentroid(int[] face, Point3D[] vertices)
    {
        double cx = 0, cy = 0, cz = 0;
        foreach (int idx in face) { var v = vertices[idx]; cx += v.X; cy += v.Y; cz += v.Z; }
        cx /= face.Length; cy /= face.Length; cz /= face.Length;
        return new Point3D(cx, cy, cz);
    }

    private MeshGeometry3D BuildFaceMesh(int[] face, Point3D[] vertices, Point3D centroid, Func<Point3D, Point3D> transform)
    {
        var mesh = new MeshGeometry3D();
        int n = face.Length;
        for (int i = 0; i < n; i++)
            mesh.Positions.Add(transform(vertices[face[i]]));

        if (n == 3)
        {
            mesh.TriangleIndices.Add(0); mesh.TriangleIndices.Add(1); mesh.TriangleIndices.Add(2);
        }
        else
        {
            int ci = mesh.Positions.Count;
            mesh.Positions.Add(transform(centroid));
            for (int i = 0; i < n; i++)
            {
                int baseIdx = mesh.Positions.Count;
                mesh.TriangleIndices.Add(ci);
                mesh.TriangleIndices.Add(baseIdx);
                mesh.TriangleIndices.Add(baseIdx + 1);
            }
        }
        return mesh;
    }
}

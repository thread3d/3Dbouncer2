using System.Windows.Media;
using Color = System.Windows.Media.Color;
using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Distance Fog rendering: faces farther from camera are more transparent,
/// creating a fog/distance-fade effect. Near faces are fully opaque.
/// </summary>
public class DistanceFogRule : IFillRuleStrategy
{
    public string Name => "Distance Fog";
    public string Description => "Far faces fade to transparent - distance fog effect, near faces opaque";

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

        double minDepth = double.MaxValue, maxDepth = double.MinValue;
        foreach (var f in faces)
        {
            if (f.Length < 3) continue;
            var fc = ComputeFaceCentroid(f, vertices);
            var tfc = transform(fc);
            double d = Math.Sqrt(Math.Pow(tfc.X - cameraPosition.X, 2) +
                                 Math.Pow(tfc.Y - cameraPosition.Y, 2) +
                                 Math.Pow(tfc.Z - cameraPosition.Z, 2));
            if (d < minDepth) minDepth = d;
            if (d > maxDepth) maxDepth = d;
        }

        double depthRange = maxDepth - minDepth;
        if (depthRange < 1e-6) depthRange = 1;

        foreach (var f in faces)
        {
            if (f.Length < 3) continue;

            var fc = ComputeFaceCentroid(f, vertices);
            var tfc = transform(fc);
            double d = Math.Sqrt(Math.Pow(tfc.X - cameraPosition.X, 2) +
                                 Math.Pow(tfc.Y - cameraPosition.Y, 2) +
                                 Math.Pow(tfc.Z - cameraPosition.Z, 2));

            double t = (d - minDepth) / depthRange;
            double fogAlpha = alpha * (1.0 - 0.7 * t);
            byte faceAlpha = (byte)(fogAlpha * 255);

            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(faceAlpha, 255, 255, 255)));
            var mesh = BuildFaceMesh(f, vertices, centroid, transform);
            var gm = new GeometryModel3D(mesh, material);
            gm.BackMaterial = material;
            modelGroup.Children.Add(gm);
        }

        log?.Invoke($"  DistanceFog: range={minDepth:F3} to {maxDepth:F3}");
        return modelGroup;
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

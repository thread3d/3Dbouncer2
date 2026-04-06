using System.Windows.Media;
using Color = System.Windows.Media.Color;
using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Spectral coloring: each face is colored differently based on its
/// centroid position mapped to a hue gradient, creating a rainbow spectrum effect.
/// All faces are fully opaque to show the coloring clearly.
/// </summary>
public class SpectralColoringRule : IFillRuleStrategy
{
    public string Name => "Spectral Coloring";
    public string Description => "Rainbow coloring by face centroid position - reveals face boundaries clearly";

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

        double minY = double.MaxValue, maxY = double.MinValue;
        double minZ = double.MaxValue, maxZ = double.MinValue;

        foreach (var f in faces)
        {
            if (f.Length < 3) continue;
            var fc = ComputeFaceCentroid(f, vertices);
            var tfc = transform(fc);
            if (tfc.Y < minY) minY = tfc.Y; if (tfc.Y > maxY) maxY = tfc.Y;
            if (tfc.Z < minZ) minZ = tfc.Z; if (tfc.Z > maxZ) maxZ = tfc.Z;
        }

        double rangeY = maxY - minY; if (rangeY < 1e-6) rangeY = 1;
        double rangeZ = maxZ - minZ; if (rangeZ < 1e-6) rangeZ = 1;

        foreach (var f in faces)
        {
            if (f.Length < 3) continue;

            var fc = ComputeFaceCentroid(f, vertices);
            var tfc = transform(fc);

            double hue = ((tfc.Y - minY) / rangeY + (tfc.Z - minZ) / rangeZ) / 2.0;
            hue = hue % 1.0;
            if (hue < 0) hue += 1.0;

            var color = HslToRgb(hue * 360, 0.8, 0.6);
            byte faceAlpha = (byte)(alpha * 255);

            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(faceAlpha, color.R, color.G, color.B)));
            var mesh = BuildFaceMesh(f, vertices, centroid, transform);
            var gm = new GeometryModel3D(mesh, material);
            gm.BackMaterial = material;
            modelGroup.Children.Add(gm);
        }

        log?.Invoke($"  Spectral: {faces.Length} faces");
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

    private System.Windows.Media.Color HslToRgb(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;
        double r1, g1, b1;
        if (h < 60) { r1 = c; g1 = x; b1 = 0; }
        else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
        else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
        else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
        else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
        else { r1 = c; g1 = 0; b1 = x; }
        return Color.FromRgb(
            (byte)Math.Clamp((r1 + m) * 255, 0, 255),
            (byte)Math.Clamp((g1 + m) * 255, 0, 255),
            (byte)Math.Clamp((b1 + m) * 255, 0, 255));
    }
}

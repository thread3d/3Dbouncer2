using System.Windows;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Painter's Algorithm: sorts faces back-to-front by depth from camera,
/// then renders each with a slightly decreasing alpha to create a depth fade effect.
/// </summary>
public class PainterBackToFrontRule : IFillRuleStrategy
{
    public string Name => "Painter Back-to-Front";
    public string Description => "Sorts faces by camera depth and renders back-to-front with alpha fade";

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

        // Sort faces by depth from camera (far to near)
        var faceDepths = faces.Select((f, i) => {
            var fc = ComputeFaceCentroid(f, vertices);
            var transformed = transform(fc);
            double dist = Math.Sqrt(
                Math.Pow(transformed.X - cameraPosition.X, 2) +
                Math.Pow(transformed.Y - cameraPosition.Y, 2) +
                Math.Pow(transformed.Z - cameraPosition.Z, 2));
            return (face: f, depth: dist, index: i);
        }).OrderByDescending(x => x.depth).ToArray();

        log?.Invoke($"  Painter: {faceDepths.Length} faces, depth range {faceDepths.Last().depth:F3} to {faceDepths.First().depth:F3}");

        for (int fi = 0; fi < faceDepths.Length; fi++)
        {
            var f = faceDepths[fi].face;
            if (f.Length < 3) continue;

            double t = fi / (double)faceDepths.Length;
            byte faceAlpha = (byte)(alpha * (0.3 + 0.7 * t) * 255);

            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(faceAlpha, 255, 255, 255)));
            var mesh = BuildFaceMesh(f, vertices, centroid, transform);
            var gm = new GeometryModel3D(mesh, material);
            gm.BackMaterial = material;
            modelGroup.Children.Add(gm);
        }

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

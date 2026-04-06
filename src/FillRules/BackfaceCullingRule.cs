using System.Windows.Media;
using Color = System.Windows.Media.Color;
using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Backface Culling: only renders faces whose normal points toward the camera.
/// For convex polyhedra viewed from outside, this eliminates all interior faces.
/// </summary>
public class BackfaceCullingRule : IFillRuleStrategy
{
    public string Name => "Backface Culling";
    public string Description => "Only shows outward-facing faces - correct for convex polyhedra viewed from outside";

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

        int shown = 0, culled = 0;
        foreach (var f in faces)
        {
            if (f.Length < 3) continue;

            var fc = ComputeFaceCentroid(f, vertices);
            var transformed = transform(fc);
            var normal = ComputeFaceNormal(f, vertices);

            double dx = cameraPosition.X - transformed.X;
            double dy = cameraPosition.Y - transformed.Y;
            double dz = cameraPosition.Z - transformed.Z;
            double camDist = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (camDist < 1e-10) { culled++; continue; }

            double dot = (normal.X * dx + normal.Y * dy + normal.Z * dz) / camDist;

            if (dot > 0)
            {
                shown++;
                byte faceAlpha = (byte)(alpha * 255);
                var material = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(faceAlpha, 255, 255, 255)));
                var mesh = BuildFaceMesh(f, vertices, centroid, transform);
                var gm = new GeometryModel3D(mesh, material);
                gm.BackMaterial = material;
                modelGroup.Children.Add(gm);
            }
            else
            {
                culled++;
            }
        }

        log?.Invoke($"  BackfaceCull: shown={shown}, culled={culled}");
        return modelGroup;
    }

    private Point3D ComputeFaceCentroid(int[] face, Point3D[] vertices)
    {
        double cx = 0, cy = 0, cz = 0;
        foreach (int idx in face) { var v = vertices[idx]; cx += v.X; cy += v.Y; cz += v.Z; }
        cx /= face.Length; cy /= face.Length; cz /= face.Length;
        return new Point3D(cx, cy, cz);
    }

    private Vector3D ComputeFaceNormal(int[] face, Point3D[] vertices)
    {
        var p0 = vertices[face[0]];
        var p1 = vertices[face[1]];
        var p2 = vertices[face[2]];
        double ax = p1.X - p0.X, ay = p1.Y - p0.Y, az = p1.Z - p0.Z;
        double bx = p2.X - p0.X, by = p2.Y - p0.Y, bz = p2.Z - p0.Z;
        double nx = ay * bz - az * by;
        double ny = az * bx - ax * bz;
        double nz = ax * by - ay * bx;
        double len = Math.Sqrt(nx * nx + ny * ny + nz * nz);
        if (len < 1e-10) return new Vector3D(0, 0, 1);
        return new Vector3D(nx / len, ny / len, nz / len);
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

using System.Windows.Media;
using Color = System.Windows.Media.Color;
using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Wireframe Overlay: renders all faces semi-transparent AND overlays
/// the wireframe edges in a contrasting color on top using a mesh-based approach.
/// </summary>
public class WireframeOverlayRule : IFillRuleStrategy
{
    public string Name => "Wireframe Overlay";
    public string Description => "Semi-transparent faces with bright wireframe edges overlaid on top";

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

        byte faceAlpha = (byte)(alpha * 120);
        var faceMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(faceAlpha, 200, 200, 255)));

        var combinedMesh = new MeshGeometry3D();
        foreach (var f in faces)
        {
            if (f.Length < 3) continue;
            int baseIdx = combinedMesh.Positions.Count;
            foreach (int idx in f)
                combinedMesh.Positions.Add(transform(vertices[idx]));

            if (f.Length == 3)
            {
                combinedMesh.TriangleIndices.Add(baseIdx);
                combinedMesh.TriangleIndices.Add(baseIdx + 1);
                combinedMesh.TriangleIndices.Add(baseIdx + 2);
            }
            else
            {
                var fc = ComputeFaceCentroid(f, vertices);
                int ci = combinedMesh.Positions.Count;
                combinedMesh.Positions.Add(transform(fc));
                for (int i = 0; i < f.Length; i++)
                {
                    combinedMesh.TriangleIndices.Add(ci);
                    combinedMesh.TriangleIndices.Add(baseIdx + i);
                    combinedMesh.TriangleIndices.Add(baseIdx + (i + 1) % f.Length);
                }
            }
        }

        var faceModel = new GeometryModel3D(combinedMesh, faceMaterial);
        faceModel.BackMaterial = faceMaterial;
        modelGroup.Children.Add(faceModel);

        // Add wireframe edges as a separate mesh with thicker lines
        var wireMesh = new MeshGeometry3D();
        foreach (var edge in edges)
        {
            if (edge.Length < 2) continue;
            int baseIdx = wireMesh.Positions.Count;
            wireMesh.Positions.Add(transform(vertices[edge[0]]));
            wireMesh.Positions.Add(transform(vertices[edge[1]]));
            // Each edge is a line: use a degenerate triangle (all indices the same)
            wireMesh.TriangleIndices.Add(baseIdx);
            wireMesh.TriangleIndices.Add(baseIdx + 1);
            wireMesh.TriangleIndices.Add(baseIdx); // degenerate
        }
        var wireMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)));
        var wireModel = new GeometryModel3D(wireMesh, wireMaterial);
        modelGroup.Children.Add(wireModel);

        log?.Invoke("  WireOverlay: " + faces.Length + " faces, " + edges.Length + " edges");
        return modelGroup;
    }

    private Point3D ComputeFaceCentroid(int[] face, Point3D[] vertices)
    {
        double cx = 0, cy = 0, cz = 0;
        foreach (int idx in face) { var v = vertices[idx]; cx += v.X; cy += v.Y; cz += v.Z; }
        cx /= face.Length; cy /= face.Length; cz /= face.Length;
        return new Point3D(cx, cy, cz);
    }
}

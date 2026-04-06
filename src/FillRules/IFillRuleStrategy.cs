using System.Windows.Media.Media3D;

namespace TextBouncer.FillRules;

/// <summary>
/// Unified strategy interface for face rendering.
/// Supports two modes:
/// - 2D Triangulation: Triangulate() returns triangle indices, caller builds the 3D mesh
/// - 3D Rendering: RenderFaces() directly returns a Model3DGroup with full 3D rendering
/// </summary>
public interface IFillRuleStrategy
{
    string Name { get; }
    string Description { get; }

    /// <summary>
    /// 2D triangulation: returns triangles as triplets of vertex indices into sortedIndices.
    /// All indices are in range [0..n-1] where n = sortedIndices.Length,
    /// or [n] as a sentinel for the centroid.
    /// </summary>
    List<int[]>? Triangulate(int[] sortedIndices, Point3D[] sorted3D, Point3D centroid,
        double nx, double ny, double nz, Action<string>? log = null);

    /// <summary>
    /// 3D rendering: directly builds and returns a Model3DGroup for the given faces.
    /// If null, the Triangulate() method is used instead.
    /// </summary>
    Model3DGroup? RenderFaces(
        int[][] faces,
        Point3D[] vertices,
        int[][] edges,
        Point3D centroid,
        Point3D cameraPosition,
        Vector3D cameraLookDirection,
        double alpha,
        Func<Point3D, Point3D> transform,
        Action<string>? log = null);
}

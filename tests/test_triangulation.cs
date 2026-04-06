using System;
using System.Windows.Media.Media3D;

// Test the triangulation
class TestTriangulation
{
    static void Main()
    {
        Console.WriteLine("Testing ear clipping triangulation...\n");

        // Test 1: Square (4-gon)
        Console.WriteLine("=== Test 1: Square ===");
        var square = new[]
        {
            new Point3D(0, 0, 0),
            new Point3D(1, 0, 0),
            new Point3D(1, 1, 0),
            new Point3D(0, 1, 0)
        };
        TestPolygon(square);

        // Test 2: Pentagon (5-gon)
        Console.WriteLine("\n=== Test 2: Pentagon ===");
        var pentagon = new[]
        {
            new Point3D(0, 0, 0),
            new Point3D(1, 0, 0),
            new Point3D(1.5, 0.5, 0),
            new Point3D(1, 1, 0),
            new Point3D(0, 1, 0)
        };
        TestPolygon(pentagon);

        // Test 3: Triangle (3-gon) - should work with fan
        Console.WriteLine("\n=== Test 3: Triangle ===");
        var triangle = new[]
        {
            new Point3D(0, 0, 0),
            new Point3D(1, 0, 0),
            new Point3D(0.5, 1, 0)
        };
        TestPolygon(triangle);

        // Test 4: Non-convex (L-shape via 5-gon)
        Console.WriteLine("\n=== Test 4: L-shape (non-convex pentagon) ===");
        var lShape = new[]
        {
            new Point3D(0, 0, 0),
            new Point3D(2, 0, 0),
            new Point3D(2, 1, 0),
            new Point3D(1, 1, 0),
            new Point3D(1, 2, 0),
            new Point3D(0, 2, 0)
        };
        TestPolygon(lShape);

        Console.WriteLine("\n=== All tests complete ===");
    }

    static void TestPolygon(Point3D[] vertices)
    {
        int n = vertices.Length;
        Console.WriteLine($"Polygon has {n} vertices");

        // Sort by angle around centroid (simple version for flat polygon on Z=0)
        var centroid = new Point3D(0, 0, 0);
        foreach (var v in vertices) { centroid.X += v.X; centroid.Y += v.Y; centroid.Z += v.Z; }
        centroid.X /= n; centroid.Y /= n; centroid.Z /= n;

        var sorted = new List<Point3D>();
        var angles = new List<double>();
        for (int i = 0; i < n; i++)
        {
            double angle = Math.Atan2(vertices[i].Y - centroid.Y, vertices[i].X - centroid.X);
            angles.Add(angle);
        }
        var indices = angles.Select((a, i) => new { a, i }).OrderBy(x => x.a).Select(x => x.i).ToList();

        Console.WriteLine($"Sorted indices: [{string.Join(",", indices)}]");

        // Build sorted array
        var sorted3D = indices.Select(i => vertices[i]).ToArray();

        // Triangulate
        var triangles = EarClipTriangulate(sorted3D);

        Console.WriteLine($"Triangulation produces {triangles.Count} triangles:");
        foreach (var tri in triangles)
        {
            Console.WriteLine($"  Triangle: ({string.Join(",", tri)}) -> " +
                $"({sorted3D[tri[0]].X:F2},{sorted3D[tri[0]].Y:F2}) " +
                $"({sorted3D[tri[1]].X:F2},{sorted3D[tri[1]].Y:F2}) " +
                $"({sorted3D[tri[2]].X:F2},{sorted3D[tri[2]].Y:F2})");
        }
    }

    static List<int[]> EarClipTriangulate(Point3D[] pts)
    {
        var indices = Enumerable.Range(0, pts.Length).ToList();
        var triangles = new List<int[]>();

        while (indices.Count > 3)
        {
            bool earFound = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                if (IsEar(indices, prev, curr, next, pts))
                {
                    triangles.Add(new[] { prev, curr, next });
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            if (!earFound)
            {
                Console.WriteLine("  WARNING: No ear found! Falling back to fan.");
                for (int i = 0; i < indices.Count; i++)
                {
                    triangles.Add(new[] { indices.Count, indices[i], indices[(i + 1) % indices.Count] });
                }
                break;
            }
        }

        if (indices.Count == 3)
        {
            triangles.Add(new[] { indices[0], indices[1], indices[2] });
        }

        return triangles;
    }

    static bool IsEar(List<int> indices, int prev, int curr, int next, Point3D[] pts)
    {
        var a = pts[prev];
        var b = pts[curr];
        var c = pts[next];

        // Check if convex (counter-clockwise)
        double cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        if (cross <= 0) return false;

        // Check if any vertex is inside
        foreach (int idx in indices)
        {
            if (idx == prev || idx == curr || idx == next) continue;
            if (PointInTriangle(a, b, c, pts[idx])) return false;
        }

        return true;
    }

    static bool PointInTriangle(Point3D a, Point3D b, Point3D c, Point3D p)
    {
        double det = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        if (Math.Abs(det) < 1e-10) return false;

        double u = ((b.Y - a.Y) * (p.X - a.X) - (b.X - a.X) * (p.Y - a.Y)) / det;
        double v = ((c.Y - a.Y) * (p.X - a.X) - (c.X - a.X) * (p.Y - a.Y)) / det;

        return u >= 0 && v >= 0 && u + v <= 1;
    }
}
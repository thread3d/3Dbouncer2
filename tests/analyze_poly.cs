// Standalone analysis of all 48 polyhedra face types
// Run with: dotnet script analyze_poly.cs
#r "src/bin/Debug/net8.0-windows/TextBouncer.dll"

using System;
using System.Windows.Media.Media3D;
using TextBouncer;

Console.WriteLine("// ============================================================");
Console.WriteLine("// Polyhedron Face Statistics");
Console.WriteLine("// ============================================================");
Console.WriteLine("// Index | Name                          | Total | Triangles | Quads | Pentagons | Hexagons | Other");
Console.WriteLine("// ------+-------------------------------+-------+-----------+-------+-----------+---------+-------");

for (int i = 0; i < 48; i++)
{
    var data = PolyhedronLibrary.GetPolyhedron(i);
    int triangles = 0, quads = 0, pentagons = 0, hexagons = 0, other = 0;
    foreach (var face in data.Faces)
    {
        switch (face.Length)
        {
            case 3: triangles++; break;
            case 4: quads++; break;
            case 5: pentagons++; break;
            case 6: hexagons++; break;
            default: other++; break;
        }
    }
    int total = data.Faces.Length;
    Console.WriteLine($"// {i,4} | {data.Name,-30} | {total,5} | {triangles,9} | {quads,5} | {pentagons,9} | {hexagons,7} | {other,5}");
    Console.WriteLine($"    ({i}, \"{data.Name}\", {triangles}, {quads}, {pentagons}, {hexagons}, {other}),");
}

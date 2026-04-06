using System;
using System.Windows.Media.Media3D;
class Test {
    static void Main() {
        var pd = PolyhedronLibrary.GetPolyhedron(1);
        Console.WriteLine($"Cube: {pd.Faces.Length} faces");
        foreach (var f in pd.Faces) Console.WriteLine($"  {f.Length}-gon: [{string.Join(",",f)}]");
    }
}

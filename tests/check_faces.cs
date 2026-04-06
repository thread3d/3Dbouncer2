using System;
using System.Linq;

class CheckFaces {
    static void Main() {
        var data = new (string name, int verts, int edges, int faces, string faceTypes)[]
        {
            // Platonic + Archimedean + Johnson from library
            ("Tetrahedron", 4, 6, 4, "4Δ"),
            ("Cube", 8, 12, 6, "6□"),
            ("Octahedron", 6, 12, 8, "8Δ"),
            ("Dodecahedron", 20, 30, 12, "12Pent"),
            ("Icosahedron", 12, 30, 20, "20Δ"),
            ("Truncated Tetrahedron", 12, 18, 8, "4Δ+4Hex"),
            ("Cuboctahedron", 12, 24, 14, "8Δ+6□"),
            ("Truncated Octahedron", 24, 36, 14, "6□+8Hex"),
            ("Truncated Cube", 24, 36, 14, "8Δ+6Oct"),
            ("Icosidodecahedron", 30, 60, 32, "20Δ+12Pent"),
            ("Truncated Dodecahedron", 60, 90, 32, "20Δ+12Dec"),
            ("Truncated Icosahedron", 60, 90, 32, "12Pent+20Hex"),
            ("Rhombicuboctahedron", 24, 48, 26, "8Δ+18□"),
            ("Truncated Cuboctahedron", 48, 72, 26, "12□+8Hex+6Oct"),
            ("Rhombicosidodecahedron", 60, 120, 62, "20Δ+30□+12Pent"),
            ("Snub Cube", 24, 60, 38, "32Δ+6□"),
            ("Triangular Prism", 6, 9, 5, "2Δ+3□"),
            ("Pentagonal Prism", 10, 15, 7, "2Pent+5□"),
            ("Hexagonal Prism", 12, 18, 8, "2Hex+6□"),
            ("Square Antiprism", 8, 16, 10, "8Δ+2□"),
            ("Pentagonal Antiprism", 10, 20, 12, "10Δ+2Pent"),
            ("Hexagonal Antiprism", 12, 24, 14, "12Δ+2Hex"),
            ("Square Pyramid (J1)", 5, 8, 5, "4Δ+□"),
            ("Pentagonal Pyramid (J2)", 6, 10, 6, "5Δ+Pent"),
            ("Triangular Cupola (J3)", 9, 15, 8, "1Δ+3□+3Δ"),
            ("Square Cupola (J4)", 12, 20, 9, "1□+4Δ+4□"),
            ("Pentagonal Cupola (J5)", 15, 25, 10, "1Pent+5Δ+5□"),
            ("Pentagonal Rotunda (J6)", 20, 35, 13, "1Pent+5Δ+5Pent"),
            ("Elongated Triangular Pyramid (J7)", 7, 12, 7, "4Δ+3□"),
            ("Elongated Square Pyramid (J8)", 9, 16, 9, "4Δ+5□"),
            ("Elongated Pentagonal Pyramid (J9)", 11, 20, 11, "5Δ+6□"),
            ("Gyroelongated Square Pyramid (J10)", 9, 20, 12, "8Δ+4□"),
            ("Gyroelongated Pentagonal Pyramid (J11)", 11, 25, 14, "10Δ+5□"),
            ("Triangular Dipyramid (J12)", 5, 9, 6, "6Δ"),
            ("Pentagonal Dipyramid (J13)", 7, 15, 10, "10Δ"),
            ("Elongated Triangular Dipyramid (J14)", 8, 15, 9, "6Δ+3□"),
            ("Elongated Square Dipyramid (J15)", 10, 20, 12, "8Δ+4□"),
            ("Elongated Pentagonal Dipyramid (J16)", 12, 25, 15, "10Δ+5□"),
            ("Gyroelongated Square Dipyramid (J17)", 10, 24, 16, "8Δ+8Δ"),
            ("Elongated Triangular Cupola (J18)", 15, 27, 11, "4Δ+3□+4□"),
            ("Elongated Square Cupola (J19)", 20, 36, 13, "4□+3Δ+6□"),
            ("Elongated Pentagonal Cupola (J20)", 25, 45, 16, "5Pent+5Δ+5□+□"),
            ("Elongated Pentagonal Rotunda (J21)", 30, 55, 19, "1Pent+5Δ+5Pent+5Δ+3Pent"),
            ("Gyroelongated Triangular Cupola (J22)", 15, 33, 14, "8Δ+3□+3Δ"),
            ("Gyroelongated Square Cupola (J23)", 20, 44, 18, "8Δ+5□+4□"),
            ("Gyroelongated Pentagonal Cupola (J24)", 25, 55, 22, "10Δ+5Pent+5□+2□"),
            ("Gyroelongated Pentagonal Rotunda (J25)", 30, 65, 27, "10Δ+5Pent+5Δ+5Pent+2Pent"),
            ("Gyrobifastigium (J26)", 8, 14, 8, "4□+4Δ"),
        };
        
        Console.WriteLine("Index | Name                              | V  | E  | F  | Face Types");
        Console.WriteLine("------+-----------------------------------+----+----+----+-----------------");
        for (int i = 0; i < data.Length; i++) {
            var d = data[i];
            Console.WriteLine($"{i,5} | {d.name,-33} | {d.verts,2} | {d.edges,2} | {d.faces,2} | {d.faceTypes}");
        }
    }
}

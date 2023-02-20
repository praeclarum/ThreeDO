using System;
using System.Collections.ObjectModel;
using System.Numerics;

namespace ThreeDO;

/* Example 3DO File:

3DO 1.30
# Textured Lambda Shuttle v. 1.2a
# Full Shuttle (with landing gear)
# by Matt Hallaron (mhallaron@yahoo.com)
# May/Sept 2001
#
3DONAME SHUTTLE
OBJECTS 00004
VERTICES 00086
POLYGONS 00061
PALETTE DEFAULT.PAL


 TEXTURES 2
     TEXTURE:      SHUBODY.BM     #0
     TEXTURE:      SHUWING.BM     #1 
#------------------------------------------------------------------------
OBJECT "BODY"
TEXTURE 0

VERTICES 46				

*/

public class ThreeDObject
{
    public string Name { get; set; }
    public ObservableCollection<ThreeDSubobject> Objects { get; set; } = new ();
    public int ObjectCount => Objects.Count;
    public int VertexCount => Objects.Sum(o => o.Vertices.Length);
    public int PolygonCount => Objects.Sum(o => o.PolygonCount);

    public static Task<ThreeDObject> LoadFromFile(string filePath)
    {
        throw new NotImplementedException();
    }

    public void ExportDae(TextWriter writer)
    {
        throw new NotImplementedException();
    }
}

public struct Quad
{
    public int A;
    public int B;
    public int C;
    public int D;
}

public class ThreeDSubobject
{
    public Vector3[] Vertices { get; set; } = Array.Empty<Vector3>();
    public Quad[] Quads { get; set; } = Array.Empty<Quad>();
    public int PolygonCount => Quads.Length;
}

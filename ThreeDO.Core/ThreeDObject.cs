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
    public string Name { get; set; } = "";
    public string Palette { get; set; } = "";
    public List<ThreeDSubobject> Objects { get; set; } = new ();
    public int ObjectCount => Objects.Count;
    public int VertexCount => Objects.Sum(o => o.Vertices.Length);
    public int PolygonCount => Objects.Sum(o => o.PolygonCount);

    public List<string> Textures { get; set; } = new ();

    public static Task<ThreeDObject> LoadFromFile(string filePath)
    {
        return Task.Run(() =>
        {
            using var reader = new StreamReader(filePath);
            return Read(reader);
        });
    }

    public static ThreeDObject Read(TextReader reader)
    {
        string? ReadValidLine()
        {
            for (; ; )
            {
                var line = reader.ReadLine();
                if (line is not string)
                    return null;
                var c = line.IndexOf('#');
                if (c == 0)
                    continue;
                if (c > 0)
                    line = line.Substring(0, c);
                line = line.Trim();
                if (line.Length == 0)
                    continue;
                return line;
            }
        }
        static string[] SplitLine(string line) => line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var obj = new ThreeDObject();
        var sobj = default(ThreeDSubobject);
        var state = ParseState.InHead;
        for (var line = ReadValidLine(); line != null; line = ReadValidLine())
        {
            switch (state)
            {
                case ParseState.InHead:
                    if (line.StartsWith("OBJECT "))
                    {
                        sobj = new ThreeDSubobject { Name = line.Substring(6).Trim().Replace("\"", ""), };
                        obj.Objects.Add(sobj);
                        state = ParseState.InObj;
                    }
                    else if (line.StartsWith("TEXTURES "))
                    {
                        state = ParseState.InTextures;
                    }
                    else if (line.StartsWith("3DONAME "))
                    {
                        obj.Name = SplitLine(line)[1];
                    }
                    else if (line.StartsWith("3DO "))
                    {
                    }
                    else if (line.StartsWith("OBJECTS "))
                    {
                    }
                    else if (line.StartsWith("VERTICES "))
                    {
                    }
                    else if (line.StartsWith("POLYGONS "))
                    {
                    }
                    else if (line.StartsWith("PALETTE "))
                    {
                        obj.Palette = SplitLine(line)[1];
                    }
                    else
                    {
                        throw new Exception($"Unexpected 3DO line: \"{line}\" {state}");
                    }
                    break;
                case ParseState.InTextures:
                    if (line.StartsWith("OBJECT "))
                    {
                        sobj = new ThreeDSubobject { Name = line.Substring(6).Trim().Replace("\"", ""), };
                        obj.Objects.Add(sobj);
                        state = ParseState.InObj;
                    }
                    else if (line.StartsWith("TEXTURE:"))
                    {
                        obj.Textures.Add(SplitLine(line)[1]);
                    }
                    else
                    {
                        throw new Exception($"Unexpected 3DO line: \"{line}\" {state}");
                    }

                    break;
                case ParseState.InObj:
                    throw new NotImplementedException();
                case ParseState.InVerts:
                    throw new NotImplementedException();
            }
            Console.WriteLine(line);
        }
        return obj;
    }

    enum ParseState
    {
        InHead,
        InTextures,
        InObj,
        InVerts
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
    public string Name { get; set; } = "";
    public Vector3[] Vertices { get; set; } = Array.Empty<Vector3>();
    public Quad[] Quads { get; set; } = Array.Empty<Quad>();
    public int PolygonCount => Quads.Length;
}

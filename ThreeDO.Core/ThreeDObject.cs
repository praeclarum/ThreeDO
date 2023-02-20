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
    public int VertexCount => Objects.Sum(o => o.Vertices.Count);
    public int PolygonCount => Objects.Sum(o => o.PolygonCount);

    public List<string> Textures { get; set; } = new ();

    public override string ToString() => Name;

    public static Task<ThreeDObject> LoadFromFile(string filePath)
    {
        return Task.Run(() =>
        {
            using var reader = new StreamReader(filePath);
            return Read(reader);
        });
    }

    static readonly char[] WS = new[] { ' ', '\t' };

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
        static string[] SplitLine(string line) => line.Split(WS, StringSplitOptions.RemoveEmptyEntries);
        var obj = new ThreeDObject();
        var sobj = default(ThreeDSubobject);
        var state = ParseState.InHead;
        for (var line = ReadValidLine(); line != null; line = ReadValidLine())
        {
            if (line.StartsWith("OBJECT") && !line.StartsWith("OBJECTS"))
            {
                sobj = new ThreeDSubobject { Name = line.Substring(6).Trim().Replace("\"", ""), };
                obj.Objects.Add(sobj);
                state = ParseState.InObj;
            }
            else
            {
                switch (state)
                {
                    case ParseState.InHead:
                        if (line.StartsWith("TEXTURES"))
                        {
                            state = ParseState.InTextures;
                        }
                        else if (line.StartsWith("3DONAME"))
                        {
                            obj.Name = SplitLine(line)[1];
                        }
                        else if (line.StartsWith("3DO"))
                        {
                        }
                        else if (line.StartsWith("OBJECTS"))
                        {
                        }
                        else if (line.StartsWith("VERTICES"))
                        {
                        }
                        else if (line.StartsWith("POLYGONS"))
                        {
                        }
                        else if (line.StartsWith("PALETTE"))
                        {
                            obj.Palette = SplitLine(line)[1];
                        }
                        else
                        {
                            throw new Exception($"Unexpected 3DO line: \"{line}\" {state}");
                        }
                        break;
                    case ParseState.InTextures:
                        if (line.StartsWith("TEXTURE:"))
                        {
                            obj.Textures.Add(SplitLine(line)[1]);
                        }
                        else
                        {
                            throw new Exception($"Unexpected 3DO line: \"{line}\" {state}");
                        }
                        break;
                    case ParseState.InObj:
                        if (line.StartsWith("TEXTURE"))
                        {
                            if (sobj is { } o)
                            {
                                o.TextureIndex = int.Parse(SplitLine(line)[1]);
                            }
                        }
                        else if (line.StartsWith("VERTICES"))
                        {
                            state = ParseState.InVertices;
                        }
                        else
                        {
                            throw new Exception($"Unexpected 3DO line: \"{line}\" {state}");
                        }
                        break;
                    case ParseState.InVertices:
                        if (line.StartsWith("QUADS"))
                        {
                            state = ParseState.InQuads;
                        }
                        else
                        {
                            var parts = SplitLine(line);
                            var v = new Vector3(float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                            if (sobj is { } o)
                            {
                                o.Vertices.Add(v);
                            }
                        }
                        break;
                    case ParseState.InQuads:
                        if (line.StartsWith("TEXTURE VERTICES"))
                        {
                            state = ParseState.InTextureVertices;
                        }
                        else
                        {
                            var parts = SplitLine(line);
                            var fill = parts[6] switch
                            {
                                "GOURTEX" => PolygonFill.GourTex,
                                var x => throw new NotSupportedException($"Unknown fill {x}"),
                            };
                            var q = new Quad
                            {
                                A = int.Parse(parts[1]),
                                B = int.Parse(parts[2]),
                                C = int.Parse(parts[3]),
                                D = int.Parse(parts[4]),
                                Color = int.Parse(parts[5]),
                                Fill = fill
                            };
                            if (sobj is { } o)
                            {
                                o.Quads.Add(q);
                            }
                        }
                        break;
                    case ParseState.InTextureVertices:
                        if (line.StartsWith("TEXTURE QUADS"))
                        {
                            state = ParseState.InTextureQuads;
                        }
                        else
                        {
                            var parts = SplitLine(line);
                            var v = new Vector2(float.Parse(parts[1]), float.Parse(parts[2]));
                            if (sobj is { } o)
                            {
                                o.TextureVertices.Add(v);
                            }
                        }
                        break;
                    case ParseState.InTextureQuads:
                        {
                            var parts = SplitLine(line);
                            var qIndex = int.Parse(parts[0].Substring(0, parts[0].Length - 1));
                            if (sobj is { } o && 0 <= qIndex && qIndex < o.Quads.Count)
                            {
                                var q = o.Quads[qIndex];
                                q.TA = int.Parse(parts[1]);
                                q.TB = int.Parse(parts[2]);
                                q.TC = int.Parse(parts[3]);
                                q.TD = int.Parse(parts[4]);
                            }
                        }
                        break;
                }
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
        InVertices,
        InQuads,
        InTextureVertices,
        InTextureQuads,
    }

    public void ExportDae(TextWriter writer)
    {
        throw new NotImplementedException();
    }
}

public struct Quad
{
    public int A, TA;
    public int B, TB;
    public int C, TC;
    public int D, TD;
    public int Color;
    public PolygonFill Fill;
}

public enum PolygonFill
{
    GourTex,
}

public class ThreeDSubobject
{
    public string Name { get; set; } = "";
    public int TextureIndex { get; set; } = -1;
    public bool HasTexture => TextureIndex >= 0;
    public List<Vector3> Vertices { get; set; } = new();
    public List<Vector2> TextureVertices { get; set; } = new();
    public List<Quad> Quads { get; set; } = new();
    public int PolygonCount => Quads.Count;
    public override string ToString() => Name;
}

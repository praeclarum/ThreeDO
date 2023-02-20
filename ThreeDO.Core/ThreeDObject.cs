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
                                o.Quads[qIndex] = q;
                            }
                        }
                        break;
                }
            }
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
        var createdTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

        writer.WriteLine($"<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        writer.WriteLine($"<COLLADA xmlns=\"http://www.collada.org/2005/11/COLLADASchema\" version=\"1.4.1\">");
        writer.WriteLine($"  <asset>");
        writer.WriteLine($"    <contributor>");
        writer.WriteLine($"      <authoring_tool>3DO2DAE</authoring_tool>");
        writer.WriteLine($"    </contributor>");
        writer.WriteLine($"    <created>{createdTime}</created>");
        writer.WriteLine($"    <modified>{createdTime}</modified>");
        writer.WriteLine($"    <unit name=\"meter\" meter=\"1\"/>");
        writer.WriteLine($"    <up_axis>Y_UP</up_axis>");
        writer.WriteLine($"  </asset>");
        writer.WriteLine($"  <library_images>");
        for (var i = 0; i < Textures.Count; i++)
        {
            writer.WriteLine($"    <image id=\"image{i}\" name=\"{Textures[i]}\">");
            writer.WriteLine($"      <init_from>{Textures[i]}</init_from>");
            writer.WriteLine($"    </image>");
        }
        writer.WriteLine($"  </library_images>");
        writer.WriteLine($"  <library_effects>");
        for (var i = 0; i < Textures.Count; i++)
        {
            writer.WriteLine($"    <effect id=\"effect{i}\">");
            writer.WriteLine($"      <profile_COMMON>");
            writer.WriteLine($"        <newparam sid=\"surface{i}\">");
            writer.WriteLine($"          <surface type=\"2D\">");
            writer.WriteLine($"            <init_from>image{i}</init_from>");
            writer.WriteLine($"          </surface>");
            writer.WriteLine($"        </newparam>");
            writer.WriteLine($"        <newparam sid=\"sampler{i}\">");
            writer.WriteLine($"          <sampler2D>");
            writer.WriteLine($"            <source>surface{i}</source>");
            writer.WriteLine($"          </sampler2D>");
            writer.WriteLine($"        </newparam>");
            writer.WriteLine($"        <technique sid=\"common\">");
            writer.WriteLine($"          <phong>");
            writer.WriteLine($"            <diffuse>");
            writer.WriteLine($"              <texture texture=\"sampler{i}\" texcoord=\"UVMap\"/>");
            writer.WriteLine($"            </diffuse>");
            writer.WriteLine($"          </phong>");
            writer.WriteLine($"        </technique>");
            writer.WriteLine($"      </profile_COMMON>");
            writer.WriteLine($"    </effect>");
        }
        writer.WriteLine($"  </library_effects>");
        writer.WriteLine($"  <library_materials>");
        for (var i = 0; i < Textures.Count; i++)
        {
            writer.WriteLine($"    <material id=\"material{i}\" name=\"{Textures[i]}\">");
            writer.WriteLine($"      <instance_effect url=\"#effect{i}\"/>");
            writer.WriteLine($"    </material>");
        }
        writer.WriteLine($"  </library_materials>");
        writer.WriteLine($"  <library_geometries>");
        for (var i = 0; i < Objects.Count; i++)
        {
            var subobj = Objects[i];
            writer.WriteLine($"    <geometry id=\"geometry{i}\" name=\"{subobj.Name}\">");
            writer.WriteLine($"      <mesh>");
            writer.WriteLine($"        <source id=\"geometry{i}-positions\">");
            writer.WriteLine($"          <float_array id=\"geometry{i}-positions-array\" count=\"{subobj.Vertices.Count * 3}\">");
            foreach (var v in subobj.Vertices)
            {
                writer.WriteLine($"            {v.X} {-v.Y} {v.Z}");
            }
            writer.WriteLine($"          </float_array>");
            writer.WriteLine($"          <technique_common>");
            writer.WriteLine($"            <accessor source=\"#geometry{i}-positions-array\" count=\"{subobj.Vertices.Count}\" stride=\"3\">");
            writer.WriteLine($"              <param name=\"X\" type=\"float\"/>");
            writer.WriteLine($"              <param name=\"Y\" type=\"float\"/>");
            writer.WriteLine($"              <param name=\"Z\" type=\"float\"/>");
            writer.WriteLine($"            </accessor>");
            writer.WriteLine($"          </technique_common>");
            writer.WriteLine($"        </source>");
            if (subobj.HasTexture)
            {
                writer.WriteLine($"        <source id=\"geometry{i}-map-0\">");
                writer.WriteLine($"          <float_array id=\"geometry{i}-map-0-array\" count=\"{subobj.TextureVertices.Count * 2}\">");
                foreach (var v in subobj.TextureVertices)
                {
                    writer.WriteLine($"            {v.X} {v.Y}");
                }
                writer.WriteLine($"          </float_array>");
                writer.WriteLine($"          <technique_common>");
                writer.WriteLine($"            <accessor source=\"#geometry{i}-map-0-array\" count=\"{subobj.TextureVertices.Count}\" stride=\"2\">");
                writer.WriteLine($"              <param name=\"S\" type=\"float\"/>");
                writer.WriteLine($"              <param name=\"T\" type=\"float\"/>");
                writer.WriteLine($"            </accessor>");
                writer.WriteLine($"          </technique_common>");
                writer.WriteLine($"        </source>");
            }
            writer.WriteLine($"        <vertices id=\"geometry{i}-vertices\">");
            writer.WriteLine($"          <input semantic=\"POSITION\" source=\"#geometry{i}-positions\"/>");
            writer.WriteLine($"        </vertices>");
            writer.WriteLine($"        <polylist count=\"{subobj.PolygonCount}\">");
            writer.WriteLine($"          <input semantic=\"VERTEX\" source=\"#geometry{i}-vertices\" offset=\"0\"/>");
            if (subobj.HasTexture)
            {
                writer.WriteLine($"          <input semantic=\"TEXCOORD\" source=\"#geometry{i}-map-0\" offset=\"1\" set=\"0\"/>");
            }
            writer.WriteLine($"          <vcount>");
            for (var j = 0; j < subobj.PolygonCount; j++)
            {
                writer.WriteLine($"            4");
            }
            writer.WriteLine($"          </vcount>");
            writer.WriteLine($"          <p>");
            foreach (var q in subobj.Quads)
            {
                writer.WriteLine($"            {q.A} {q.TA} {q.B} {q.TB} {q.C} {q.TC} {q.D} {q.TD}");
            }
            writer.WriteLine($"          </p>");
            writer.WriteLine($"        </polylist>");
            writer.WriteLine($"      </mesh>");
            writer.WriteLine($"    </geometry>");
        }
        writer.WriteLine($"  </library_geometries>");
        writer.WriteLine($"  <library_visual_scenes>");
        writer.WriteLine($"    <visual_scene id=\"Scene\" name=\"Scene\">");
        for (var i = 0; i < Objects.Count; i++)
        {
            var subobj = Objects[i];
            writer.WriteLine($"      <node id=\"node{i}\" name=\"{subobj.Name}\">");
            writer.WriteLine($"        <instance_geometry url=\"#geometry{i}\">");
            if (subobj.HasTexture)
            {
                writer.WriteLine($"          <bind_material>");
                writer.WriteLine($"            <technique_common>");
                writer.WriteLine($"              <instance_material symbol=\"material{i}\" target=\"#material{i}\"/>");
                writer.WriteLine($"            </technique_common>");
                writer.WriteLine($"          </bind_material>");
            }
            writer.WriteLine($"        </instance_geometry>");
            writer.WriteLine($"      </node>");
        }
        writer.WriteLine($"    </visual_scene>");
        writer.WriteLine($"  </library_visual_scenes>");
        writer.WriteLine($"  <scene>");
        writer.WriteLine($"    <instance_visual_scene url=\"#Scene\"/>");
        writer.WriteLine($"  </scene>");
        writer.WriteLine($"</COLLADA>");
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

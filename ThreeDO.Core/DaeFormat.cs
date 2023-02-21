using System;
using System.Globalization;

namespace ThreeDO
{
    public static class DaeFormat
    {
        public static void ExportDae(this ThreeDObject obj, TextWriter writer)
        {
            var createdTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var icult = CultureInfo.InvariantCulture;

            // DAE works best when the vertices do not share normals
            obj = obj.UnshareVertices();

            writer.WriteLine($"<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            writer.WriteLine($"<COLLADA xmlns=\"http://www.collada.org/2005/11/COLLADASchema\" version=\"1.4.1\">");
            writer.WriteLine($"  <asset>");
            writer.WriteLine($"    <contributor>");
            writer.WriteLine($"      <authoring_tool>ThreeDO</authoring_tool>");
            writer.WriteLine($"    </contributor>");
            writer.WriteLine($"    <created>{createdTime}</created>");
            writer.WriteLine($"    <modified>{createdTime}</modified>");
            writer.WriteLine($"    <unit name=\"meter\" meter=\"1\"/>");
            writer.WriteLine($"    <up_axis>Y_UP</up_axis>");
            writer.WriteLine($"  </asset>");
            writer.WriteLine($"  <library_images>");
            for (var i = 0; i < obj.Textures.Count; i++)
            {
                writer.WriteLine($"    <image id=\"image{i}\" name=\"{obj.Textures[i]}\">");
                writer.WriteLine($"      <init_from>{obj.Textures[i]}</init_from>");
                writer.WriteLine($"    </image>");
            }
            writer.WriteLine($"  </library_images>");
            writer.WriteLine($"  <library_effects>");
            for (var i = 0; i < obj.Textures.Count; i++)
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
            for (var i = 0; i < obj.Textures.Count; i++)
            {
                writer.WriteLine($"    <material id=\"material{i}\" name=\"{obj.Textures[i]}\">");
                writer.WriteLine($"      <instance_effect url=\"#effect{i}\"/>");
                writer.WriteLine($"    </material>");
            }
            writer.WriteLine($"  </library_materials>");
            writer.WriteLine($"  <library_geometries>");
            for (var i = 0; i < obj.Objects.Count; i++)
            {
                var subobj = obj.Objects[i];
                writer.WriteLine($"    <geometry id=\"geometry{i}\" name=\"{subobj.Name}\">");
                writer.WriteLine($"      <mesh>");
                writer.WriteLine($"        <source id=\"geometry{i}-positions\">");
                writer.WriteLine($"          <float_array id=\"geometry{i}-positions-array\" count=\"{subobj.Vertices.Count * 3}\">");
                foreach (var v in subobj.Vertices)
                {
                    writer.WriteLine(String.Format(icult, "            {0} {1} {2}", v.X, -v.Y, v.Z));
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
                        writer.WriteLine(String.Format(icult, "            {0} {1}", v.X, v.Y));
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
                writer.Write($"          <vcount>");
                for (var j = 0; j < subobj.Quads.Count; j++)
                {
                    writer.Write($"4 ");
                }
                for (var j = 0; j < subobj.Triangles.Count; j++)
                {
                    writer.WriteLine($"3 ");
                }
                writer.WriteLine($"          </vcount>");
                writer.WriteLine($"          <p>");
                if (subobj.HasTexture)
                {
                    foreach (var q in subobj.Quads)
                    {
                        writer.WriteLine($"            {q.A} {q.TA} {q.B} {q.TB} {q.C} {q.TC} {q.D} {q.TD}");
                    }
                    foreach (var q in subobj.Triangles)
                    {
                        writer.WriteLine($"            {q.A} {q.TA} {q.B} {q.TB} {q.C} {q.TC}");
                    }
                }
                else
                {
                    foreach (var q in subobj.Quads)
                    {
                        writer.WriteLine($"            {q.A} {q.B} {q.C} {q.D}");
                    }
                    foreach (var q in subobj.Triangles)
                    {
                        writer.WriteLine($"            {q.A} {q.B} {q.C}");
                    }
                }
                writer.WriteLine($"          </p>");
                writer.WriteLine($"        </polylist>");
                writer.WriteLine($"      </mesh>");
                writer.WriteLine($"    </geometry>");
            }
            writer.WriteLine($"  </library_geometries>");
            writer.WriteLine($"  <library_visual_scenes>");
            writer.WriteLine($"    <visual_scene id=\"Scene\" name=\"Scene\">");
            for (var i = 0; i < obj.Objects.Count; i++)
            {
                var subobj = obj.Objects[i];
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
}


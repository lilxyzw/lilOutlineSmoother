using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using jp.lilxyzw.outlinesmoother.runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace jp.lilxyzw.outlinesmoother
{
    internal static class OutlineSmootherProcessor
    {
        public static Material GetModifiedMaterial(Material material)
        {
            if (!material) return material;
            material = Object.Instantiate(material);
            if (material.HasProperty("_OutlineVertexR2Width")) material.SetInt("_OutlineVertexR2Width", 2);
            return material;
        }

        public static ValueTask Smooth(Mesh mesh, OutlineSmoother smoother)
        {
            float smoothingDistance = smoother.smoothingDistance;
            if (smoother.referenceMesh) smoothingDistance = Mathf.Max(smoothingDistance, 0.000000001f);
            float smoothingDistance2 = smoothingDistance * smoothingDistance;
            float shrinkTipStrength = smoother.shrinkTipStrength;
            var smoothedMesh = Object.Instantiate(smoother.referenceMesh ? smoother.referenceMesh : mesh);
            smoothedMesh.RecalculateNormals(MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
            var normalizedNormals = smoothedMesh.normals;
            var normalizedVertices = smoothedMesh.vertices;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            var uvs = mesh.uv;
            var colors = new Color[vertices.Length];

            var subMeshCount = mesh.subMeshCount;
            for (int submesh = 0; submesh < subMeshCount; submesh++)
            {
                if (smoother.settings is null || smoother.settings.Length <= submesh) break;
                var settings = smoother.settings[submesh];
                if (settings.skipSmoothing) continue;
                var widthMask = GetReadable(settings.widthMask);
                var normalMask = GetReadable(settings.normalMask);
                var normalMap = GetReadable(settings.normalMap);

                var indices = mesh.GetIndices(submesh).Distinct().ToArray();

                float GetWidth(Vector2 uv)
                {
                    if (!widthMask) return 1f;
                    return widthMask.Sample(uv).r;
                }

                if (normalMap)
                {
                    // ノーマルマップから取得
                    foreach(var i in indices)
                    {
                        var uv = uvs[i];
                        var width = GetWidth(uv);
                        var n = normalMap.Sample(uv);
                        colors[i] = new(n.r, n.g, n.b, width);
                    }
                }
                else if (smoothingDistance <= 0)
                {
                    // 再計算した法線から取得
                    foreach(var i in indices)
                    {
                        var uv = uvs[i];
                        var width = GetWidth(uv);
                        var normal = normals[i];
                        var tangent = tangents[i];
                        if (normal.x == tangent.x && normal.y == tangent.y && normal.z == tangent.z)
                        {
                            colors[i] = new(0.5f, 0.5f, 1.0f, width);
                            continue;
                        }
                        var outline = normalizedNormals[i];
                        if (normalMask) outline = Vector3.Normalize(Vector3.Lerp(normal, outline, normalMask.Sample(uv).r));
                        colors[i] = ToVertexColor(outline, normal, tangent, width);
                    }
                }
                else
                {
                    // 再計算した法線の平均値から取得
                    var dic = Grid(normalizedVertices, smoothingDistance);
                    foreach(var i in indices)
                    {
                        var uv = uvs[i];
                        var width = GetWidth(uv);
                        var normal = normals[i];
                        var tangent = tangents[i];
                        if (normal.x == tangent.x && normal.y == tangent.y && normal.z == tangent.z)
                        {
                            colors[i] = new(0.5f, 0.5f, 1.0f, width);
                            continue;
                        }
                        var outline = Vector3.zero;
                        var vertex = vertices[i];

                        var intpos = PosToInt(vertex, smoothingDistance);
                        var poss = new[] { intpos - Vector3Int.left, intpos, intpos - Vector3Int.right }
                            .SelectMany(p => new[] { p - Vector3Int.down, p, p - Vector3Int.up })
                            .SelectMany(p => new[] { p - Vector3Int.back, p, p - Vector3Int.forward });

                        var ns = new List<Vector3>();
                        foreach (var pos in poss)
                        {
                            if (!dic.TryGetValue(pos, out var list)) continue;
                            foreach (var index in list)
                            {
                                var nv = normalizedVertices[index];
                                if (RawDistance(vertex, nv) > smoothingDistance2) continue;
                                var n = normalizedNormals[index];
                                ns.Add(n);
                                outline += n;
                            }
                        }
                        outline.Normalize();

                        if (shrinkTipStrength > 0) width *= Mathf.Pow(Mathf.Clamp01(ns.Average(n => Vector3.Dot(n, outline))), 1f/(1.00001f-shrinkTipStrength));
                        if (normalMask) outline = Vector3.Normalize(Vector3.Lerp(normal, outline, normalMask.Sample(uv).r));
                        colors[i] = ToVertexColor(outline, normal, tangent, width);
                    }
                }
            }

            mesh.SetColors(colors);
            return default;
        }

        private static Color Sample(this Texture2D tex, Vector2 uv) => tex.GetPixelBilinear(uv.x, uv.y);

        private static Texture2D GetReadable(Texture2D tex)
        {
            if (!tex || tex.isReadable) return tex;
            var currentRT = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            RenderTexture.active = rt;
            Graphics.Blit(tex, rt);
            tex = new Texture2D(tex.width, tex.height);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        private static Color ToVertexColor(Vector3 normalAverage, Vector3 normal, Vector4 tangent, float width)
        {
            var binormal = Vector3.Cross(normal, tangent) * tangent.w;
            return new Color(
                Vector3.Dot(normalAverage, tangent) * 0.5f + 0.5f,
                Vector3.Dot(normalAverage, binormal) * 0.5f + 0.5f,
                Vector3.Dot(normalAverage, normal) * 0.5f + 0.5f,
                width
            );
        }

        private static float RawDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float y = a.y - b.y;
            float z = a.z - b.z;
            return x * x + y * y + z * z;
        }

        private static Dictionary<Vector3Int, List<int>> Grid(Vector3[] vertices, float threshold)
        {
            var dic = new Dictionary<Vector3Int, List<int>>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var intpos = PosToInt(vertex, threshold);
                if (!dic.TryGetValue(intpos, out var list))
                    list = dic[intpos] = new List<int>();
                list.Add(i);
            }
            return dic;
        }

        private static Vector3Int PosToInt(Vector3 vertex, float threshold) => new((int)(vertex.x / threshold), (int)(vertex.y / threshold), (int)(vertex.z / threshold));
    }
}

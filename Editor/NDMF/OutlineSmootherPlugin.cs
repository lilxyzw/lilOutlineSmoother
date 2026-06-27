#if LIL_NDMF
using System.Collections.Generic;
using System.Linq;
using jp.lilxyzw.outlinesmoother;
using jp.lilxyzw.outlinesmoother.runtime;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

[assembly: ExportsPlugin(typeof(OutlineSmootherPlugin))]

namespace jp.lilxyzw.outlinesmoother
{
    [RunsOnAllPlatforms]
    internal class OutlineSmootherPlugin : Plugin<OutlineSmootherPlugin>
    {
        public override string QualifiedName => "jp.lilxyzw.outlinesmoother";
        public override string DisplayName => "lilOutlineSmoother";

        protected override void Configure()
        {
            var Transforming = InPhase(BuildPhase.Transforming).BeforePlugin("nadena.dev.modular-avatar").BeforePlugin("net.rs64.tex-trans-tool");
            Transforming.Run("ModifyPreProcess", ctx =>
            {
                var smoothers = ctx.AvatarRootObject.GetComponentsInChildren<OutlineSmoother>(true);
                if (smoothers.Length == 0) return;

                var modifiedMaterials = new Dictionary<Material, Material>();
                foreach (var s in smoothers)
                {
                    var renderer = s.GetComponent<Renderer>();
                    var materials = renderer.sharedMaterials;
                    if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                    {
                        var mesh = Object.Instantiate(skinnedMeshRenderer.sharedMesh);
                        OutlineSmootherProcessor.Smooth(mesh, s, materials);
                        skinnedMeshRenderer.sharedMesh = Replace(skinnedMeshRenderer.sharedMesh, mesh, ctx);
                    }
                    else if (renderer is MeshRenderer meshRenderer)
                    {
                        var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                        if (meshFilter)
                        {
                            var mesh = Object.Instantiate(meshFilter.sharedMesh);
                            OutlineSmootherProcessor.Smooth(mesh, s, materials);
                            meshFilter.sharedMesh = Replace(meshFilter.sharedMesh, mesh, ctx);
                        }
                    }
                    renderer.sharedMaterials = ReplaceToModifiedMaterials(renderer.sharedMaterials, materials, modifiedMaterials, ctx);
                }

                foreach (var s in smoothers) Object.DestroyImmediate(s);
            }).PreviewingWith(new PreviewOutlineSmoother());
        }

        internal static Material[] ReplaceToModifiedMaterials(Material[] origs, Material[] objs, Dictionary<Material, Material> modifiedMaterials, BuildContext ctx = null)
        {
            for  (int i = 0; i < origs.Length; i++)
            {
                if (modifiedMaterials.TryGetValue(origs[i], out var value)) objs[i] = value;
                else objs[i] = Replace(origs[i], modifiedMaterials[origs[i]] = objs[i], ctx);
            }
            return objs;
        }

        internal static T Replace<T>(T orig, T obj, BuildContext ctx = null) where T : Object
        {
            if (orig == obj) return orig;
            ObjectRegistry.RegisterReplacedObject(orig, obj);
            if (ctx != null) AssetDatabase.AddObjectToAsset(obj, ctx.AssetContainer);
            return obj;
        }
    }
}
#endif

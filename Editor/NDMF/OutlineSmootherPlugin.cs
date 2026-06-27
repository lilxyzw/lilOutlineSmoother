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
                    if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
                    {
                        var mesh = Object.Instantiate(skinnedMeshRenderer.sharedMesh);
                        OutlineSmootherProcessor.Smooth(mesh, s);
                        skinnedMeshRenderer.sharedMesh = Replace(skinnedMeshRenderer.sharedMesh, mesh, ctx);
                    }
                    else if (renderer is MeshRenderer meshRenderer)
                    {
                        var meshFilter = meshRenderer.GetComponent<MeshFilter>();
                        if (meshFilter)
                        {
                            var mesh = Object.Instantiate(meshFilter.sharedMesh);
                            OutlineSmootherProcessor.Smooth(mesh, s);
                            meshFilter.sharedMesh = Replace(meshFilter.sharedMesh, mesh, ctx);
                        }
                    }
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => ReplaceToModifiedMaterial(m, modifiedMaterials, ctx)).ToArray();
                }

                foreach (var s in smoothers) Object.DestroyImmediate(s);
            }).PreviewingWith(new PreviewOutlineSmoother());
        }

        private Material ReplaceToModifiedMaterial(Material material, Dictionary<Material, Material> modifiedMaterials, BuildContext ctx)
        {
            if (modifiedMaterials.TryGetValue(material, out var value)) return value;
            return Replace(material, modifiedMaterials[material] = OutlineSmootherProcessor.GetModifiedMaterial(material), ctx);
        }

        private T Replace<T>(T orig, T obj, BuildContext ctx) where T : Object
        {
            ObjectRegistry.RegisterReplacedObject(orig, obj);
            AssetDatabase.AddObjectToAsset(obj, ctx.AssetContainer);
            return obj;
        }
    }
}
#endif

#if LIL_NDMF
using System.Linq;
using jp.lilxyzw.outlinesmoother;
using jp.lilxyzw.outlinesmoother.runtime;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

[assembly: ExportsPlugin(typeof(OutlineSmootherPlugin))]

namespace jp.lilxyzw.outlinesmoother
{
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
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => Replace(m, OutlineSmootherProcessor.GetModifiedMaterial(m), ctx)).ToArray();
                }

                foreach (var s in smoothers) Object.DestroyImmediate(s);
            }).PreviewingWith(new PreviewOutlineSmoother());
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

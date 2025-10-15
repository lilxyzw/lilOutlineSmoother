#if LIL_NDMF
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using jp.lilxyzw.outlinesmoother.runtime;
using nadena.dev.ndmf.preview;
using UnityEngine;
using Object = UnityEngine.Object;

namespace jp.lilxyzw.outlinesmoother
{
    internal class PreviewOutlineSmoother : IRenderFilter
    {
        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            return context.GetComponentsByType<OutlineSmoother>().Select(c => c.GetComponent<Renderer>()).Where(r => r is MeshRenderer or SkinnedMeshRenderer).Select(r => RenderGroup.For(r)).ToImmutableList();
        }

        public async Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var smoother = proxyPairs.First().Item1.GetComponent<OutlineSmoother>();
            var proxy = proxyPairs.First().Item2;

            context.Observe(smoother);

            var materials = proxy.sharedMaterials.Select(m => context.Observe(m));
            var mesh = default(Mesh);
            switch (proxy)
            {
                case SkinnedMeshRenderer smr:
                    if (smr.sharedMesh) mesh = context.Observe(smr.sharedMesh);
                    break;
                case MeshRenderer mr:
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf && mf.sharedMesh) mesh = context.Observe(mf.sharedMesh);
                    break;
            }
            var node = new ReplaceNode();
            await node.Process(mesh, materials, smoother);
            return node;
        }

        internal class ReplaceNode : IRenderFilterNode
        {
            private Mesh mesh;
            private Material[] materials;

            public RenderAspects WhatChanged => RenderAspects.Mesh | RenderAspects.Material;

            public async ValueTask Process(Mesh meshIn, IEnumerable<Material> materialsIn, OutlineSmoother smoother)
            {
                if (!meshIn) return;
                mesh = Object.Instantiate(meshIn);
                await OutlineSmootherProcessor.Smooth(mesh, smoother);
                materials = materialsIn.Select(m => OutlineSmootherProcessor.GetModifiedMaterial(m)).ToArray();
            }

            public void OnFrame(Renderer original, Renderer proxy)
            {
                if (!mesh) return;
                switch (proxy)
                {
                    case SkinnedMeshRenderer smr:
                        if (smr.sharedMesh) smr.sharedMesh = mesh;
                        break;
                    case MeshRenderer mr:
                        var mf = mr.GetComponent<MeshFilter>();
                        if (mf && mf.sharedMesh) mf.sharedMesh = mesh;
                        break;
                }
                proxy.sharedMaterials = materials;
            }

            void IDisposable.Dispose()
            {
                Object.DestroyImmediate(mesh);
                foreach (var m in materials) Object.DestroyImmediate(m);
            }
        }
    }
}
#endif

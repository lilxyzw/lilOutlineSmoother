using System;
using UnityEngine;

namespace jp.lilxyzw.outlinesmoother.runtime
{
    internal class OutlineSmoother : MonoBehaviour
#if LIL_VRCSDK
    , VRC.SDKBase.IEditorOnly
#endif
    {
        public Mesh referenceMesh;
        public float smoothingDistance = 0.00001f;
        [Range(0,1)] public float shrinkTipStrength;
        public MeshSettings[] settings = new MeshSettings[] { };

#if UNITY_EDITOR
        void OnValidate()
        {
            if (TryGetComponent<SkinnedMeshRenderer>(out var smr) && smr.sharedMesh is Mesh smrmesh)
            {
                Array.Resize(ref settings, smrmesh.subMeshCount);
            }
            else if (TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh is Mesh mfmesh)
            {
                Array.Resize(ref settings, mfmesh.subMeshCount);
            }
        }
#endif
    }

    [Serializable]
    internal struct MeshSettings
    {
        public bool skipSmoothing;
        public Texture2D normalMap;
        public Texture2D normalMask;
        public Texture2D widthMask;
        public Texture2D zoffsetMask;
    }
}

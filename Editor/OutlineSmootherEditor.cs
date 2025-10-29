using System.Linq;
using jp.lilxyzw.outlinesmoother.runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace jp.lilxyzw.outlinesmoother
{
    [CustomEditor(typeof(OutlineSmoother))]
    internal class OutlineSmootherEditor : Editor
    {
        public static string TEXT_NDMF_NOT_EXIST => L10n.L("Please add NDMF (Non-Destructive Modular Framework) 1.5.0 or later to your project. NDMF is automatically added when you add a Modular Avatar to your project.");
        public static string TEXT_referenceMesh => L10n.L("Reference Mesh");
        public static string TEXT_referenceMesh_tooltip => L10n.L("Specify this if you want to reference the normals of another mesh. If not specified, the normals of the existing mesh will be recalculated and used.");
        public static string TEXT_smoothingDistance => L10n.L("Smoothing Distance");
        public static string TEXT_smoothingDistance_tooltip => L10n.L("Averages the normals of vertices within a specified distance.");
        public static string TEXT_shrinkTipStrength => L10n.L("Shrink Tip Strength");
        public static string TEXT_shrinkTipStrength_tooltip => L10n.L("The dot product of the original normal and the smoothed normal is calculated, and the smaller the dot product, the smaller the outline will be.");

        public VisualElement root;
        public HelpBox ndmfNotExist;
        public PropertyField referenceMesh;
        public PropertyField smoothingDistance;
        public Slider shrinkTipStrength;

        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();
            root.FixFont();
            root.Bind(serializedObject);

            // 言語選択
            root.Add(L10n.SelectionGUI());

#if !LIL_NDMF
            // NDMFが存在しない場合のエラー
            ndmfNotExist = new HelpBox(TEXT_NDMF_NOT_EXIST, HelpBoxMessageType.Error);
            root.Add(ndmfNotExist);
#endif
            referenceMesh = new(){ bindingPath = "referenceMesh" };
            smoothingDistance = new(){ bindingPath = "smoothingDistance" };
            shrinkTipStrength = new(){ bindingPath = "shrinkTipStrength", lowValue = 0, highValue = 1, showInputField = true };

            root.Add(referenceMesh);
            root.Add(smoothingDistance);
            root.Add(shrinkTipStrength);

            // サブメッシュ設定
            using var settings = serializedObject.FindProperty("settings");
            var arraySize = settings.arraySize;
            string[] labels = new string[] { };
            var component = target as OutlineSmoother;
            if (component.TryGetComponent<Renderer>(out var renderer) && renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0)
            {
                labels = renderer.sharedMaterials.Select(m => m ? m.name : null).ToArray();
            }
            for (int i = 0; i < arraySize; i++)
            {
                var element = new VisualElement();
                root.Add(element);
                element.style.paddingTop = 10;
                var label = $"{i}.";
                if (labels.Length > i && !string.IsNullOrEmpty(labels[i])) label = $"{i}. {labels[i]}";
                element.Add(new Label(label));
                var box = new VisualElement();
                element.Add(box);
                box.Add(new PropertyField { bindingPath = $"settings.Array.data[{i}].skipSmoothing" });
                box.Add(new PropertyField { bindingPath = $"settings.Array.data[{i}].normalMap" });
                box.Add(new PropertyField { bindingPath = $"settings.Array.data[{i}].normalMask" });
                box.Add(new PropertyField { bindingPath = $"settings.Array.data[{i}].widthMask" });
                box.EnableInClassList("unity-collection-view--with-border", true);
            }

            // 言語が変更されたらラベルを更新
            UpdateVisualElements();
            L10n.langchanged += UpdateVisualElements;
            return root;
        }

        private void UpdateVisualElements()
        {
            if (serializedObject == null || !serializedObject.targetObject)
            {
                Debug.Log(this);
                L10n.langchanged -= UpdateVisualElements;
                return;
            }
#if !LIL_NDMF
            ndmfNotExist.text = TEXT_NDMF_NOT_EXIST;
#endif
            referenceMesh.label = TEXT_referenceMesh;
            referenceMesh.tooltip = TEXT_referenceMesh_tooltip;
            smoothingDistance.label = TEXT_smoothingDistance;
            smoothingDistance.tooltip = TEXT_smoothingDistance_tooltip;
            shrinkTipStrength.label = TEXT_shrinkTipStrength;
            shrinkTipStrength.tooltip = TEXT_shrinkTipStrength_tooltip;
            root.Bind(serializedObject);
        }

        private void OnDisable()
        {
            L10n.langchanged -= UpdateVisualElements;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace jp.lilxyzw.outlinesmoother
{
    internal static class VisualElementExtension
    {
        // UIElementsで日本語フォントが壊れているため生成して上書き
        private static readonly string[] fontNamesEn = {"Inter", "Arial"};
        private static readonly string[] fontNamesJp = {"Yu Gothic UI", "Meiryo UI"};
        private static bool isInitialized = false;
        private static FontAsset m_FontAsset = null;
        private static FontAsset fontAsset => m_FontAsset ? m_FontAsset : m_FontAsset = InitializeFontAsset();
        private static FontDefinition fontDefinition = FontDefinition.FromSDFFont(fontAsset);

        private static FontAsset InitializeFontAsset()
        {
            if (isInitialized) return m_FontAsset;
            isInitialized = true;
            var allFonts = Font.GetOSInstalledFontNames();

            foreach (var fontName in fontNamesEn)
                if (allFonts.Contains(fontName)) 
                    AddFont(FontAsset.CreateFontAsset(fontName, ""));

            foreach (var fontName in fontNamesJp)
                if (allFonts.Contains(fontName)) 
                    AddFont(FontAsset.CreateFontAsset(fontName, ""));

            return m_FontAsset;
        }

        private static void AddFont(FontAsset fontAsset)
        {
            if (m_FontAsset)
            {
                m_FontAsset.fallbackFontAssetTable.Add(fontAsset);
                return;
            }

            m_FontAsset = fontAsset;
            m_FontAsset.fallbackFontAssetTable = new List<FontAsset>();
        }

        internal static void FixFont(this VisualElement element)
        {
            if (fontAsset) element.style.unityFontDefinition = fontDefinition;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace jp.lilxyzw.outlinesmoother
{
    internal partial class L10n : ScriptableSingleton<L10n>
    {
        public LocalizationAsset localizationAsset;
        private static string[] languages;
        private static string[] languageNames;
        private static readonly Dictionary<string, GUIContent> guicontents = new();
        private static string localizationFolder => AssetDatabase.GUIDToAssetPath("3bf54f60e8f539e4f879dac13bd53a44");
        public delegate void CallbackFunction();
        public static CallbackFunction langchanged;

        private static void Load()
        {
            guicontents.Clear();
            var path = localizationFolder + "/" + Settings.Language + ".po";
            if (File.Exists(path)) instance.localizationAsset = AssetDatabase.LoadAssetAtPath<LocalizationAsset>(path);

            if (!instance.localizationAsset) instance.localizationAsset = new LocalizationAsset();
        }

        internal static string[] GetLanguages()
        {
            return languages ??= Directory.GetFiles(localizationFolder).Where(f => f.EndsWith(".po")).Select(f => Path.GetFileNameWithoutExtension(f)).Where(f => !f.StartsWith(".")).ToArray();
        }

        private static string[] GetLanguageNames()
        {
            return languageNames ??= languages.Select(l =>
            {
                if (l == "zh-Hans") return "简体中文";
                if (l == "zh-Hant") return "繁體中文";
                return new CultureInfo(l).NativeName;
            }).ToArray();
        }

        internal static string L(string key)
        {
            if (!instance.localizationAsset) Load();
            return instance.localizationAsset.GetLocalizedString(key);
        }

        internal static VisualElement SelectionGUI()
        {
            return new IMGUIContainer(() =>
            {
                EditorGUI.BeginChangeCheck();
                var value = EditorGUILayout.Popup("Language", Array.IndexOf(GetLanguages(), Settings.Language), GetLanguageNames());
                if (EditorGUI.EndChangeCheck())
                {
                    Settings.instance.language = GetLanguages()[value];
                    Settings.Save();
                    Load();
                    langchanged?.Invoke();
                }
            });
        }
    }
}

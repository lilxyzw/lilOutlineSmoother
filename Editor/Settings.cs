using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace jp.lilxyzw.outlinesmoother
{
    [FilePath("jp.lilxyzw/outlinesmoother.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal class Settings : ScriptableSingleton<Settings>
    {
        [Tooltip("The language setting for lilOutlineSmoother. The language file exists in `jp.lilxyzw.outlinesmoother/Editor/Localization`, and you can support other languages by creating a language file.")]
        public string language = CultureInfo.CurrentCulture.Name;
        public static string Language => L10n.GetLanguages().Contains(instance.language) ? instance.language : "en-US";

        public static void Save() => instance.Save(true);
    }
}

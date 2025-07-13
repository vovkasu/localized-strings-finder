using UnityEngine.Localization;

namespace LocalizedStringsFinder.Editor
{
    public class LocalizedStringReference
    {
        public LocalizedString LocalizedString;
        public string AssetPath;
        public AssetObjType AssetType;

        public override string ToString()
        {
            return $" LocalizedString:{LocalizedString} AssetType:{AssetType} AssetPath:{AssetPath}";
        }
    }
}
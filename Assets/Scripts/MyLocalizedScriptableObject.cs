using UnityEngine;
using UnityEngine.Localization;

namespace LocalizedStringsFinder
{
    [CreateAssetMenu(menuName = "Create MyLocalizedScriptableObject", fileName = "MyLocalizedScriptableObject", order = 0)]
    public class MyLocalizedScriptableObject : ScriptableObject
    {
        public LocalizedString localizedString;
    }
}

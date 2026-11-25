using UnityEngine;

namespace BeachBoxing
{
    internal static class OriginalArt
    {
        private const string Root = "BeachOriginal/";

        public static T Get<T>(string path) where T : Object => Resources.Load<T>(Root + path);

        public static Transform Place(Transform parent, string prefab, string name, Vector3 position)
        {
            var source = Get<GameObject>("Models/" + prefab);
            if (!source) return null;
            var instance = Object.Instantiate(source, parent, false).transform;
            instance.name = name;
            instance.localPosition = position;
            return instance;
        }
    }
}

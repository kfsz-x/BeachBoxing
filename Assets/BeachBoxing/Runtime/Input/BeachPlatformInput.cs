using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BeachBoxing
{
    public static class BeachPlatformInput
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int BB_HasQGTouch();
#endif

        public static bool HasQGTouch
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return BB_HasQGTouch() != 0;
#else
                return false;
#endif
            }
        }

        public static bool UseTouch { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => UseTouch = false;

        public static bool DetectMobile(bool simulateTouchInEditor = false)
        {
            bool mobile = Application.isEditor
                ? simulateTouchInEditor
                : Application.isMobilePlatform || HasQGTouch;
            UseTouch = mobile;
            return mobile;
        }

        public static void SetTouchOverride(bool enabled) => UseTouch = enabled;

        public static void EnsureEventSystem(Transform preferredParent = null)
        {
            var all = UnityEngine.Object.FindObjectsOfType<EventSystem>(true);
            EventSystem active = null;
            foreach (var es in all)
            {
                if (preferredParent != null && es.transform.IsChildOf(preferredParent))
                {
                    active = es;
                    break;
                }
            }
            if (active == null && all.Length > 0) active = all[0];

            if (active == null)
            {
                var host = new GameObject("EventSystem", typeof(EventSystem));
                if (preferredParent != null) host.transform.SetParent(preferredParent, false);
                active = host.GetComponent<EventSystem>();
            }

            foreach (var es in all)
            {
                if (es != active) UnityEngine.Object.Destroy(es.gameObject);
            }

            var standalone = active.GetComponent<StandaloneInputModule>();
            var touchModule = active.GetComponent<BeachTouchModule>();
            if (standalone != null && !(standalone is BeachTouchModule))
                UnityEngine.Object.Destroy(standalone);
            if (touchModule == null)
                touchModule = active.gameObject.AddComponent<BeachTouchModule>();
            touchModule.enabled = true;
            active.sendNavigationEvents = false;

#if UNITY_WEBGL && !UNITY_EDITOR
            if (HasQGTouch)
            {
                var overrideType = Type.GetType("QGTouchInputOverride, VIVO-Game-SDK")
                    ?? Type.GetType("QGTouchInputOverride");
                if (overrideType != null && active.GetComponent(overrideType) == null)
                    active.gameObject.AddComponent(overrideType);
            }
#endif
        }
    }
}

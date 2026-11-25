using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    public static class BeachMobileWorkoutRestore
    {
        const string UI = "Assets/BeachBoxing/OriginalArt/Resources/BeachOriginal/UI/";

        [MenuItem("Beach Boxing/Restore separate mobile workout layout")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play before changing saved workout controls.");
            var flow = Object.FindObjectOfType<BeachGameFlow>();
            var data = new SerializedObject(flow);
            var hud = data.FindProperty("hud");
            var workout = hud.FindPropertyRelative("workout").objectReferenceValue as GameObject;
            Undo.RegisterFullObjectHierarchyUndo(workout, "Restore mobile workout layout");
            var desktop = workout.transform.Find("Desktop workout layout") as RectTransform;
            if (!desktop)
            {
                var children = new Transform[workout.transform.childCount];
                for (int i = 0; i < children.Length; i++) children[i] = workout.transform.GetChild(i);
                desktop = new GameObject("Desktop workout layout", typeof(RectTransform)).GetComponent<RectTransform>();
                desktop.SetParent(workout.transform, false);
                desktop.anchorMin = Vector2.zero; desktop.anchorMax = Vector2.one;
                desktop.offsetMin = desktop.offsetMax = Vector2.zero;
                foreach (var child in children) child.SetParent(desktop, false);
            }
            var mobile = workout.transform.Find("Mobile workout layout") as RectTransform;
            if (!mobile)
            {
                mobile = Object.Instantiate(desktop, workout.transform, false);
                mobile.name = "Mobile workout layout";
            }
            mobile.anchorMin = mobile.anchorMax = new Vector2(.5f, 0);
            mobile.pivot = new Vector2(.5f, 0);
            mobile.anchoredPosition = new Vector2(0, -60);
            mobile.sizeDelta = new Vector2(400, 240);
            var horizontalFinger = mobile.Find("Icon");
            if (horizontalFinger) horizontalFinger.gameObject.SetActive(false);
            var keyboardHint = mobile.Find("Label");
            if (keyboardHint) keyboardHint.gameObject.SetActive(false);
            var boost = mobile.Find("WORKOUT FASTER").GetComponent<TouchControl>();
            var exit = mobile.Find("EXIT").GetComponent<Button>();
            var bar = mobile.Find("Repetition") as RectTransform;
            Bottom((RectTransform)boost.transform, 21, 48, 202, 83);
            Bottom((RectTransform)exit.transform, 238, 48, 125, 50);
            Bottom(bar, 20, 8, 360, 24);
            var boostText = boost.GetComponentInChildren<Text>(true);
            boostText.text = "WORKOUT\nFASTER"; boostText.fontSize = 26;
            boostText.resizeTextForBestFit = false;
            boostText.horizontalOverflow = HorizontalWrapMode.Wrap;
            boostText.verticalOverflow = VerticalWrapMode.Overflow;
            var exitText = exit.GetComponentInChildren<Text>(true);
            exitText.fontSize = 36; exitText.resizeTextForBestFit = false;
            var left = Guide(mobile, "Mobile downward finger", "Finger", new Vector2(118, 194), new Vector2(65, 90), 180);
            var arrows = Guide(mobile, "Mobile training arrows", "arrows", new Vector2(200, 168), new Vector2(100, 100), 0);
            var right = Guide(mobile, "Mobile angled finger", "Finger", new Vector2(246, 93), new Vector2(90, 125), 25);
            left.SetAsFirstSibling(); arrows.SetAsFirstSibling(); right.SetAsFirstSibling();
            boost.transform.SetAsLastSibling(); exit.transform.SetAsLastSibling();
            hud.FindPropertyRelative("desktopWorkoutLayout").objectReferenceValue = desktop.gameObject;
            hud.FindPropertyRelative("mobileWorkoutLayout").objectReferenceValue = mobile.gameObject;
            hud.FindPropertyRelative("desktopBoost").objectReferenceValue = desktop.GetComponentInChildren<TouchControl>(true);
            hud.FindPropertyRelative("mobileBoost").objectReferenceValue = boost;
            hud.FindPropertyRelative("mobileExit").objectReferenceValue = exit;
            hud.FindPropertyRelative("mobileFill").objectReferenceValue = bar.Find("Fill").GetComponent<Image>();
            desktop.gameObject.SetActive(true); mobile.gameObject.SetActive(false);
            foreach (var text in mobile.GetComponentsInChildren<Text>(true)) text.resizeTextForBestFit = false;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(flow);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
        }

        static void Bottom(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height); rect.localScale = Vector3.one;
        }
        static RectTransform Guide(RectTransform parent, string name, string sprite, Vector2 position, Vector2 size, float angle)
        {
            var child = parent.Find(name);
            var image = child ? child.GetComponent<Image>() : new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var rect = image.rectTransform; rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position; rect.sizeDelta = size; rect.localEulerAngles = new Vector3(0, 0, angle);
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UI + sprite + ".asset");
            image.preserveAspect = true; image.raycastTarget = false;
            return rect;
        }
    }
}

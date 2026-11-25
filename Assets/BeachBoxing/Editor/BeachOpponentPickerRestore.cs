using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    public static class BeachOpponentPickerRestore
    {
        const string UI = "Assets/BeachBoxing/OriginalArt/Resources/BeachOriginal/UI/";
        const string Art = "Assets/BeachBoxing/OriginalArt/Sprite/";
        static Font font;
        static Material outline;

        [MenuItem("Beach Boxing/Restore original opponent picker")]
        public static void Apply()
        {
            if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play before restoring the saved picker.");
            var flow = Object.FindObjectOfType<BeachGameFlow>();
            var data = new SerializedObject(flow);
            var hud = data.FindProperty("hud");
            var picker = Ref<GameObject>(hud, "picker");
            Undo.RegisterFullObjectHierarchyUndo(picker, "Restore original opponent picker");
            font = Ref<Font>(hud, "font");
            outline = AssetDatabase.LoadAssetAtPath<Material>("Assets/BeachBoxing/OriginalFont/LilitaOneOriginalOutline.mat");
            var root = (RectTransform)picker.transform;
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            picker.GetComponent<Image>().enabled = false;
            var card = Rect(root, "Original three-column card");
            card.anchorMin = card.anchorMax = Vector2.one * 0.5f; card.pivot = Vector2.one * 0.5f;
            card.anchoredPosition = new Vector2(-7.5f, -11.5f); card.sizeDelta = new Vector2(575, 427);
            Picture(card, "Original outer frame", "BG 1", 0, 0, 575, 427);
            Picture(card, "Original central and progress frame", "image (2)", 130, 20, 425, 384);
            Picture(card, "Original stats frame", "image (2) 1", 15, 20, 114, 384);

            Text heading = null;
            foreach (var text in root.GetComponentsInChildren<Text>(true))
                if (text.text == "PICK OPPONENT") { heading = text; break; }
            if (!heading) heading = Label(card, "Picker heading", "PICK OPPONENT", 48, 110, -36, 360, 60);
            heading.transform.SetParent(card, false); Top(heading.rectTransform, 110, -36, 360, 60);
            Style(heading, 48, true); heading.text = "PICK OPPONENT";
            Label(card, "Stats heading", "STATS", 21, 20, 45, 104, 30);
            var stats = hud.FindPropertyRelative("opponentStats"); stats.arraySize = 4;
            string[] muscles = { "aRmwad 1", "aRmwad", "aRmwad 3", "aRmwad 2" };
            for (int index = 0; index < 4; index++)
            {
                Picture(card, "Opponent muscle " + index, muscles[index], 49, 82 + 77 * index, 40, 44).preserveAspect = true;
                var number = Label(card, "Opponent muscle value " + index, "0", 22, 20, 123 + 77 * index, 104, 28);
                stats.GetArrayElementAtIndex(index).objectReferenceValue = number;
            }
            var selection = Ref<Text>(hud, "selection"); selection.transform.SetParent(card, false);
            Top(selection.rectTransform, 145, 34, 281, 55); Style(selection, 40, true); selection.text = "Ramez";
            var ring = Ref<Image>(hud, "portrait").transform.parent as RectTransform;
            ring.SetParent(card, false); Top(ring, 151, 90, 264, 164);
            var ringImage = ring.GetComponent<Image>(); ringImage.sprite = Sprite("BG 2"); ringImage.color = Color.white;
            var mask = ring.GetComponent<Mask>() ?? ring.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            var ringArt = Picture(ring, "Original ring backdrop", "New Project (51)", 0, 0, 264, 164);
            ringArt.transform.SetAsFirstSibling();
            var portrait = Ref<Image>(hud, "portrait"); Top(portrait.rectTransform, 85, 28, 95, 144); portrait.preserveAspect = true;
            Picture(card, "Coin reward icon", "Coin", 178, 268, 38, 38).preserveAspect = true;
            Picture(card, "Star reward icon", "Gem", 290, 268, 38, 38).preserveAspect = true;
            hud.FindPropertyRelative("opponentCoinValue").objectReferenceValue = Label(card, "Opponent coin value", "50", 26, 216, 268, 62, 38);
            hud.FindPropertyRelative("opponentStarValue").objectReferenceValue = Label(card, "Opponent star value", "3", 26, 327, 268, 40, 38);

            var fight = Ref<Button>(hud, "fightButton"); Reparent(fight.transform, card, 192, 316, 173, 66);
            fight.GetComponent<Image>().sprite = Sprite("Join Room BTN 1");
            var fightText = fight.GetComponentInChildren<Text>(true); Top(fightText.rectTransform, 48, 7, 120, 48); Style(fightText, 36, false);
            Picture((RectTransform)fight.transform, "Original fight gloves", "2736136", 9, 12, 40, 40).preserveAspect = true;
            var finger = Picture(card, "Original fight finger", "Finger", 130, 326, 46, 54); finger.preserveAspect = true;
            finger.rectTransform.pivot = Vector2.one * .5f; finger.rectTransform.anchoredPosition = new Vector2(153, -350); finger.rectTransform.localEulerAngles = new Vector3(0, 0, -90);

            var close = Ref<Button>(hud, "closeButton"); Reparent(close.transform, card, 539, -18, 54, 58);
            close.GetComponent<Image>().sprite = Sprite("XButton");
            foreach (var text in close.GetComponentsInChildren<Text>(true)) { text.gameObject.SetActive(true); text.text = "X"; Style(text, 34, false); }
            ArrowButton(Ref<Button>(hud, "previous"), card, 142, 136, true);
            ArrowButton(Ref<Button>(hud, "next"), card, 384, 136, false);
            Label(card, "Gauntlet heading", "Rookie\nGauntlet", 21, 442, 34, 104, 52);
            var skull = Picture(card, "Gauntlet skull", "aRmwad 5", 488, 91, 36, 36); skull.color = new Color(1, 1, 1, .3f); skull.preserveAspect = true;
            var line = Picture(card, "Gauntlet path", "Square", 503, 128, 7, 216); line.color = new Color(1, 1, 1, .3f);
            var nodes = hud.FindPropertyRelative("opponentProgressNodes"); nodes.arraySize = 2;
            for (int index = 0; index < 2; index++)
            {
                var dot = Picture(card, "Core opponent node " + index, "Circle", 495, 346 - index * 65, 22, 22);
                dot.color = index == 0 ? Color.white : new Color(1, 1, 1, .3f); nodes.GetArrayElementAtIndex(index).objectReferenceValue = dot;
            }
            var marker = Picture(card, "Current opponent arrow", "Arrow", 465, 346, 30, 24); marker.preserveAspect = true;
            marker.rectTransform.pivot = new Vector2(0, .5f); marker.rectTransform.anchoredPosition = new Vector2(465, -357);
            hud.FindPropertyRelative("opponentCardArt").objectReferenceValue = card;
            hud.FindPropertyRelative("opponentProgressArrow").objectReferenceValue = marker.rectTransform;
            data.ApplyModifiedPropertiesWithoutUndo();
            var returnLabel = Ref<Text>(hud, "returnLabel");
            // The recovered bitmap font has fixed glyph metrics; Unity Best Fit supports dynamic fonts only.
            returnLabel.resizeTextForBestFit = false;
            returnLabel.horizontalOverflow = HorizontalWrapMode.Wrap; returnLabel.verticalOverflow = VerticalWrapMode.Truncate;
            picker.SetActive(false);
            EditorUtility.SetDirty(flow); EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
        }

        static T Ref<T>(SerializedProperty p, string name) where T : Object => p.FindPropertyRelative(name).objectReferenceValue as T;
        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(UI + name + ".asset") ?? AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".asset");
        static RectTransform Rect(Transform parent, string name)
        {
            var child = parent.Find(name); var rect = child ? (RectTransform)child : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        static void Top(RectTransform rect, float x, float y, float width, float height)
        { rect.anchorMin = rect.anchorMax = Vector2.up; rect.pivot = Vector2.up; rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); rect.localScale = Vector3.one; }
        static void Reparent(Transform rect, Transform parent, float x, float y, float width, float height)
        { rect.SetParent(parent, false); Top((RectTransform)rect, x, y, width, height); }
        static Image Picture(RectTransform parent, string name, string sprite, float x, float y, float width, float height)
        {
            var rect = Rect(parent, name); Top(rect, x, y, width, height); var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(sprite); image.color = Color.white; image.raycastTarget = false; return image;
        }
        static Text Label(RectTransform parent, string name, string caption, int size, float x, float y, float width, float height)
        {
            var rect = Rect(parent, name); Top(rect, x, y, width, height); var text = rect.GetComponent<Text>() ?? rect.gameObject.AddComponent<Text>();
            Style(text, size, false); text.text = caption; return text;
        }
        static void Style(Text text, int size, bool outlined)
        {
            text.font = font; text.fontSize = size; text.fontStyle = FontStyle.Normal; text.material = outlined ? outline : font.material;
            text.resizeTextForBestFit = false; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
        }
        static void ArrowButton(Button button, RectTransform parent, float x, float y, bool left)
        {
            Reparent(button.transform, parent, x, y, 50, 64); var image = button.GetComponent<Image>(); image.sprite = Sprite("Arrow"); image.color = new Color(1, 1, 1, .7f); image.preserveAspect = true;
            button.transform.localScale = new Vector3(left ? -1 : 1, 1, 1);
            foreach (var text in button.GetComponentsInChildren<Text>(true)) text.gameObject.SetActive(false);
        }
    }
}

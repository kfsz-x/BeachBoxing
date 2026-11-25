using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    public static class BeachChineseUiRestore
    {
        [MenuItem("Beach Boxing/Apply Chinese UI")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before updating saved UI.");
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/BeachBoxing/Fonts/NotoSansSC-Regular.otf");
            if (!font || !font.HasCharacter('中'))
                throw new InvalidOperationException("Import the Chinese font first.");
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                var labels = root.GetComponentsInChildren<Text>(true);
                if (labels.Length == 0) continue;
                foreach (var label in labels)
                {
                    Undo.RecordObject(label, "Localize UI text");
                    label.font = font;
                    label.material = null;
                    var input = label.GetComponentInParent<InputField>();
                    bool editable = input && input.textComponent == label;
                    var sdf = label as OriginalSdfText;
                    if (sdf)
                    {
                        sdf.useLocalizedFont = true;
                        sdf.translateText = !editable;
                        if (sdf.nativePointSize > 0) sdf.fontSize = Mathf.Max(1, Mathf.RoundToInt(sdf.nativePointSize));
                    }
                    if (!editable) label.text = BeachChineseText.Translate(label.text);
                    label.SetAllDirty();
                    EditorUtility.SetDirty(label);
                }
                var localizer = root.GetComponent<BeachChineseUi>();
                if (!localizer) localizer = Undo.AddComponent<BeachChineseUi>(root);
                Undo.RecordObject(localizer, "Bind Chinese labels");
                localizer.labels = labels;
                EditorUtility.SetDirty(localizer);

                var flow = root.GetComponent<BeachGameFlow>();
                if (!flow) continue;
                var hud = new SerializedObject(flow).FindProperty("hud");
                var native = root.GetComponent<BeachPeripheralView>();
                if (native && native.IsBound)
                    Hide(hud, "stats", "menu", "interact", "prompt");
                if (root.GetComponent<BeachNativeTrainingView>()) Hide(hud, "workout");
                if (root.GetComponent<BeachNativeOpponentView>()) Hide(hud, "picker");
                if (root.GetComponent<BeachNativeMobileControls>())
                    Hide(hud, "move", "attack", "block", "joystickGraphic", "tutorialFinger");
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void Hide(SerializedProperty hud, params string[] names)
        {
            foreach (var name in names)
            {
                var value = hud.FindPropertyRelative(name)?.objectReferenceValue;
                var target = value as GameObject;
                if (value is Component component) target = component.gameObject;
                if (!target || !target.activeSelf) continue;
                Undo.RecordObject(target, "Hide duplicate UI");
                target.SetActive(false);
            }
        }
    }
}

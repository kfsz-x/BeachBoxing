using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    public static class BeachReferenceCapture
    {
        /// <summary>Record ordinary touch input: an empty punch, approach, guard, then pursue and punch.</summary>
        public static void RecordCombat()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter gameplay before recording.");
            string folder = Path.Combine(Path.GetTempPath(), "BeachBoxing-editor-fight");
            Directory.CreateDirectory(folder);
            var flow = Object.FindObjectOfType<BeachGameFlow>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var store = UnityCore.CoreRoot.Current.Storage;
            string backup = store.GetString("beachBoxing.progress");
            File.WriteAllText(Path.Combine(folder, "save-backup.json"), backup);
            var data = new SerializedObject(flow);
            var entry = (Transform)data.FindProperty("stage.ringEntry").objectReferenceValue;
            GameObject.Find("Player").transform.position = entry.position;
            flow.GetType().GetMethod("LocateInteraction", flags).Invoke(flow, null);
            flow.GetType().GetMethod("Interact", flags).Invoke(flow, null);
            flow.GetType().GetMethod("BeginFight", flags).Invoke(flow, null);
            var hud = data.FindProperty("hud");
            var move = ((GameObject)hud.FindPropertyRelative("move").objectReferenceValue).GetComponent<TouchControl>();
            var punch = ((GameObject)hud.FindPropertyRelative("attack").objectReferenceValue).GetComponent<TouchControl>();
            var block = ((GameObject)hud.FindPropertyRelative("block").objectReferenceValue).GetComponent<TouchControl>();
            var thumb = new PointerEventData(EventSystem.current) { pointerId = 610, position = new Vector2(100, 100) };
            var fist = new PointerEventData(EventSystem.current) { pointerId = 611 };
            var shield = new PointerEventData(EventSystem.current) { pointerId = 612 };
            int index = 0, lastFrame = -1, blockedHits = 0;
            int lastImpacts = 0;
            const int framesPerSecond = 30;
            int previousCaptureFramerate = Time.captureFramerate;
            int firstPunchFrame = Mathf.CeilToInt((BeachPresentationTimeline.OpeningDuration + BeachPresentationTimeline.SceneLoadDelay) * framesPerSecond) + 2;
            int finalFrame = firstPunchFrame + 265;
            Time.captureFramerate = framesPerSecond;
            EditorApplication.CallbackFunction capture = null;
            capture = () =>
            {
                if (!EditorApplication.isPlaying || !flow || index >= finalFrame)
                {
                    EditorApplication.update -= capture;
                    Time.captureFramerate = previousCaptureFramerate;
                    if (move) move.OnPointerUp(thumb);
                    if (punch) ExecuteEvents.Execute(punch.gameObject, fist, ExecuteEvents.pointerUpHandler);
                    if (block) ExecuteEvents.Execute(block.gameObject, shield, ExecuteEvents.pointerUpHandler);
                    store.SetString("beachBoxing.progress", backup);
                    store.Save();
                    if (flow)
                    {
                        // Freeze the completed capture before a queued punch can save another result.
                        flow.enabled = false;
                        flow.GetType().GetField("progress", flags).SetValue(flow, new ProgressStore(store).Load());
                    }
                    File.WriteAllText(Path.Combine(folder, "complete.txt"), index + " frames; blocked hits=" + blockedHits);
                    return;
                }
                if (Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                if (index == firstPunchFrame) ExecuteEvents.Execute(punch.gameObject, fist, ExecuteEvents.pointerDownHandler);
                if (index == firstPunchFrame + 25)
                {
                    ExecuteEvents.Execute(punch.gameObject, fist, ExecuteEvents.pointerUpHandler);
                    move.OnPointerDown(thumb);
                    thumb.position += Vector2.up * Mathf.Max(36, Screen.width * .14f);
                    move.OnDrag(thumb);
                }
                if (index == firstPunchFrame + 47) ExecuteEvents.Execute(block.gameObject, shield, ExecuteEvents.pointerDownHandler);
                if (index == firstPunchFrame + 145)
                {
                    ExecuteEvents.Execute(block.gameObject, shield, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.Execute(punch.gameObject, fist, ExecuteEvents.pointerDownHandler);
                    thumb.position = new Vector2(100, 100);
                    move.OnPointerDown(thumb);
                    thumb.position += Vector2.up * Mathf.Max(36, Screen.width * .14f);
                    move.OnDrag(thumb);
                }
                var fight = (FightController)flow.GetType().GetField("fight", flags).GetValue(flow);
                if (fight != null)
                {
                    if (fight.Player.ImpactCount > lastImpacts && fight.Player.LastImpactWasBlocked) blockedHits++;
                    lastImpacts = fight.Player.ImpactCount;
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "frame" + index.ToString("D4") + ".png"));
                index++;
            };
            EditorApplication.update += capture;
        }

        /// <summary>Capture each bound workout through its normal interaction and exit callbacks.</summary>
        public static void CaptureWorkouts()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter gameplay before capturing.");
            var flow = Object.FindObjectOfType<BeachGameFlow>();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var store = UnityCore.CoreRoot.Current.Storage;
            string backup = store.GetString("beachBoxing.progress");
            string folder = Path.Combine(Application.dataPath, "../docs/beach-boxing/reference");
            var player = GameObject.Find("Player").transform;
            var beach = GameObject.Find("Training beach").transform;
            string[] names = { "Chest", "Arms", "Back", "Legs" };
            int index = 0, state = 0;
            double next = EditorApplication.timeSinceStartup;
            EditorApplication.CallbackFunction capture = null;
            capture = () =>
            {
                if (!EditorApplication.isPlaying || !flow)
                {
                    EditorApplication.update -= capture;
                    store.SetString("beachBoxing.progress", backup);
                    store.Save();
                    return;
                }
                if (EditorApplication.timeSinceStartup < next) return;
                if (state == 0)
                {
                    player.position = beach.Find(names[index]).position;
                    flow.GetType().GetMethod("LocateInteraction", flags).Invoke(flow, null);
                    flow.GetType().GetMethod("Interact", flags).Invoke(flow, null);
                    next = EditorApplication.timeSinceStartup + 1.7;
                    state = 1;
                }
                else if (state == 1)
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder, "replica-workout-" + names[index] + ".png"));
                    next = EditorApplication.timeSinceStartup + .2;
                    state = 2;
                }
                else
                {
                    flow.GetType().GetMethod("StopTraining", flags).Invoke(flow, null);
                    if (++index < names.Length)
                    {
                        next = EditorApplication.timeSinceStartup + .2;
                        state = 0;
                        return;
                    }
                    EditorApplication.update -= capture;
                    store.SetString("beachBoxing.progress", backup);
                    store.Save();
                    flow.GetType().GetField("progress", flags).SetValue(flow, new ProgressStore(store).Load());
                    Debug.Log("Captured four original workout poses; restored saved progress.");
                }
            };
            EditorApplication.update += capture;
        }

        public static void RecordTraining()
        {
            if (!EditorApplication.isPlaying) throw new System.InvalidOperationException("Enter gameplay before recording.");
            string folder = Path.Combine(Path.GetTempPath(), "BeachBoxing-editor-video");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "save-backup.json"), UnityCore.CoreRoot.Current.Storage.GetString("beachBoxing.progress"));
            var player = GameObject.Find("Player").transform;
            var target = GameObject.Find("Training beach").transform.Find("Arms");
            var move = GameObject.Find("Safe area").transform.Find("Move").GetComponent<TouchControl>();
            var camera = GameObject.Find("Gameplay camera").transform;
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 531, button = PointerEventData.InputButton.Left };
            bool held = false, trained = false, exited = false;
            int workoutFrame = 0, index = 0, lastFrame = -1;
            Time.captureFramerate = 30;
            EditorApplication.CallbackFunction capture = null;
            capture = () =>
            {
                if (!EditorApplication.isPlaying || index >= 240)
                {
                    EditorApplication.update -= capture;
                    Time.captureFramerate = 0;
                    if (held && move) move.OnPointerUp(pointer);
                    File.WriteAllText(Path.Combine(folder, "complete.txt"), index + " frames; trained=" + trained + "; exited=" + exited);
                    return;
                }
                if (Time.frameCount == lastFrame) return;
                lastFrame = Time.frameCount;
                if (index > 15 && !trained)
                {
                    Vector3 delta = target.position - player.position;
                    delta.y = 0;
                    if (delta.magnitude > 1.2f)
                    {
                        if (!held) { pointer.position = new Vector2(100, 100); move.OnPointerDown(pointer); held = true; }
                        Vector3 forward = camera.forward; forward.y = 0;
                        Vector3 right = camera.right; right.y = 0;
                        Vector3 direction = delta.normalized;
                        Vector2 input = new Vector2(Vector3.Dot(direction, right.normalized), Vector3.Dot(direction, forward.normalized));
                        pointer.position = new Vector2(100, 100) + input * Mathf.Max(36, Screen.width * .14f);
                        move.OnDrag(pointer);
                    }
                    else
                    {
                        if (held) { move.OnPointerUp(pointer); held = false; }
                        var button = GameObject.Find("INTERACT");
                        if (button) { button.GetComponent<Button>().onClick.Invoke(); trained = true; workoutFrame = index; }
                    }
                }
                if (trained && !exited && index - workoutFrame > 80)
                {
                    var button = GameObject.Find("EXIT");
                    if (button) { button.GetComponent<Button>().onClick.Invoke(); exited = true; }
                }
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "frame" + index.ToString("D4") + ".png"));
                index++;
            };
            EditorApplication.update += capture;
        }
    }
}

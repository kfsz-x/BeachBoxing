using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BeachBoxing.Editor
{
    public static class BeachNativeMobileControlsRestore
    {
        [MenuItem("Beach Boxing/Restore original landscape controls")]
        public static void Apply() => BeachPeripheralRestore.AppendLandscapeControls();
    }

    public static partial class BeachPeripheralRestore
    {
        private const string LandscapeUndo = "Restore source landscape controls";
        private const int CombatCanvas = 1000037, CombatMobile = 1000327;
        private static readonly int[] BeachControlAppendIds = { 37, 165, 1405, 1407, 1406, 532, 1115, 865 };
        private static readonly int[] CombatControlSourceIds =
        {
            19, 27, 48, 86, 92, 97, 103, 138, 205, 207, 225, 249, 308, 320, 327, 364,
            372, 444, 450, 469, 491, 507, 543, 559, 577, 589, 601, 633, 885, 886, 887
        };

        internal static bool IsLandscapeControlNode(int id) =>
            BeachControlAppendIds.Contains(id) || CombatControlSourceIds.Contains(id - 1000000);

        internal static void AppendLandscapeControls()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before restoring controls.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated Beach Boxing scene.");
            var peripheral = flow.GetComponent<BeachPeripheralView>();
            var canvasRect = flow.transform.Find(CanvasName) as RectTransform;
            var canvas = canvasRect ? canvasRect.GetComponent<Canvas>() : null;
            var feedback = flow.GetComponent<BeachNativeCombatFeedback>();
            var screen = feedback && feedback.screen ? feedback.screen.transform as RectTransform : null;
            if (!peripheral || !canvas || !screen || screen.parent != canvasRect)
                throw new InvalidOperationException("Restore the original peripheral canvas and combat feedback screen first.");

            var source = JsonUtility.FromJson<NativeUi>(Required<TextAsset>(RemainingPath).text);
            var rows = source.nodes.Where(row => IsLandscapeControlNode(row.sourceId)).ToArray();
            if (rows.Length != 39 || rows.Count(row => row.sourceId >= 1000000) != 31)
                throw new InvalidOperationException("Expected eight source beach nodes and 31 source combat nodes.");
            var nodes = peripheral.nodes.Where(node => node != null && node.transform).ToDictionary(node => node.sourceId);
            var existing = new HashSet<int>(nodes.Keys);
            foreach (int id in new[] { 559, 172, 548, 140, 1110, 626, 972, 601, 820, 333, 967, 1057, 804 })
                if (!existing.Contains(id)) throw new InvalidOperationException("Missing original UI node " + id);
            var green = nodes[1057].button;
            var hand = nodes[1110].button;
            var audio = nodes[967].transform.GetComponent<AudioSource>();
            var workout = nodes[804].transform.GetComponent<TouchControl>();
            if (!green || !hand || !audio || !audio.clip || !workout)
                throw new InvalidOperationException("Bind the original training and interaction controls before appending mobile UI.");

            // The original combat canvas has its own source ID, even though the merged scene shares one Canvas.
            nodes.Add(CombatCanvas, new NativeUiNode { sourceId = CombatCanvas, transform = screen });
            var parents = new HashSet<int>(nodes.Keys);
            foreach (var row in rows)
            {
                if (!parents.Contains(row.parentId))
                    throw new InvalidOperationException("Missing source parent " + row.parentId + " for " + row.sourceId);
                parents.Add(row.sourceId);
                if (row.image?.componentId > 0) SpriteAt(row.image.spritePath);
                if (!(row.button?.componentId > 0)) continue;
                if (row.button.targetId != 0 && !parents.Contains(row.button.targetId))
                    throw new InvalidOperationException("Missing source button target " + row.button.targetId + " for " + row.sourceId);
                SpriteAt(row.button.highlightedSprite);
                SpriteAt(row.button.pressedSprite);
                SpriteAt(row.button.selectedSprite);
                SpriteAt(row.button.disabledSprite);
            }
            var motions = RemainingBindings().Where(binding => binding.targetId == 532 || binding.targetId == 1000364)
                .Select(binding => (binding, clip: RemainingClip(binding.defaultClipId))).ToArray();
            if (motions.Length != 2) throw new InvalidOperationException("Expected two source control motion bindings.");

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(LandscapeUndo);
            try
            {
                Undo.RegisterFullObjectHierarchyUndo(flow.gameObject, LandscapeUndo);
                foreach (var row in rows)
                {
                    var parent = nodes[row.parentId].transform;
                    if (nodes.TryGetValue(row.sourceId, out var placed))
                    {
                        if (placed.transform.parent != parent)
                            throw new InvalidOperationException("Unexpected saved parent for control " + row.sourceId);
                        continue;
                    }
                    nodes.Add(row.sourceId, RestoreNode(row, parent, row.sourceSiblingIndex, null));
                }
                foreach (var row in rows.Where(row => !existing.Contains(row.sourceId)))
                    RestoreWiring(nodes[row.sourceId], row, nodes);

                // The combat controls sit underneath the five existing native overlay siblings.
                nodes[CombatMobile].transform.SetAsFirstSibling();
                var mobileRoot = nodes[559].transform;
                int chromeIndex = nodes[820].transform.GetSiblingIndex();
                if (mobileRoot.GetSiblingIndex() > chromeIndex) mobileRoot.SetSiblingIndex(chromeIndex);
                var beachMembers = new[] { 37, 165, 1405, 172, 548, 140, 865, 626, 972, 601 };
                for (int i = 0; i < beachMembers.Length; i++) nodes[beachMembers[i]].transform.SetSiblingIndex(i);
                nodes[532].transform.SetAsFirstSibling();

                var view = Component<BeachNativeMobileControls>(flow.gameObject);
                view.beachJoystick = BindJoystick(nodes, canvas, 1405, 1407, 1406, 165);
                view.combatJoystick = BindJoystick(nodes, canvas, 1000885, 1000887, 1000886, 1000097);
                view.combatRoot = nodes[CombatMobile].transform.gameObject;
                view.combatMoveTutorial = nodes[1000364].transform.gameObject;
                view.blockKeyboardPrompt = nodes[1000019].transform.gameObject;
                view.punchKeyboardPrompt = nodes[1000205].transform.gameObject;

                CopyControlPose(nodes[1000086].transform, nodes[1000320].transform);
                CopyControlPose(nodes[1000444].transform, nodes[1000589].transform);
                CopyControlPose(nodes[1000364].transform, nodes[1000207].transform);
                var legacyHand = nodes[1110].transform.GetComponent<NativeStationTouch>();
                if (legacyHand) legacyHand.enabled = false;
                workout.pulseEveryPointerDown = true;
                BindClick(green, view, view.Interact);
                if (!green.GetComponent<EventTrigger>())
                    RestoreAudioEvents(nodes[1057], source.nodes.Single(row => row.sourceId == 1057), nodes);
                BindTrigger(nodes[1110], Entry(EventTriggerType.PointerDown, view.Interact));
                BindClick(hand, view, view.InteractUp);
                BindTrigger(nodes[532], Entry(EventTriggerType.PointerDown, view.Jump));
                BindClick(nodes[532].button, view, view.Jump);
                BindTrigger(nodes[1000308], Entry(EventTriggerType.PointerDown, view.PressBlock),
                    Entry(EventTriggerType.PointerUp, view.ReleaseBlock));
                BindTrigger(nodes[1000450], Entry(EventTriggerType.PointerDown, view.PressPunch),
                    Entry(EventTriggerType.PointerUp, view.ReleasePunch));
                foreach (int id in new[] { 865, 1000491 })
                    BindTrigger(nodes[id], Entry(EventTriggerType.PointerDown), Entry(EventTriggerType.PointerUp));

                nodes[140].transform.gameObject.SetActive(false);
                nodes[1110].transform.gameObject.SetActive(false);
                view.combatRoot.SetActive(false);
                mobileRoot.gameObject.SetActive(false);
                BindControlMotion(flow, nodes, motions);
                peripheral.nodes = peripheral.nodes.Concat(rows.Where(row => !existing.Contains(row.sourceId))
                    .Select(row => nodes[row.sourceId])).ToArray();
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
        }

        private static BeachNativeFloatingJoystick BindJoystick(Dictionary<int, NativeUiNode> nodes, Canvas canvas,
            int id, int bg, int handle, int marker)
        {
            var joystick = Component<BeachNativeFloatingJoystick>(nodes[id].transform.gameObject);
            joystick.canvas = canvas;
            joystick.background = nodes[bg].transform;
            joystick.handle = nodes[handle].transform;
            joystick.landscape = nodes[marker].transform;
            joystick.ApplyLandscapePose();
            joystick.ResetInput();
            return joystick;
        }

        private static void CopyControlPose(RectTransform target, RectTransform marker)
        {
            target.position = marker.position;
            target.localScale = marker.localScale;
        }

        private static void BindClick(Button button, BeachNativeMobileControls view, UnityAction callback)
        {
            for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                if (button.onClick.GetPersistentTarget(i) == view)
                    UnityEventTools.RemovePersistentListener(button.onClick, i);
            UnityEventTools.AddPersistentListener(button.onClick, callback);
        }

        private static EventTrigger.Entry Entry(EventTriggerType type, UnityAction<BaseEventData> callback)
        {
            var entry = Entry(type);
            UnityEventTools.AddPersistentListener(entry.callback, callback);
            return entry;
        }

        private static EventTrigger.Entry Entry(EventTriggerType type, UnityAction callback)
        {
            var entry = Entry(type);
            UnityEventTools.AddVoidPersistentListener(entry.callback, callback);
            return entry;
        }

        private static EventTrigger.Entry Entry(EventTriggerType type) => new EventTrigger.Entry { eventID = type };

        private static void BindTrigger(NativeUiNode node, params EventTrigger.Entry[] entries) =>
            Component<EventTrigger>(node.transform.gameObject).triggers = entries.ToList();

        private static void BindControlMotion(BeachGameFlow flow, Dictionary<int, NativeUiNode> nodes,
            (NativeUiMotionBinding binding, AnimationClip clip)[] source)
        {
            var motion = Component<BeachNativeUiMotion>(flow.gameObject);
            var newMotion = source.Select(item => new BeachNativeUiMotion.Binding
            {
                sourceId = item.binding.sourceAnimatorId,
                target = nodes[item.binding.targetId].transform,
                clip = item.clip,
                duration = item.clip.length,
                loop = item.binding.loop
            });
            var targets = new HashSet<Transform>(source.Select(item => nodes[item.binding.targetId].transform));
            motion.bindings = motion.bindings.Where(binding => binding == null || !targets.Contains(binding.target))
                .Concat(newMotion).ToArray();
        }
    }
}

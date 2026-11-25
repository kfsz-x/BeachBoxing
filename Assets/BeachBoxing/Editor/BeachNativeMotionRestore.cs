using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeachBoxing.Editor
{
    public static class BeachNativeMotionRestore
    {
        [MenuItem("Beach Boxing/Bind original decorative UI motion")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated Beach Boxing scene.");
            var peripheral = flow.GetComponent<BeachPeripheralView>();
            if (!peripheral) throw new InvalidOperationException("Append the original UI before binding its motion.");
            var ownedByViews = new[] { 80, 204, 457, 602, 679, 757, 61, 438, 933 };
            var motion = flow.GetComponent<BeachNativeUiMotion>();
            if (!motion) motion = Undo.AddComponent<BeachNativeUiMotion>(flow.gameObject);
            Undo.RecordObject(motion, "Bind native UI animation references");
            motion.bindings = BeachRemainingUiRestore.MotionBindings()
                .Where(binding => binding.enabled && !ownedByViews.Contains(binding.targetId))
                .Where(binding => !BeachPeripheralRestore.IsLandscapeControlNode(binding.targetId) ||
                    peripheral.nodes.Any(node => node.sourceId == binding.targetId))
                .Select(binding =>
                {
                    var target = peripheral.nodes.Single(node => node.sourceId == binding.targetId).transform;
                    var clip = BeachRemainingUiRestore.Clip(binding.defaultClipId);
                    return new BeachNativeUiMotion.Binding
                    {
                        sourceId = binding.sourceAnimatorId, target = target, clip = clip,
                        duration = clip.length, loop = binding.loop, unscaled = binding.updateMode == 2
                    };
                }).ToArray();
            EditorUtility.SetDirty(motion);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
        }
    }
}

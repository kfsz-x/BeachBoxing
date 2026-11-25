using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeachBoxing.Editor
{
    /// <summary>Explicit editor binding; preserves every existing source transform and artwork component.</summary>
    public static class BeachTrainingUiRestore
    {
        [MenuItem("Beach Boxing/Bind original training and tutorial UI")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            var flow = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BeachGameFlow>(true)).SingleOrDefault();
            if (EditorApplication.isPlayingOrWillChangePlaymode || !flow)
                throw new InvalidOperationException("Open the Beach Boxing gameplay scene in Edit Mode.");
            var peripheral = flow.GetComponent<BeachPeripheralView>();
            if (!peripheral) throw new InvalidOperationException("Append the original native UI first.");
            var nodes = peripheral.nodes.Where(node => node != null && node.transform).ToDictionary(node => node.sourceId);
            foreach (int id in new[] { 881, 159, 402, 910, 377, 804, 28, 139, 937, 1119, 513, 3, 314, 372, 601, 559, 1014, 455, 308 })
                if (!nodes.ContainsKey(id)) throw new InvalidOperationException("Missing original UI node " + id + ". Append the remaining UI first.");

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Bind original training and tutorial UI");
            try
            {
                var training = Component<BeachNativeTrainingView>(flow.gameObject);
                var tutorial = Component<BeachNativeTutorialView>(flow.gameObject);
                var pulse = Component<TouchControl>(nodes[804].transform.gameObject);
                Undo.RecordObject(pulse, "Bind source workout pointer pulses");
                pulse.pulseEveryPointerDown = true;
                Undo.RecordObject(training, "Bind original workout controls");
                Undo.RecordObject(tutorial, "Bind original tutorial hints");
                training.peripheral = peripheral;
                training.pulseControl = pulse;
                tutorial.peripheral = peripheral;
                EditorUtility.SetDirty(training);
                EditorUtility.SetDirty(tutorial);
                EditorUtility.SetDirty(pulse);
                EditorSceneManager.MarkSceneDirty(scene);
                Undo.CollapseUndoOperations(group);
                Debug.Log("Bound original workout controls and tutorial hints without changing UI transforms or artwork.");
            }
            catch { Undo.RevertAllDownToGroup(group); throw; }
        }

        private static T Component<T>(GameObject target) where T : Component =>
            target.GetComponent<T>() ?? Undo.AddComponent<T>(target);
    }
}

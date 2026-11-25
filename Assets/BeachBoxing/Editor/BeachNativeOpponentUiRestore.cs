using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BeachBoxing.Editor
{
    public static class BeachNativeOpponentUiRestore
    {
        [Serializable] private sealed class Mapping { public Gauntlet[] gauntlets; }
        [Serializable] private sealed class Gauntlet { public string title; }

        [MenuItem("Beach Boxing/Bind complete original opponent menu")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode first.");
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            if (!flow) throw new InvalidOperationException("Open the populated Beach Boxing scene.");
            var view = flow.GetComponent<BeachNativeOpponentView>();
            if (!view) view = Undo.AddComponent<BeachNativeOpponentView>(flow.gameObject);
            Undo.RecordObject(view, "Bind source FightMenu457");
            view.peripheral = flow.GetComponent<BeachPeripheralView>();
            view.catalog = flow.GetComponent<NativeOpponentCatalog>();
            view.menuOpen = BeachRemainingUiRestore.Clip(329);
            view.menuClose = BeachRemainingUiRestore.Clip(327);
            var mapping = JsonUtility.FromJson<Mapping>(File.ReadAllText(
                ".superpowers/sdd/2026-11-20-remaining-native-parity/workspace/fight-menu-457-view-mapping.json"));
            view.gauntletTitles = mapping.gauntlets.Select(row => row.title).ToArray();
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(flow.gameObject.scene);
        }
    }
}

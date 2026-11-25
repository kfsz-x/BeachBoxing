using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeRosterFlowTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator EveryPlayableSelectionStartsItsOwnPreplacedModel()
        {
            return WithProgress(CheckModels);
        }

        [UnityTest]
        public IEnumerator FinalVictoryBoundaryShowsComingSoonAndCannotStartAnotherFight()
        {
            return WithProgress(CheckBoundary);
        }

        private static IEnumerator CheckModels(BeachGameFlow flow)
        {
            var catalog = flow.GetComponent<NativeOpponentCatalog>();
            Assert.That(catalog, Is.Not.Null);
            for (int index = 0; index < 45; index++)
            {
                typeof(BeachGameFlow).GetField("opponentIndex", Flags).SetValue(flow, index);
                typeof(BeachGameFlow).GetMethod("EnterFight", Flags).Invoke(flow, null);
                Assert.That(catalog.Models[index].gameObject.activeInHierarchy, Is.True,
                    "Selection must activate original opponent " + index);
                for (int other = 0; other < 45; other++)
                    Assert.That(catalog.Models[other].gameObject.activeSelf, Is.EqualTo(index == other));
                Assert.That(catalog.Models[index].GetComponentInChildren<Animator>().runtimeAnimatorController, Is.Not.Null);
            }
            yield return null;
        }

        private static IEnumerator CheckBoundary(BeachGameFlow flow)
        {
            typeof(BeachGameFlow).GetField("phase", Flags).SetValue(flow, BeachPhase.OpponentSelect);
            typeof(BeachGameFlow).GetField("opponentIndex", Flags).SetValue(flow, 45);
            yield return null;
            Assert.That((int)typeof(BeachGameFlow).GetMethod("LastUnlocked", Flags).Invoke(flow, null), Is.EqualTo(45));
            typeof(BeachGameFlow).GetField("focused", Flags).SetValue(flow, true);
            typeof(BeachGameFlow).GetMethod("BeginFight", Flags).Invoke(flow, null);
            Assert.That(typeof(BeachGameFlow).GetField("pendingTransition", Flags).GetValue(flow), Is.Null,
                "COMING SOON has no original combat prefab and must not start a transition.");
            Assert.That(typeof(BeachGameFlow).GetField("phase", Flags).GetValue(flow), Is.EqualTo(BeachPhase.OpponentSelect));
        }

        private static IEnumerator WithProgress(Func<BeachGameFlow, IEnumerator> run)
        {
            var core = CoreRoot.GetOrCreate();
            var storage = new LocalStore("BeachBoxing.NativeRosterFlow." + Guid.NewGuid());
            var storageProperty = typeof(CoreRoot).GetProperty(nameof(CoreRoot.Storage));
            var previousStorage = core.Storage;
            float previousScale = Time.timeScale;
            BeachGameFlow flow = null;
            try
            {
                new ProgressStore(storage).Save(new PlayerProgress { unlockedOpponent = 45, playerName = "Roster Test" });
                storageProperty.SetValue(core, storage);
                Time.timeScale = 1;
                yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
                yield return null;
                flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
                Assert.That(flow, Is.Not.Null);
                storageProperty.SetValue(core, previousStorage);
                typeof(BeachGameFlow).GetField("focused", Flags).SetValue(flow, true);
                yield return run(flow);
            }
            finally
            {
                storageProperty.SetValue(core, previousStorage);
                if (flow) UnityEngine.Object.Destroy(flow.gameObject);
                storage.Delete("beachBoxing.progress");
                storage.Delete("beachBoxing.progress.backup");
                storage.Save();
                Time.timeScale = previousScale;
            }
        }
    }
}

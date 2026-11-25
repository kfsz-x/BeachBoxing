using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeOpponentMenuTests
    {
        [UnityTest]
        public IEnumerator FullSourceMenuUsesEveryPortraitAndComingSoonRewards()
        {
            yield return NativePlayModeScene.WithScene(true, (flow, store) => Exercise(flow));
        }

        private static IEnumerator Exercise(BeachGameFlow flow)
        {
            flow.enabled = false;
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.BeachNativeOpponentView");
            Assert.That(type, Is.Not.Null, "Original457 picker requires a bound state owner.");
            var view = flow.GetComponent(type);
            Assert.That(view, Is.Not.Null);
            var native = flow.GetComponent<BeachPeripheralView>();
            var catalog = flow.GetComponent<NativeOpponentCatalog>();
            var progress = new PlayerProgress { unlockedOpponent = 45 };
            for (int i = 0; i <= 45; i++)
            {
                type.GetMethod("Render").Invoke(view, new object[] { true, progress, i });
                Assert.That(native.Node(332).label.text, Is.EqualTo(BeachChineseText.Translate(i == 45 ? "COMING SOON" : catalog.Profiles[i].name)));
                Assert.That(native.Node(813).image.sprite, Is.SameAs(i == 45 ? catalog.ComingSoonPortrait : catalog.Portraits[i]));
                Assert.That(native.Node(1171).label.text, Is.EqualTo((50 * (i + 1)).ToString()));
                Assert.That(native.Node(51).label.text, Is.EqualTo((3 * (i + 1)).ToString()));
                Assert.That(native.Node(806).transform.gameObject.activeSelf, Is.EqualTo(i < 45));
                Assert.That(native.Node(553).button.interactable, Is.EqualTo(i > 0));
                Assert.That(native.Node(965).button.interactable, Is.EqualTo(i < 45));
            }
            Assert.That(native.Node(150).label.text, Is.EqualTo("拳坛传奇挑战赛"));
            Assert.That(native.Node(355).label.text, Is.EqualTo("0"));
            progress.unlockedOpponent = 7;
            type.GetMethod("Render").Invoke(view, new object[] { true, progress, 6 });
            Assert.That(native.Node(150).label.text, Is.EqualTo("业余挑战赛"));
            Assert.That(native.Node(670).image.color.a, Is.EqualTo(1));
            Assert.That(native.Node(728).image.color.a, Is.EqualTo(.2f).Within(.00001f));
            Assert.That(native.Node(728).image.color.r, Is.EqualTo(1));
            type.GetMethod("Render").Invoke(view, new object[] { true, progress, 2 });
            Assert.That(native.Node(728).image.color.a, Is.EqualTo(1), "Earlier bracket is fully completed.");
            yield return null;
        }
    }
}

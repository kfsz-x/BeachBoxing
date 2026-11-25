using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeBoxingAnimationTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [UnityTest]
        public IEnumerator ResultFreezesOnlyLoserAndReentryRestoresDynamicBodies()
        {
            yield return Load();
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            var stage = typeof(BeachGameFlow).GetField("stage", Flags).GetValue(flow);
            var winner = stage.GetType().GetField("Player", Flags).GetValue(stage);
            var loser = stage.GetType().GetField("Enemy", Flags).GetValue(stage);
            var winnerBody = ((Transform)winner.GetType().GetField("Root", Flags).GetValue(winner)).GetComponent<Rigidbody>();
            var loserBody = ((Transform)loser.GetType().GetField("Root", Flags).GetValue(loser)).GetComponent<Rigidbody>();
            var momentum = new Vector3(1, 2, 3);
            winnerBody.velocity = momentum;
            Call(winner, "Result", true);
            Call(loser, "Result", false);
            Assert.That(winnerBody.isKinematic, Is.False);
            Assert.That(winnerBody.velocity, Is.EqualTo(momentum), "Source Victory does not erase the winning body's velocity.");
            Assert.That(loserBody.isKinematic, Is.True, "Source Die freezes only the losing Rigidbody.");
            typeof(BeachGameFlow).GetMethod("EnterFight", Flags).Invoke(flow, null);
            Assert.That(winnerBody.isKinematic, Is.False);
            Assert.That(loserBody.isKinematic, Is.False);
        }

        [UnityTest]
        public IEnumerator SourceParametersDriveBlockingAndTimedTakingDamage()
        {
            yield return Load();
            object boxer = Player();
            var fighter = new FighterState(50, 50, 20);
            fighter.Tick(0, true);
            Call(boxer, "Fight", fighter, 0f);
            var animator = (Animator)boxer.GetType().GetField("animator", Flags).GetValue(boxer);
            Assert.That(animator.GetBool("Blocking"), Is.True, "Native controller must own the .15s block transition.");
            fighter.Tick(0, false);
            var attacker = new FighterState(50, 50, 20);
            attacker.Punch(1);
            attacker.Tick(.1f, false);
            Assert.That(attacker.TryLand(fighter, true, 3), Is.True);
            Call(boxer, "Fight", fighter, .16f);
            Assert.That(animator.GetBool("TakingDamage"), Is.True);
            yield return new WaitForSeconds(.32f);
            Call(boxer, "Fight", fighter, 0f);
            Assert.That(animator.GetBool("TakingDamage"), Is.False);
        }

        [UnityTest]
        public IEnumerator VictoryDoesNotRequestUnusedPhotoShootState()
        {
            yield return Load();
            object boxer = Player();
            Call(boxer, "Fight", new FighterState(50, 50, 20), 0f);
            var animator = (Animator)boxer.GetType().GetField("animator", Flags).GetValue(boxer);
            animator.Play("Boxing_Idle", 0, 0);
            animator.Update(0);
            Call(boxer, "Result", true);
            animator.Update(.2f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Boxing_Idle"), Is.True,
                "PhotoShoot has no source combat caller or component instance.");
        }

        private static IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            flow.enabled = false;
            typeof(BeachGameFlow).GetMethod("EnterFight", Flags).Invoke(flow, null);
        }

        private static object Player()
        {
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            var stage = typeof(BeachGameFlow).GetField("stage", Flags).GetValue(flow);
            return stage.GetType().GetField("Player", Flags).GetValue(stage);
        }
        private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Flags).Invoke(target, args);
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class TrainerFootstepAudioTests
    {
        [UnityTest]
        public IEnumerator BothWalkingTrainersPlayTheirBoundFootstepsThroughAnimationEvents()
        {
            yield return NativePlayModeScene.WithScene(true, (flow, storage) => Exercise(flow));
        }

        private static IEnumerator Exercise(BeachGameFlow flow)
        {
            flow.enabled = false;
            flow.GetComponent<BeachTrainerFollowers>().enabled = false;
            var models = NativePlayModeScene.Field<BeachTrainerFollowers.Model[]>(
                flow.GetComponent<BeachTrainerFollowers>(), "models");

            for (int index = 0; index < 2; index++)
            {
                var model = models[index];
                var receiver = model.animator.GetComponent<BeachNativeBoxingAudio>();
                Assert.That(receiver, Is.Not.Null, "Walking trainer " + index + " requires its animation-event receiver.");
                Assert.That(receiver.step1, Is.Not.Null);
                Assert.That(receiver.step2, Is.Not.Null);
                Assert.That(receiver.step1.clip, Is.Not.Null);
                Assert.That(receiver.step2.clip, Is.EqualTo(receiver.step1.clip));
                Assert.That(receiver.step1.outputAudioMixerGroup, Is.Not.Null);
                Assert.That(receiver.step2.outputAudioMixerGroup, Is.EqualTo(receiver.step1.outputAudioMixerGroup));
                Assert.That(receiver.jump, Is.Null);
                Assert.That(receiver.land, Is.Null);
                foreach (var channel in new[] { receiver.step1, receiver.step2 })
                {
                    Assert.That(channel.spatialBlend, Is.EqualTo(1));
                    Assert.That(channel.volume, Is.EqualTo(.16f).Within(.00001f));
                    Assert.That(channel.minDistance, Is.EqualTo(.356f).Within(.00001f));
                    Assert.That(channel.maxDistance, Is.EqualTo(35.6f).Within(.00001f));
                }

                model.root.gameObject.SetActive(true);
                model.animator.enabled = false;
                Assert.That(model.animator.fireEvents, Is.True);
                Assert.That(receiver.step1.pitch, Is.InRange(.7f, 1.1f));
                Assert.That(receiver.step2.pitch, Is.InRange(.7f, 1.1f));
                model.animator.SetBool("Grounded", true);
                model.animator.SetBool("Walking", true);
                model.animator.Play("Player_Walk", 0, 0);
                model.animator.Update(0);
                receiver.step1.Stop();
                receiver.step2.Stop();
                model.animator.Update(.15f);
                Assert.That(receiver.step1.isPlaying, Is.True, "First footstep must be dispatched by the walk clip.");
                Assert.That(receiver.step2.isPlaying, Is.False);
                receiver.step1.Stop();
                model.animator.Update(.20f);
                Assert.That(receiver.step2.isPlaying, Is.True, "Second footstep must be dispatched by the walk clip.");
                receiver.step2.Stop();
                model.root.gameObject.SetActive(false);
            }

            for (int index = 2; index < models.Length; index++)
                Assert.That(models[index].animator.GetComponent<BeachNativeBoxingAudio>(), Is.Null);
            yield return null;
        }
    }
}

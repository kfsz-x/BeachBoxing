using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BeachBoxing.Tests
{
    public sealed class NativeBeachJumpTests
    {
        [UnityTest]
        public IEnumerator BeachMovementUsesSourceRigidbodyVelocity()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            flow.enabled = false;
            var body = flow.transform.Find("Player").GetComponent<Rigidbody>();
            body.position += Vector3.up * 10;
            var stage = typeof(BeachGameFlow).GetField("stage", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(flow);
            stage.GetType().GetMethod("WalkPlayer").Invoke(stage, new object[] { Vector2.right * .25f, .016f });
            yield return new WaitForFixedUpdate();
            var horizontal = new Vector2(body.velocity.x, body.velocity.z);
            Assert.That(horizontal.magnitude, Is.EqualTo(35f * .356f).Within(.001f),
                "Original Move sets normalized camera-relative Rigidbody velocity and leaves collision response to physics.");
        }

        [UnityTest]
        public IEnumerator SourceBodyAndJumpRiseThenLandWithoutGroundPinning()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var flow = UnityEngine.Object.FindObjectOfType<BeachGameFlow>();
            var type = typeof(BeachGameFlow).Assembly.GetType("BeachBoxing.BeachNativeLocomotion");
            Assert.That(type, Is.Not.Null, "Source jump/grounding controller must exist.");
            var motor = flow.GetComponentInChildren(type, true);
            Assert.That(motor, Is.Not.Null, "Jump controller and capsule must be preplaced.");
            var body = motor.GetComponent<Rigidbody>();
            var capsule = motor.transform.Find("PlayerCollider").GetComponent<CapsuleCollider>();
            Assert.That(capsule.radius, Is.EqualTo(0.75f * 0.356f).Within(0.00001f));
            Assert.That(capsule.height, Is.EqualTo(5.0137600899f * 0.356f).Within(0.00001f));
            Assert.That(body.useGravity, Is.True);
            Assert.That(Physics.gravity.y, Is.EqualTo(-80f * 0.356f).Within(0.00001f));
            Assert.That(Time.fixedDeltaTime, Is.EqualTo(0.033f).Within(0.000001f));
            yield return new WaitForSeconds(0.2f);
            float start = motor.transform.position.y;
            type.GetMethod("RequestJump").Invoke(motor, null);
            yield return new WaitForSeconds(0.2f);
            Assert.That(motor.transform.position.y - start, Is.GreaterThan(0.7f), "GroundPlayer must not pin an airborne actor.");
            Assert.That((bool)type.GetProperty("Grounded").GetValue(motor), Is.False);
            yield return new WaitForSeconds(1.1f);
            Assert.That(motor.transform.position.y, Is.EqualTo(start).Within(0.08f));
            Assert.That((bool)type.GetProperty("Grounded").GetValue(motor), Is.True);
        }
    }
}

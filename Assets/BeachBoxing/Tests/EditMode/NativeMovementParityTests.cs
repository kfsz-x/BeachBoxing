using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativeMovementParityTests
    {
        [Test]
        public void BeachAnalogDirectionUsesNativeNormalizedSpeed()
        {
            var actor = new GameObject("native movement fixture");
            try
            {
                var start = new Vector3(1000, 1000, 1000);
                var bounds = new Rect(900, 900, 200, 200);
                actor.transform.position = start;
                BeachMotor.Walk(actor.transform, new Vector2(0, .25f), 35f * .356f, .01f, bounds);
                float partial = Vector3.Distance(start, actor.transform.position);
                actor.transform.position = start;
                BeachMotor.Walk(actor.transform, Vector2.up, 35f * .356f, .01f, bounds);
                Assert.That(partial, Is.EqualTo(Vector3.Distance(start, actor.transform.position)).Within(.0001f));
            }
            finally { Object.DestroyImmediate(actor); }
        }

        [Test]
        public void BeachTurnUsesSourceQuaternionLerpTiming()
        {
            var actor = new GameObject("native rotation fixture");
            try
            {
                actor.transform.position = new Vector3(1000, 1000, 1000);
                BeachMotor.Walk(actor.transform, Vector2.right, 35f * .356f, .01f, new Rect(900, 900, 200, 200));
                var expected = Quaternion.Lerp(Quaternion.identity, Quaternion.LookRotation(Vector3.right), .01f * 25f);
                Assert.That(Quaternion.Angle(actor.transform.rotation, expected), Is.LessThan(.001f));
            }
            finally { Object.DestroyImmediate(actor); }
        }
    }
}

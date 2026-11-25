using System;
using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class LateVivoAdEventsTests
    {
        [TestCase("AdOnErrorCallBack")]
        [TestCase("AdOnLoadCallBack")]
        [TestCase("RewardedVideoAdOnCloseCallBack")]
        [TestCase("RewardedVideoAdOnRewardCallBack")]
        public void APlatformEventForAReleasedAdIsIgnored(string callback)
        {
            var type = Type.GetType("QGMiniGame.QGMiniGameManager, VIVO-Game-SDK");
            Assert.That(type, Is.Not.Null);
            var holder = new GameObject("Late vivo ad event probe");
            try
            {
                var manager = holder.AddComponent(type);
                var method = type.GetMethod(callback);
                Assert.That(method, Is.Not.Null);
                Assert.DoesNotThrow(() => method.Invoke(manager,
                    new object[] { "{\"callbackId\":\"BeachBoxing-released-ad-probe\",\"isEnded\":true}" }));
            }
            finally { UnityEngine.Object.DestroyImmediate(holder); }
        }
    }
}

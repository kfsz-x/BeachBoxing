using NUnit.Framework;
using UnityEngine;

namespace BeachBoxing.Tests
{
    public sealed class NativePrestigeGeometryTests
    {
        // Literal separations are independently checked against shipped capsule/sphere transforms:
        // enemy radius=2.25, player detached punch radius=2.092315; world scale=.356.
        [TestCase(3, 2.1f)]
        [TestCase(44, 2.6f)]
        public void PlayerPunchCanReachTheSourcePrestigeScaledEnemyCapsule(int opponentIndex, float separation)
        {
            WithStationaryFight(opponentIndex, 1, separation, (fight, player, enemy) =>
            {
                fight.Player.Punch(0);
                fight.Tick(.11f, default);
                Assert.That(fight.Enemy.ImpactCount, Is.EqualTo(1),
                    "The original enemy capsule inherits its prefab root prestige scale.");
            });
        }

        [Test]
        public void HighPrestigePlayerPunchUsesItsScaledSourceAnchorHeight()
        {
            WithStationaryFight(0, 10, 1.7f, (fight, player, enemy) =>
            {
                fight.Player.Punch(0);
                fight.Tick(.11f, default);
                Assert.That(fight.Enemy.ImpactCount, Is.Zero,
                    "At player scale 2.1 the source anchor is above this level-one capsule; the detached sphere radius stays fixed.");
            });
        }

        [Test]
        public void EnemyPunchSphereInheritsTheFinalRematchRootScale()
        {
            WithStationaryFight(44, 1, 2.6f, (fight, player, enemy) =>
            {
                fight.Enemy.Punch(0);
                fight.Tick(.08f, default);
                Assert.That(fight.Player.ImpactCount, Is.EqualTo(1),
                    "The final enemy's attached punch sphere inherits its 2.1 root scale.");
            });
        }

        [Test]
        public void PreplacedEnemyActorCapsuleChangesToTheSelectedNativePrestigeGeometry()
        {
            WithStationaryFight(44, 1, 100, (fight, player, enemy) =>
            {
                var capsule = enemy.GetComponent<CapsuleCollider>();
                Assert.That(capsule.radius, Is.EqualTo(1.6821f).Within(.0001f));
                Assert.That(capsule.height, Is.EqualTo(5.36171f).Within(.0001f));
                Assert.That(capsule.center.y, Is.EqualTo(1.936284f).Within(.0001f));
            });
        }

        [Test]
        public void MinimumSeparationUsesBothScaledSourceBodyRadii()
        {
            WithStationaryFight(44, 10, 1, (fight, player, enemy) =>
            {
                fight.Tick(.02f, default);
                Assert.That(Vector3.Distance(player.position, enemy.position), Is.EqualTo(2.2428f).Within(.0001f));
            });
        }

        static void WithStationaryFight(int index, int playerPrestige, float separation,
            System.Action<FightController, Transform, Transform> verify)
        {
            var player = new GameObject("prestige geometry player test");
            var enemy = new GameObject("prestige geometry enemy test");
            try
            {
                enemy.AddComponent<CapsuleCollider>(); // authored scene actor collider; runtime must configure, not create it.
                player.transform.localScale = Vector3.one * BeachProgressionRules.RootScale(playerPrestige);
                enemy.transform.position = Vector3.forward * 100;
                var profile = OpponentProfile.RookieRoster()[index];
                // Isolate hit geometry from AI movement; all source collider data remains intact.
                profile.ai.speed = profile.ai.blockSpeed = 0;
                var fight = new FightController(player.transform, enemy.transform,
                    new PlayerProgress { prestigeLevel = playerPrestige }, profile, new Rect(-200, -200, 400, 400), 7);
                fight.Tick(5f, default);
                player.transform.position = Vector3.zero;
                enemy.transform.position = Vector3.forward * separation;
                verify(fight, player.transform, enemy.transform);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(enemy);
            }
        }
    }
}

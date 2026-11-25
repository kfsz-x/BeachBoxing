using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace BeachBoxing.Tests
{
    public sealed class OriginalArtTests
    {
        [UnityTest]
        public IEnumerator BeachAndFightersUseRecoveredAssets()
        {
            yield return SceneManager.LoadSceneAsync("Assets/BeachBoxing/Scenes/BeachBoxing.unity");
            yield return null;
            var player = GameObject.Find("Player");
            Assert.That(player.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.GreaterThan(0));
            Assert.That(player.GetComponentInChildren<Animator>(true).runtimeAnimatorController, Is.Not.Null);
            Assert.That(GameObject.Find("Training beach").transform.Find("Original Workout world"), Is.Not.Null);
            var terrain = GameObject.Find("Training beach").GetComponentInChildren<Terrain>(true);
            Assert.That(terrain, Is.Not.Null);
            Assert.That(terrain.terrainData.heightmapResolution, Is.EqualTo(513));
            Assert.That(terrain.terrainData.terrainLayers[0].diffuseTexture.name, Is.EqualTo("Sand"));
            Assert.That(GameObject.Find("Safe area").transform.Find("PUNCH/Icon").GetComponent<Image>().sprite.name, Is.EqualTo("PunchIcon"));
            var fighters = GameObject.Find("BeachBoxing").transform;
            Assert.That(fighters.Find("Opponent").GetComponentsInChildren<ParticleSystem>(true).Length, Is.GreaterThan(0));
            Assert.That(Resources.Load<AudioClip>("BeachOriginal/Audio/Punch"), Is.Not.Null);
        }
    }
}

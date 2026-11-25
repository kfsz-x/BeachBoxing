using NUnit.Framework;
using UnityCore;

namespace BeachBoxing.Tests
{
    public sealed class ChineseNameCompatibilityTests
    {
        [TestCase("{\"version\":1,\"playerName\":\"Back\",\"generatedName\":true}", true)]
        [TestCase("{\"version\":1,\"playerName\":\"Cool Cat\",\"generatedName\":true}", false)]
        [TestCase("{\"version\":1,\"playerName\":\"Cool Cat\",\"generatedName\":true,\"customPlayerName\":false}", false)]
        [TestCase("{\"version\":1,\"playerName\":\"Cozy Undies\",\"generatedName\":true,\"customPlayerName\":true}", false)]
        [TestCase("{\"version\":2,\"playerName\":\"Cozy Undies\",\"generatedName\":true,\"customPlayerName\":true}", true)]
        public void LegacyGeneratedNamesRecoverWhileExplicitNewCustomNamesStayLiteral(string json, bool literal)
        {
            var storage = new LocalStore("BeachBoxing.ChineseNames." + System.Guid.NewGuid());
            try
            {
                storage.SetString("beachBoxing.progress", json);
                var progress = new ProgressStore(storage).Load();
                Assert.That(progress.customPlayerName, Is.EqualTo(literal));
                Assert.That(progress.version, Is.EqualTo(2));
                Assert.That(storage.GetString("beachBoxing.progress"), Is.EqualTo(json), "Loading must not rewrite the existing save.");
            }
            finally { storage.Delete("beachBoxing.progress"); }
        }
    }
}

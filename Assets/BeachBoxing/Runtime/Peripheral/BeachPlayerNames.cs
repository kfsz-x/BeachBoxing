using System;
using UnityEngine;

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachPlayerNames : MonoBehaviour
    {
        [SerializeField] private string[] adjectives = Array.Empty<string>();
        [SerializeField] private string[] nouns = Array.Empty<string>();

        public string Generate()
        {
            if (adjectives.Length == 0 || nouns.Length == 0) return string.Empty;
            var adjective = adjectives[UnityEngine.Random.Range(0, adjectives.Length)];
            var noun = nouns[UnityEngine.Random.Range(0, nouns.Length)];
            return adjective + " " + noun;
        }
    }
}

using System;
using UnityEngine;

namespace BeachBoxing
{
    // Scene-owned references. Runtime selection only switches these preplaced objects.
    [DisallowMultipleComponent]
    public sealed class NativeOpponentCatalog : MonoBehaviour
    {
        public const int PlayableCount = 45;
        public const int ComingSoonIndex = 45;
        [SerializeField] private OpponentProfile[] profiles = Array.Empty<OpponentProfile>();
        [SerializeField] private Transform[] models = Array.Empty<Transform>();
        [SerializeField] private Sprite[] portraits = Array.Empty<Sprite>();
        [SerializeField] private Sprite comingSoonPortrait;
        [SerializeField] private Sprite[] gauntletBackgrounds = Array.Empty<Sprite>();
        public OpponentProfile[] Profiles => profiles;
        public Transform[] Models => models;
        public Sprite[] Portraits => portraits;
        public Sprite ComingSoonPortrait => comingSoonPortrait;
        public Sprite[] GauntletBackgrounds => gauntletBackgrounds;

#if UNITY_EDITOR
        public void Bind(OpponentProfile[] data, Transform[] sceneModels, Sprite[] icons, Sprite comingSoon, Sprite[] backgrounds)
        {
            if (data.Length != PlayableCount || sceneModels.Length != PlayableCount || icons.Length != PlayableCount)
                throw new ArgumentException("The native catalog requires exactly 45 playable opponents.");
            for (int i = 0; i < PlayableCount; i++)
                if (data[i].sourceIndex != i || !sceneModels[i] || !icons[i])
                    throw new ArgumentException("Missing or misordered native opponent " + i);
            profiles = data; models = sceneModels; portraits = icons;
            comingSoonPortrait = comingSoon; gauntletBackgrounds = backgrounds;
        }
#endif
    }
}

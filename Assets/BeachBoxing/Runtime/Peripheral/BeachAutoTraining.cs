using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using QGMiniGame;
#endif

namespace BeachBoxing
{
    [DisallowMultipleComponent]
    public sealed class BeachAutoTraining : MonoBehaviour
    {
        private const float RewardDuration = 120f;

        [SerializeField] private string vivoRewardedAdUnitId = "";
        private float remainingSeconds;

        public bool Enabled { get; private set; }
        public bool Accelerated => remainingSeconds > 0f;
        public int SecondsLeft => Mathf.CeilToInt(remainingSeconds);

        public void SetEnabled(bool value) => Enabled = value;

        public void Advance(float seconds)
        {
            if (seconds > 0f) remainingSeconds = Mathf.Max(0f, remainingSeconds - seconds);
        }

        public void ResolveReward(bool completed)
        {
            if (completed && !Accelerated) remainingSeconds = RewardDuration;
        }

        public void RequestAcceleration(Action<bool> completed = null)
        {
            if (Accelerated || !isActiveAndEnabled || string.IsNullOrWhiteSpace(vivoRewardedAdUnitId))
            {
                completed?.Invoke(false);
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            RequestVivoReward(completed);
#else
            completed?.Invoke(false);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private QGRewardedVideoAd pendingAd;
        private Action<bool> pendingCompletion;
        private Action<QGRewardedVideoResponse> closeHandler;
        private Action<QGBaseResponse> errorHandler;
        private bool previousAudioPause;

        private void RequestVivoReward(Action<bool> completed)
        {
            if (pendingAd != null)
            {
                completed?.Invoke(false);
                return;
            }

            QGRewardedVideoAd ad = null;
            try
            {
                if (QG.isVivo())
                    ad = QG.CreateRewardedVideoAd(new QGCommonAdParam { adUnitId = vivoRewardedAdUnitId.Trim() });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not create the vivo rewarded ad. {exception.Message}", this);
            }

            if (ad == null)
            {
                completed?.Invoke(false);
                return;
            }

            pendingAd = ad;
            pendingCompletion = completed;
            previousAudioPause = AudioListener.pause;
            AudioListener.pause = true;
            closeHandler = result => CompleteRequest(ad, result?.isEnded == true);
            errorHandler = _ => CompleteRequest(ad, false);
            ad.OnClose(closeHandler);
            ad.OnError(errorHandler);
            CallVivo(ad, () => ad.Load(_ => CallVivo(ad, () => ad.Show(failed: errorHandler)), errorHandler));
        }

        private void CallVivo(QGRewardedVideoAd ad, Action request)
        {
            if (pendingAd != ad) return;
            try
            {
                request();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"The vivo rewarded ad request failed. {exception.Message}", this);
                CompleteRequest(ad, false);
            }
        }

        private void CompleteRequest(QGRewardedVideoAd ad, bool ended)
        {
            if (pendingAd != ad) return;
            var completed = pendingCompletion;
            pendingAd = null;
            pendingCompletion = null;
            AudioListener.pause = previousAudioPause;
            ad.OffClose(closeHandler);
            ad.OffError(errorHandler);
            closeHandler = null;
            errorHandler = null;
            try
            {
                ad.Destroy();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not release the vivo rewarded ad. {exception.Message}", this);
            }

            ResolveReward(ended);
            completed?.Invoke(ended);
        }

        private void OnDisable()
        {
            if (pendingAd != null) CompleteRequest(pendingAd, false);
        }
#endif
    }
}

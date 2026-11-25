using System;
using System.Collections.Generic;
using System.Text;
using UnityCore;
using UnityEngine;

namespace BeachBoxing
{
    public readonly struct BeachAnalyticsEvent
    {
        public readonly string EventName;
        public readonly Dictionary<string, object> Data;

        public BeachAnalyticsEvent(string eventName, Dictionary<string, object> data)
        {
            EventName = eventName;
            Data = data;
        }
    }

    public interface IBeachAdProvider
    {
        void ShowRewarded(string placement, Action onReward, Action onFailure);
        void ReportEvent(string eventName, Dictionary<string, object> data);
        void RequestSdkEntry(string featureName);
    }

    public static class BeachAdService
    {
        public const string RewardAdEventName = "look_ad_id";
        public const string SdkEntryEventName = "DY_giftPack_id";

        private static IBeachAdProvider provider;
        private static int generation;
        private static readonly HashSet<string> reportedEventsThisSession = new HashSet<string>();

        public static bool IsShowing { get; private set; }
        public static event Action<string, Dictionary<string, object>> EventReported;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            provider = null;
            generation = 0;
            IsShowing = false;
            EventReported = null;
            reportedEventsThisSession.Clear();
        }

        public static void RegisterProvider(IBeachAdProvider customProvider)
        {
            provider = customProvider;
            generation++;
            IsShowing = false;
        }

        public static void RequestSdkEntry(string featureName)
        {
            var source = provider;
            ReportStatus(source, SdkEntryEventName, featureName, "点击");
            if (source != null)
            {
                try { source.RequestSdkEntry(featureName); }
                catch (Exception e) { Debug.LogException(e); }
                return;
            }
            Notify("请接入SDK");
        }

        public static bool ShowRewardedAd(string placement, Action onReward, Action onFail = null)
        {
            if (IsShowing) return false;
            IsShowing = true;
            int requestGeneration = generation;
            bool settled = false;
            var source = provider;
            ReportStatus(source, RewardAdEventName, placement, "点击");

            void Complete(bool rewarded)
            {
                if (settled || requestGeneration != generation) return;
                settled = true;
                IsShowing = false;
                ReportStatus(source, RewardAdEventName, placement, rewarded ? "成功" : "失败");
                if (rewarded) onReward?.Invoke();
                else onFail?.Invoke();
            }

            // Default behavior without an external ad SDK: grant the reward directly after reporting analytics.
            if (source == null)
            {
                Complete(true);
                return true;
            }

            try
            {
                source.ShowRewarded(placement, () => Complete(true), () => Complete(false));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Complete(false);
            }
            return true;
        }

        public static bool ShowRewardedAd(string placement, string adUnitId, Action onReward, Action onFail = null)
        {
            return ShowRewardedAd(placement, onReward, onFail);
        }

        public static void ReportEvent(string eventName, Dictionary<string, object> data, bool oncePerSession = false)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            if (oncePerSession)
            {
                string key = BuildEventKey(eventName, data);
                if (!reportedEventsThisSession.Add(key)) return;
            }
            DispatchEvent(provider, eventName, data);
        }

        public static void ReportEventOnce(string eventName, Dictionary<string, object> data)
        {
            ReportEvent(eventName, data, true);
        }

        private static string BuildEventKey(string eventName, Dictionary<string, object> data)
        {
            if (data == null || data.Count == 0) return eventName;
            var sb = new StringBuilder(eventName);
            foreach (var kv in data)
                sb.Append('_').Append(kv.Key).Append(':').Append(kv.Value);
            return sb.ToString();
        }

        public static void Notify(string message)
        {
            var common = BeachCommonUI.Ensure();
            if (common != null && common.Toast != null)
                common.Toast.Show(message);
        }

        private static void ReportStatus(IBeachAdProvider source, string eventName, string placement, string status)
        {
            var payload = new Dictionary<string, object>
            {
                { eventName, placement ?? string.Empty },
                { "status", status ?? string.Empty }
            };
            DispatchEvent(source, eventName, payload);
        }

        private static void DispatchEvent(IBeachAdProvider source, string eventName, Dictionary<string, object> data)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            var payload = data ?? new Dictionary<string, object>();

            if (source != null)
            {
                try { source.ReportEvent(eventName, payload); }
                catch (Exception e) { Debug.LogException(e); }
            }
            else
            {
                Debug.Log($"[埋点] {eventName}: {FormatPayload(payload)}");
            }

            try { EventReported?.Invoke(eventName, payload); }
            catch (Exception e) { Debug.LogException(e); }

            try { CoreRoot.Current?.Events?.Publish(new BeachAnalyticsEvent(eventName, payload)); }
            catch (Exception e) { Debug.LogException(e); }
        }

        private static string FormatPayload(Dictionary<string, object> payload)
        {
            if (payload == null || payload.Count == 0) return "{}";
            var sb = new StringBuilder();
            sb.Append('{');
            bool first = true;
            foreach (var kv in payload)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append('"').Append(kv.Key).Append("\":\"").Append(kv.Value).Append('"');
            }
            sb.Append('}');
            return sb.ToString();
        }
    }
}

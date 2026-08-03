using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Wotc.Mtgo.Gre.External.Messaging;

namespace StormWatch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "dev.deep.mtga.stormwatch";
        public const string PluginName = "StormWatch";
        public const string PluginVersion = "0.1.9";

        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Log = Logger;

            var enabled = Config.Bind("General", "Enabled", true,
                "Show StormWatch while a game is in progress.");
            var toggleShortcut = Config.Bind("General", "Toggle Shortcut",
                new KeyboardShortcut(KeyCode.F8),
                "Show or hide StormWatch. The setting is remembered between games.");
            var positionX = Config.Bind("Above Action Controls Placement", "OffsetX", -30f,
                "Horizontal offset in reference pixels from the lower-right corner.");
            var positionY = Config.Bind("Above Action Controls Placement", "OffsetY", 232f,
                "Vertical offset in reference pixels above the phase and action controls.");
            var scale = Config.Bind("Appearance", "Scale", 1f,
                new ConfigDescription("Overlay scale.", new AcceptableValueRange<float>(0.65f, 1.75f)));
            var opacity = Config.Bind("Appearance", "Opacity", 0.96f,
                new ConfigDescription("Overlay opacity.", new AcceptableValueRange<float>(0.35f, 1f)));

            var persistentObject = new GameObject("StormWatch_Persistent");
            persistentObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(persistentObject);
            var runtime = persistentObject.AddComponent<StormWatchRuntime>();
            runtime.Initialize(enabled, toggleShortcut, positionX, positionY, scale, opacity);

            var harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(Plugin).Assembly);

            Logger.LogInfo(
                $"{PluginName} {PluginVersion} loaded with persistent read-only GRE listener");
        }
    }

    internal sealed class StormWatchRuntime : MonoBehaviour
    {
        internal static StormWatchRuntime Instance { get; private set; }

        private readonly StormStateTracker _tracker = new StormStateTracker();
        private StormOverlay _overlay;
        private ConfigEntry<bool> _enabled;
        private ConfigEntry<KeyboardShortcut> _toggleShortcut;
        private ConfigEntry<float> _positionX;
        private ConfigEntry<float> _positionY;
        private ConfigEntry<float> _scale;
        private ConfigEntry<float> _opacity;

        internal void Initialize(
            ConfigEntry<bool> enabled,
            ConfigEntry<KeyboardShortcut> toggleShortcut,
            ConfigEntry<float> positionX,
            ConfigEntry<float> positionY,
            ConfigEntry<float> scale,
            ConfigEntry<float> opacity)
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _enabled = enabled;
            _toggleShortcut = toggleShortcut;
            _positionX = positionX;
            _positionY = positionY;
            _scale = scale;
            _opacity = opacity;

            _overlay = gameObject.AddComponent<StormOverlay>();
            _overlay.Initialize(
                _positionX.Value,
                _positionY.Value,
                _scale.Value,
                _opacity.Value);
        }

        private void Update()
        {
            if (_overlay == null) return;

            if (_toggleShortcut.Value.IsDown())
            {
                SetOverlayEnabled(!_enabled.Value, "shortcut");
            }

            _overlay.SetEnabled(_enabled.Value);
            _overlay.ApplyLayout(
                _positionX.Value,
                _positionY.Value,
                _scale.Value,
                _opacity.Value);
        }

        internal bool IsOverlayEnabled => _enabled == null || _enabled.Value;

        internal void SetOverlayEnabled(bool enabled, string source)
        {
            if (_enabled == null) return;

            _enabled.Value = enabled;
            _overlay?.SetEnabled(enabled);
            Plugin.Log.LogInfo($"overlay {(enabled ? "shown" : "hidden")} via {source}");
        }

        internal void HandleGreMessage(GREToClientMessage message)
        {
            if (message == null) return;

            try
            {
                var update = _tracker.Process(message);
                if (!update.Changed) return;

                _overlay?.ShowState(
                    update.Count,
                    update.Turn,
                    update.InGame,
                    update.Incremented,
                    update.Reset);

                if (update.Reset || update.Incremented)
                {
                    Plugin.Log.LogInfo(
                        $"state: inGame={update.InGame}, turn={update.Turn}, spells={update.Count}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Ignored malformed GRE update: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            try
            {
                _overlay?.Dispose();
            }
            finally
            {
                if (ReferenceEquals(Instance, this))
                {
                    new Harmony(Plugin.PluginGuid).UnpatchSelf();
                    Instance = null;
                }
            }
        }
    }

    [HarmonyPatch(typeof(MatchManager), "OnMessageReceived")]
    internal static class MatchManagerMessagePatch
    {
        [HarmonyPostfix]
        private static void AfterMessageReceived(GREToClientMessage __0)
        {
            // This postfix only observes the already-received protobuf. It does not
            // replace the message, mutate it, or alter MatchManager state.
            StormWatchRuntime.Instance?.HandleGreMessage(__0);
        }
    }

    [HarmonyPatch(typeof(SettingsPanelGameplay), "ShowPanel")]
    internal static class SettingsPanelGameplayPatch
    {
        [HarmonyPostfix]
        private static void AfterShowPanel(SettingsPanelGameplay __instance)
        {
            StormWatchSettingsToggle.TryMount(__instance);
        }
    }
}

using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StormWatch
{
    /// <summary>
    /// Adds a StormWatch row by cloning Arena's own Gameplay toggle prefab.  Replacing
    /// the cloned Toggle component is deliberate: it preserves the visual styling but
    /// prevents any of Arena's serialized settings callbacks from being copied across.
    /// </summary>
    internal static class StormWatchSettingsToggle
    {
        private const string RowName = "StormWatchSettingsRow";
        private static readonly AccessTools.FieldRef<SettingsPanelGameplay, Toggle> AutoPayToggle =
            AccessTools.FieldRefAccess<SettingsPanelGameplay, Toggle>("_autoPayToggle");

        internal static void TryMount(SettingsPanelGameplay panel)
        {
            if (panel == null || StormWatchRuntime.Instance == null) return;

            var existing = FindRow(panel.transform);
            if (existing != null)
            {
                Sync(existing);
                return;
            }

            Toggle sourceToggle;
            try
            {
                sourceToggle = AutoPayToggle(panel);
            }
            catch
            {
                return;
            }

            if (sourceToggle == null || sourceToggle.transform.parent == null) return;

            var sourceRow = FindRowRoot(sourceToggle);
            if (sourceRow == null || sourceRow.transform.parent == null) return;
            var row = Object.Instantiate(sourceRow, sourceRow.transform.parent);
            row.name = RowName;
            row.transform.SetSiblingIndex(sourceRow.transform.GetSiblingIndex() + 1);
            PositionInPanel(sourceRow, row);

            DisableLocalizers(row);
            SetLabel(row, "StormWatch Overlay");

            var toggle = ReplaceToggle(row);
            if (toggle == null)
            {
                Object.Destroy(row);
                return;
            }

            toggle.SetIsOnWithoutNotify(StormWatchRuntime.Instance.IsOverlayEnabled);
            toggle.onValueChanged.AddListener(value =>
            {
                StormWatchRuntime.Instance?.SetOverlayEnabled(value, "Gameplay settings");
            });

            Plugin.Log.LogInfo("added native Gameplay settings toggle");
        }

        private static GameObject FindRowRoot(Toggle toggle)
        {
            var current = toggle.transform;
            while (current.parent != null)
            {
                if (current.parent.GetComponent<LayoutGroup>() != null)
                    return current.gameObject;
                current = current.parent;
            }

            return toggle.transform.parent != null ? toggle.transform.parent.gameObject : null;
        }

        private static void PositionInPanel(GameObject sourceRow, GameObject row)
        {
            var parentRect = row.transform.parent as RectTransform;
            if (parentRect == null) return;

            // A layout group handles both the vertical placement and expanding scroll
            // content.  Some Arena versions use manually positioned rows instead.
            if (parentRect.GetComponent<LayoutGroup>() != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                return;
            }

            var sourceRect = sourceRow.transform as RectTransform;
            var rowRect = row.transform as RectTransform;
            if (sourceRect == null || rowRect == null) return;

            var rowHeight = Mathf.Max(sourceRect.rect.height, 32f);
            rowRect.anchoredPosition = sourceRect.anchoredPosition + Vector2.down * rowHeight;
            foreach (Transform sibling in parentRect)
            {
                if (sibling == sourceRow.transform || sibling == row.transform) continue;
                var siblingRect = sibling as RectTransform;
                if (siblingRect != null && siblingRect.anchoredPosition.y < sourceRect.anchoredPosition.y)
                    siblingRect.anchoredPosition += Vector2.down * rowHeight;
            }
        }

        private static GameObject FindRow(Transform root)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == RowName) return child.gameObject;
            }

            return null;
        }

        private static void Sync(GameObject row)
        {
            var toggle = row.GetComponentInChildren<Toggle>(true);
            if (toggle != null && StormWatchRuntime.Instance != null)
                toggle.SetIsOnWithoutNotify(StormWatchRuntime.Instance.IsOverlayEnabled);
        }

        private static Toggle ReplaceToggle(GameObject row)
        {
            var original = row.GetComponentInChildren<Toggle>(true);
            if (original == null) return null;

            var host = original.gameObject;
            var targetGraphic = original.targetGraphic;
            var graphic = original.graphic;
            var transition = original.transition;
            var colors = original.colors;
            var spriteState = original.spriteState;
            var animationTriggers = original.animationTriggers;
            var navigation = original.navigation;

            Object.DestroyImmediate(original);

            var toggle = host.AddComponent<Toggle>();
            toggle.targetGraphic = targetGraphic;
            toggle.graphic = graphic;
            toggle.transition = transition;
            toggle.colors = colors;
            toggle.spriteState = spriteState;
            toggle.animationTriggers = animationTriggers;
            toggle.navigation = navigation;
            return toggle;
        }

        private static void DisableLocalizers(GameObject row)
        {
            foreach (var component in row.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var name = component.GetType().FullName;
                if (name != null && name.Contains(".Loc.Localize")) component.enabled = false;
            }
        }

        private static void SetLabel(GameObject row, string label)
        {
            TMP_Text best = null;
            var bestWidth = float.MinValue;
            foreach (var text in row.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null || string.IsNullOrEmpty(text.text)) continue;
                var width = text.rectTransform.rect.width;
                if (width <= bestWidth) continue;
                best = text;
                bestWidth = width;
            }

            if (best != null) best.text = label;
        }
    }
}

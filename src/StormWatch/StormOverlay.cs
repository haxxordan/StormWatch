using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StormWatch
{
    internal sealed class StormOverlay : MonoBehaviour
    {
        private static readonly Color Cyan = new Color(0.18f, 0.84f, 1f, 1f);
        private static readonly Color Violet = new Color(0.69f, 0.36f, 1f, 1f);
        private static readonly Color Gold = new Color(1f, 0.72f, 0.24f, 1f);
        private static readonly Color Muted = new Color(0.43f, 0.48f, 0.62f, 1f);

        private GameObject _root;
        private RectTransform _rootRect;
        private CanvasGroup _canvasGroup;
        private Image _panel;
        private Image _glow;
        private Image _accent;
        private Image _brandBadge;
        private TMP_Text _titleText;
        private TMP_Text _countText;
        private TMP_Text _turnText;
        private RectTransform _countRect;
        private TMP_FontAsset _font;
        private bool _usingMagicFont;
        private Sprite _panelSprite;
        private Sprite _glowSprite;
        private Texture2D _panelTexture;
        private Texture2D _glowTexture;

        private bool _enabled = true;
        private bool _inGame;
        private float _pulse;
        private float _visible;
        private float _opacity = 0.96f;
        private int _count;
        private float _lastX = float.NaN;
        private float _lastY = float.NaN;
        private float _lastScale = float.NaN;

        internal void Initialize(float x, float y, float scale, float opacity)
        {
            _font = FindArenaFont(out _usingMagicFont);
            _panelSprite = CreateRoundedSprite(
                64, 15f,
                new Color(0.055f, 0.058f, 0.105f, 0.97f),
                new Color(0.105f, 0.085f, 0.19f, 0.97f),
                1.5f,
                new Color(0.36f, 0.3f, 0.68f, 0.72f),
                out _panelTexture);
            _glowSprite = CreateGlowSprite(64, 13f, out _glowTexture);

            _root = new GameObject("StormWatchCanvas");
            _root.transform.SetParent(transform, false);
            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var scaler = _root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _canvasGroup = _root.AddComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;

            var card = NewUiObject("Card", _root.transform);
            _rootRect = card.GetComponent<RectTransform>();
            _rootRect.anchorMin = new Vector2(1f, 0f);
            _rootRect.anchorMax = new Vector2(1f, 0f);
            _rootRect.pivot = new Vector2(1f, 0f);
            _rootRect.sizeDelta = new Vector2(132f, 106f);

            var glow = NewUiObject("Glow", card.transform);
            Stretch(glow.GetComponent<RectTransform>(), -14f);
            _glow = glow.AddComponent<Image>();
            _glow.sprite = _glowSprite;
            _glow.type = Image.Type.Sliced;
            _glow.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0f);
            _glow.raycastTarget = false;

            var panel = NewUiObject("Panel", card.transform);
            Stretch(panel.GetComponent<RectTransform>(), 0f);
            _panel = panel.AddComponent<Image>();
            _panel.sprite = _panelSprite;
            _panel.type = Image.Type.Sliced;
            _panel.color = Color.white;
            _panel.raycastTarget = false;

            var accent = NewUiObject("Accent", panel.transform);
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0.18f);
            accentRect.anchorMax = new Vector2(0f, 0.82f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = new Vector2(8f, 0f);
            accentRect.sizeDelta = new Vector2(4f, 0f);
            _accent = accent.AddComponent<Image>();
            _accent.sprite = _panelSprite;
            _accent.type = Image.Type.Sliced;
            _accent.color = Muted;
            _accent.raycastTarget = false;

            var brandBadge = NewUiObject("BrandBadge", panel.transform);
            var brandBadgeRect = brandBadge.GetComponent<RectTransform>();
            SetRect(brandBadgeRect, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-10f, 11f), new Vector2(78f, 20f), new Vector2(1f, 0f));
            _brandBadge = brandBadge.AddComponent<Image>();
            _brandBadge.sprite = _panelSprite;
            _brandBadge.type = Image.Type.Sliced;
            _brandBadge.color = new Color(0.12f, 0.25f, 0.36f, 0.92f);
            _brandBadge.raycastTarget = false;

            _titleText = CreateText(brandBadge.transform, "STORMWATCH", 8, FontStyles.Bold, Cyan);
            Stretch(_titleText.rectTransform, 2f);
            _titleText.alignment = TextAlignmentOptions.Center;

            _turnText = CreateText(panel.transform, "TURN —", 10, FontStyles.Bold,
                new Color(0.48f, 0.51f, 0.64f, 1f));
            SetRect(_turnText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(19f, 15f), new Vector2(55f, 20f), new Vector2(0f, 0f));
            _turnText.alignment = TextAlignmentOptions.Left;

            _countText = CreateText(panel.transform, "0", 48, FontStyles.Bold, Color.white);
            _countRect = _countText.rectTransform;
            SetRect(_countRect, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-15f, -10f), new Vector2(70f, 66f), new Vector2(1f, 1f));
            _countText.alignment = TextAlignmentOptions.Right;
            _countText.outlineColor = new Color32(38, 140, 204, 90);
            _countText.outlineWidth = 0.12f;

            ApplyLayout(x, y, scale, opacity);
        }

        internal void SetEnabled(bool enabled)
        {
            _enabled = enabled;
        }

        internal void ApplyLayout(float x, float y, float scale, float opacity)
        {
            _opacity = Mathf.Clamp01(opacity);
            if (_rootRect == null) return;

            if (!Mathf.Approximately(x, _lastX) || !Mathf.Approximately(y, _lastY))
            {
                _rootRect.anchoredPosition = new Vector2(x, y);
                _lastX = x;
                _lastY = y;
            }

            if (!Mathf.Approximately(scale, _lastScale))
            {
                _rootRect.localScale = Vector3.one * Mathf.Clamp(scale, 0.65f, 1.75f);
                _lastScale = scale;
            }
        }

        internal void ShowState(
            int count,
            uint turn,
            bool inGame,
            bool incremented,
            bool reset)
        {
            _inGame = inGame;
            _count = count;

            if (_countText != null)
                _countText.text = count.ToString();
            if (_turnText != null)
                _turnText.text = turn > 0 ? $"TURN {turn}" : "TURN —";

            var color = AccentFor(count);
            if (_accent != null) _accent.color = color;
            if (_countText != null)
                _countText.color = count == 0
                    ? new Color(0.82f, 0.84f, 0.92f, 1f)
                    : Color.Lerp(Color.white, color, 0.2f);

            if (incremented)
                _pulse = 1f;
            else if (reset)
                _pulse = Mathf.Max(_pulse, 0.28f);
        }

        private void Update()
        {
            if (_canvasGroup == null) return;

            EnsureArenaFont();

            var targetVisible = _enabled && _inGame ? 1f : 0f;
            _visible = Damp(_visible, targetVisible, 11f);
            _canvasGroup.alpha = _visible * _opacity;

            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.unscaledDeltaTime * 2.8f);
            var easedPulse = Mathf.Sin(_pulse * Mathf.PI) * _pulse;
            if (_countRect != null)
                _countRect.localScale = Vector3.one * (1f + easedPulse * 0.2f);

            var accent = AccentFor(_count);
            if (_brandBadge != null)
                _brandBadge.color = Color.Lerp(
                    new Color(0.12f, 0.25f, 0.36f, 0.92f),
                    new Color(accent.r, accent.g, accent.b, 0.92f),
                    0.15f + easedPulse * 0.1f);
            if (_glow != null)
            {
                _glow.color = new Color(
                    accent.r,
                    accent.g,
                    accent.b,
                    0.055f + easedPulse * 0.2f);
            }

            if (_panel != null)
            {
                var lift = easedPulse * 0.035f;
                _panel.color = new Color(1f + lift, 1f + lift, 1f + lift, 1f);
            }
        }

        internal void Dispose()
        {
            if (_root != null) Destroy(_root);
            if (_panelSprite != null) Destroy(_panelSprite);
            if (_glowSprite != null) Destroy(_glowSprite);
            if (_panelTexture != null) Destroy(_panelTexture);
            if (_glowTexture != null) Destroy(_glowTexture);
        }

        private TMP_Text CreateText(
            Transform parent,
            string content,
            float size,
            FontStyles style,
            Color color)
        {
            var obj = NewUiObject(content.Replace(" ", string.Empty), parent);
            var text = obj.AddComponent<TextMeshProUGUI>();
            if (_font != null) text.font = _font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.extraPadding = true;
            return text;
        }

        private void EnsureArenaFont()
        {
            var preferredFont = FindArenaFont(out var isMagicFont);
            if (preferredFont == null || (ReferenceEquals(preferredFont, _font) && isMagicFont == _usingMagicFont))
                return;

            _font = preferredFont;
            _usingMagicFont = isMagicFont;

            if (_titleText != null) _titleText.font = _font;
            if (_turnText != null) _turnText.font = _font;
            if (_countText != null) _countText.font = _font;
        }

        private static TMP_FontAsset FindArenaFont(out bool isMagicFont)
        {
            isMagicFont = false;

            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fonts != null)
            {
                foreach (var font in fonts)
                {
                    if (font != null && IsMagicFont(font.name))
                    {
                        isMagicFont = true;
                        return font;
                    }
                }
            }

            var legacyFonts = Resources.FindObjectsOfTypeAll<Font>();
            if (legacyFonts != null)
            {
                foreach (var legacyFont in legacyFonts)
                {
                    if (legacyFont == null || !IsMagicFont(legacyFont.name)) continue;

                    try
                    {
                        var created = TMP_FontAsset.CreateFontAsset(legacyFont);
                        if (created != null)
                        {
                            created.name = "StormWatch_Beleren";
                            isMagicFont = true;
                            return created;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogDebug($"Could not create Beleren TMP font: {ex.Message}");
                    }
                }
            }

            var sample = FindObjectOfType<TMP_Text>();
            if (sample != null && sample.font != null)
                return sample.font;

            return fonts != null && fonts.Length > 0 ? fonts[0] : null;
        }

        private static bool IsMagicFont(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf("Beleren", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static GameObject NewUiObject(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float expansion)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-expansion, -expansion);
            rect.offsetMax = new Vector2(expansion, expansion);
        }

        private static float Damp(float current, float target, float speed)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        }

        private static Color AccentFor(int count)
        {
            if (count <= 0) return Muted;
            if (count < 3) return Cyan;
            if (count < 6) return Violet;
            return Gold;
        }

        private static Sprite CreateRoundedSprite(
            int size,
            float radius,
            Color top,
            Color bottom,
            float borderWidth,
            Color border,
            out Texture2D texture)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "StormWatchPanelTexture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                var gradient = Color.Lerp(bottom, top, y / (size - 1f));
                for (var x = 0; x < size; x++)
                {
                    var distance = RoundedRectDistance(x + 0.5f, y + 0.5f, size, radius);
                    var coverage = Mathf.Clamp01(0.5f - distance);
                    var borderMix = Mathf.Clamp01(borderWidth - Mathf.Abs(distance));
                    var color = Color.Lerp(gradient, border, borderMix * 0.8f);
                    color.a *= coverage;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        private static Sprite CreateGlowSprite(int size, float radius, out Texture2D texture)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "StormWatchGlowTexture";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = RoundedRectDistance(x + 0.5f, y + 0.5f, size, radius);
                    var alpha = Mathf.Clamp01(1f - Mathf.Max(0f, distance) / 11f);
                    alpha = alpha * alpha * 0.8f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        private static float RoundedRectDistance(float x, float y, float size, float radius)
        {
            var center = size * 0.5f;
            var qx = Mathf.Abs(x - center) - (center - radius - 1f);
            var qy = Mathf.Abs(y - center) - (center - radius - 1f);
            var outside = Mathf.Sqrt(
                Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace BottleBattle
{
    /// <summary>
    /// Responsive, dependency-free prototype for the main menu.
    /// It uses Unity IMGUI so the first screen works before any art package is imported.
    /// The final art can later replace the procedural shapes without changing menu logic.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;
        private const int MinimumBottleCount = 3;
        private const int MaximumBottleCount = 10;

        [SerializeField, Range(MinimumBottleCount, MaximumBottleCount)]
        private int showcaseBottleCount = 7;

        private readonly Dictionary<string, GUIStyle> panelStyles = new();
        private readonly List<Texture2D> generatedTextures = new();

        private Font uiFont;
        private GUIStyle titleCoral;
        private GUIStyle titleNavy;
        private GUIStyle topBarText;
        private GUIStyle buttonText;
        private GUIStyle secondaryButtonText;
        private GUIStyle smallButtonText;
        private GUIStyle bottleBrandText;
        private GUIStyle bottleNumberText;
        private GUIStyle bottleFooterText;
        private GUIStyle bottleMarkText;
        private GUIStyle hintText;

        private string statusMessage = string.Empty;
        private float statusUntil;

        private static readonly Color Cream = Html("#FFF8E8");
        private static readonly Color CreamDark = Html("#F5E5C2");
        private static readonly Color Navy = Html("#123E64");
        private static readonly Color Coral = Html("#FF5266");
        private static readonly Color Cyan = Html("#15AFE0");
        private static readonly Color Lime = Html("#84C62D");
        private static readonly Color Gold = Html("#FFB817");
        private static readonly Color Purple = Html("#9A61C6");
        private static readonly Color Orange = Html("#F57C1F");
        private static readonly Color Teal = Html("#36BEAD");
        private static readonly Color Oak = Html("#C88538");
        private static readonly Color OakDark = Html("#8B5222");
        private static readonly Color White = new(1f, 1f, 1f, 1f);
        private static readonly Color Shadow = new(0.08f, 0.18f, 0.24f, 0.22f);

        private static readonly Color[] BottleColors =
        {
            Coral, Cyan, Lime, Gold, Purple, Orange, Teal,
            Html("#ED6DA8"), Html("#4D83E1"), Html("#A8C93A")
        };

        public event Action PlayRequested;
        public event Action OnlinePlayRequested;
        public event Action DailyPuzzleRequested;
        public event Action LevelsRequested;
        public event Action SettingsRequested;

        public int ShowcaseBottleCount => showcaseBottleCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureMenuExists()
        {
            if (FindAnyObjectByType<MainMenuController>() != null)
            {
                return;
            }

            var menu = new GameObject("Main Menu");
            menu.AddComponent<MainMenuController>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private void OnEnable()
        {
            CreateStyles();
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in generatedTextures)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Minus))
            {
                SetBottleCount(showcaseBottleCount - 1);
            }

            if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                SetBottleCount(showcaseBottleCount + 1);
            }
#endif
        }

        public void SetBottleCount(int count)
        {
            showcaseBottleCount = Mathf.Clamp(count, MinimumBottleCount, MaximumBottleCount);
        }

        private void OnGUI()
        {
            if (uiFont == null)
            {
                CreateStyles();
            }

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;

            Rect safeArea = Screen.safeArea;
            float scale = Mathf.Min(safeArea.width / DesignWidth, safeArea.height / DesignHeight);
            float contentWidth = DesignWidth * scale;
            float contentHeight = DesignHeight * scale;
            float safeTop = Screen.height - safeArea.yMax;
            float offsetX = safeArea.x + (safeArea.width - contentWidth) * 0.5f;
            float offsetY = safeTop + (safeArea.height - contentHeight) * 0.5f;

            GUI.matrix = Matrix4x4.TRS(
                new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity,
                new Vector3(scale, scale, 1f));

            DrawBackground();
            DrawTopBar();
            DrawTitle();
            DrawBottleShowcase();
            DrawActions();
            DrawStatus();

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawBackground()
        {
            GUI.color = Cream;
            GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, DesignHeight), Texture2D.whiteTexture);

            GUI.color = new Color(CreamDark.r, CreamDark.g, CreamDark.b, 0.55f);
            DrawCircle(new Rect(-95f, 530f, 230f, 230f));
            DrawCircle(new Rect(935f, 590f, 220f, 220f));
            DrawCircle(new Rect(-65f, 1510f, 190f, 190f));
            DrawCircle(new Rect(950f, 1545f, 195f, 195f));
            GUI.color = White;
        }

        private void DrawTopBar()
        {
            if (DrawRoundedButton(
                    new Rect(54f, 56f, 126f, 112f),
                    "≡",
                    Cream,
                    CreamDark,
                    topBarText))
            {
                SettingsRequested?.Invoke();
                ShowStatus("Settings coming soon");
            }

            DrawRoundedPanel(new Rect(715f, 67f, 310f, 92f), Cream, CreamDark);
            GUI.color = Gold;
            DrawCircle(new Rect(734f, 79f, 68f, 68f));
            GUI.color = White;
            GUI.Label(new Rect(740f, 83f, 60f, 58f), "₺", topBarText);
            GUI.Label(new Rect(815f, 82f, 125f, 58f), "1250", topBarText);

            GUI.color = Lime;
            DrawCircle(new Rect(946f, 84f, 56f, 56f));
            GUI.color = White;
            GUI.Label(new Rect(949f, 84f, 50f, 48f), "+", topBarText);
        }

        private void DrawTitle()
        {
            GUI.color = Shadow;
            GUI.Label(new Rect(8f, 186f, DesignWidth, 142f), "BOTTLE", titleCoral);
            GUI.Label(new Rect(8f, 306f, DesignWidth, 142f), "BATTLE", titleNavy);

            GUI.color = White;
            GUI.Label(new Rect(0f, 178f, DesignWidth, 142f), "BOTTLE", titleCoral);
            GUI.Label(new Rect(0f, 298f, DesignWidth, 142f), "BATTLE", titleNavy);
        }

        private void DrawBottleShowcase()
        {
            const float left = 88f;
            const float right = 992f;
            const float shelfTop = 908f;
            float availableWidth = right - left;
            float density = Mathf.InverseLerp(MinimumBottleCount, MaximumBottleCount, showcaseBottleCount);
            float gap = Mathf.Lerp(28f, 7f, density);
            float bottleWidth = Mathf.Min(
                134f,
                (availableWidth - gap * (showcaseBottleCount - 1)) / showcaseBottleCount);
            float usedWidth = bottleWidth * showcaseBottleCount + gap * (showcaseBottleCount - 1);
            float startX = left + (availableWidth - usedWidth) * 0.5f;
            float bottleHeight = Mathf.Lerp(300f, 220f, density);

            for (int index = 0; index < showcaseBottleCount; index++)
            {
                float x = startX + index * (bottleWidth + gap);
                DrawBottle(new Rect(x, shelfTop - bottleHeight, bottleWidth, bottleHeight), index);
            }

            GUI.color = Shadow;
            DrawRoundedPanel(new Rect(78f, shelfTop + 16f, 924f, 70f), Shadow, Shadow, 24);
            GUI.color = White;
            DrawRoundedPanel(new Rect(66f, shelfTop, 948f, 68f), Oak, OakDark, 24);
            DrawRoundedPanel(new Rect(116f, shelfTop + 55f, 48f, 38f), Oak, OakDark, 12);
            DrawRoundedPanel(new Rect(916f, shelfTop + 55f, 48f, 38f), Oak, OakDark, 12);

#if UNITY_EDITOR
            GUI.Label(
                new Rect(320f, 1024f, 440f, 34f),
                $"Preview: {showcaseBottleCount} bottles  (− / +)",
                hintText);
#endif
        }

        private void DrawBottle(Rect rect, int index)
        {
            if (DrawSpriteBottle(rect, index))
            {
                return;
            }

            Color color = BottleColors[index % BottleColors.Length];
            float capHeight = Mathf.Max(22f, rect.height * 0.10f);
            float neckWidth = rect.width * 0.48f;
            float neckHeight = rect.height * 0.13f;
            float neckX = rect.center.x - neckWidth * 0.5f;
            float bodyTop = rect.y + capHeight + neckHeight * 0.62f;
            float bodyHeight = rect.yMax - bodyTop;
            Color edge = Darken(color, 0.28f);

            GUI.color = Shadow;
            DrawRoundedPanel(
                new Rect(rect.x + 7f, bodyTop + 9f, rect.width, rect.yMax - bodyTop),
                Shadow,
                Shadow,
                Mathf.RoundToInt(rect.width * 0.22f));

            GUI.color = White;
            DrawRoundedPanel(
                new Rect(rect.x, bodyTop, rect.width, rect.yMax - bodyTop),
                color,
                edge,
                Mathf.RoundToInt(rect.width * 0.22f));
            DrawRoundedPanel(
                new Rect(
                    rect.x + rect.width * 0.79f,
                    bodyTop + 8f,
                    rect.width * 0.14f,
                    bodyHeight - 18f),
                Darken(color, 0.13f),
                Color.clear,
                Mathf.RoundToInt(rect.width * 0.07f));
            DrawRoundedPanel(
                new Rect(
                    rect.x + rect.width * 0.10f,
                    rect.yMax - bodyHeight * 0.13f,
                    rect.width * 0.80f,
                    bodyHeight * 0.09f),
                Darken(color, 0.17f),
                Color.clear,
                10);
            DrawRoundedPanel(
                new Rect(neckX, rect.y + capHeight * 0.75f, neckWidth, neckHeight),
                color,
                edge,
                Mathf.RoundToInt(neckWidth * 0.25f));
            DrawRoundedPanel(
                new Rect(neckX - 5f, rect.y, neckWidth + 10f, capHeight),
                Lighten(color, 0.08f),
                edge,
                12);

            GUI.color = new Color(1f, 1f, 1f, 0.30f);
            for (int groove = 1; groove <= 3; groove++)
            {
                float grooveX = neckX - 5f + (neckWidth + 10f) * groove / 4f;
                GUI.DrawTexture(
                    new Rect(grooveX, rect.y + 4f, 2f, Mathf.Max(8f, capHeight - 8f)),
                    Texture2D.whiteTexture);
            }

            GUI.color = new Color(1f, 1f, 1f, 0.22f);
            DrawRoundedPanel(
                new Rect(rect.x + rect.width * 0.12f, bodyTop + 10f, rect.width * 0.18f, rect.height * 0.53f),
                new Color(1f, 1f, 1f, 0.18f),
                new Color(1f, 1f, 1f, 0f),
                12);
            GUI.color = new Color(1f, 1f, 1f, 0.46f);
            DrawCircle(
                new Rect(
                    rect.x + rect.width * 0.27f,
                    bodyTop + bodyHeight * 0.09f,
                    rect.width * 0.10f,
                    rect.width * 0.10f));
            GUI.color = White;

            float labelHeight = bodyHeight * 0.38f;
            float labelY = bodyTop + bodyHeight * 0.31f;
            Rect labelRect = new(
                rect.x + rect.width * 0.12f,
                labelY,
                rect.width * 0.76f,
                labelHeight);
            DrawRoundedPanel(
                labelRect,
                new Color(Cream.r, Cream.g, Cream.b, 0.96f),
                new Color(edge.r, edge.g, edge.b, 0.70f),
                Mathf.RoundToInt(rect.width * 0.10f));
            GUI.Label(
                new Rect(labelRect.x, labelRect.y + labelHeight * 0.04f, labelRect.width, labelHeight * 0.24f),
                "BOTTLE",
                bottleBrandText);
            GUI.Label(
                new Rect(labelRect.x, labelRect.y + labelHeight * 0.20f, labelRect.width, labelHeight * 0.54f),
                $"{index + 1:00}",
                bottleNumberText);
            GUI.Label(
                new Rect(labelRect.x, labelRect.y + labelHeight * 0.69f, labelRect.width, labelHeight * 0.23f),
                "ORDER",
                bottleFooterText);

            string mark = (index % 4) switch
            {
                0 => "●",
                1 => "≈",
                2 => "◆",
                _ => "☀"
            };
            GUI.Label(
                new Rect(rect.x, bodyTop + (rect.yMax - bodyTop) * 0.40f, rect.width, rect.height * 0.25f),
                mark,
                bottleMarkText);
        }

        private bool DrawSpriteBottle(Rect rect, int index)
        {
            if (!BottleSpriteCatalog.TryGet(
                    index,
                    out Texture2D texture,
                    out Rect textureCoordinates,
                    out float aspectRatio))
            {
                return false;
            }

            GUI.color = Shadow;
            DrawCircle(
                new Rect(
                    rect.x + rect.width * 0.13f,
                    rect.yMax - rect.height * 0.055f,
                    rect.width * 0.74f,
                    rect.height * 0.09f));

            float drawHeight = rect.height * 0.98f;
            float drawWidth = drawHeight * aspectRatio;
            float maximumWidth = rect.width * 0.98f;
            if (drawWidth > maximumWidth)
            {
                drawWidth = maximumWidth;
                drawHeight = drawWidth / aspectRatio;
            }

            Rect drawRect = new(
                rect.center.x - drawWidth * 0.5f,
                rect.yMax - drawHeight,
                drawWidth,
                drawHeight);

            GUI.color = White;
            GUI.DrawTextureWithTexCoords(drawRect, texture, textureCoordinates, true);
            return true;
        }

        private void DrawActions()
        {
            if (DrawRoundedButton(
                    new Rect(124f, 1088f, 832f, 152f),
                    "PLAY",
                    Coral,
                    Darken(Coral, 0.24f),
                    buttonText))
            {
                PlayRequested?.Invoke();
                StartGame();
            }

            if (DrawRoundedButton(
                    new Rect(124f, 1264f, 832f, 152f),
                    "PLAY ONLINE",
                    Cyan,
                    Darken(Cyan, 0.25f),
                    buttonText))
            {
                OnlinePlayRequested?.Invoke();
                ShowStatus("Online matchmaking coming next");
            }

            GUI.color = Lime;
            DrawCircle(new Rect(886f, 1304f, 44f, 44f));
            GUI.color = White;

            if (DrawRoundedButton(
                    new Rect(190f, 1447f, 700f, 120f),
                    "▣   DAILY PUZZLE",
                    Cream,
                    CreamDark,
                    secondaryButtonText))
            {
                DailyPuzzleRequested?.Invoke();
                ShowStatus("Daily puzzle selected");
            }

            if (DrawRoundedButton(
                    new Rect(317f, 1592f, 446f, 106f),
                    "▥   LEVELS",
                    Cream,
                    CreamDark,
                    smallButtonText))
            {
                LevelsRequested?.Invoke();
                ShowStatus("Levels selected");
            }
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(statusMessage) || Time.unscaledTime >= statusUntil)
            {
                return;
            }

            DrawRoundedPanel(new Rect(215f, 1750f, 650f, 76f), Navy, Darken(Navy, 0.18f), 28);
            GUI.Label(new Rect(235f, 1762f, 610f, 50f), statusMessage, hintText);
        }

        private void ShowStatus(string message)
        {
            statusMessage = message;
            statusUntil = Time.unscaledTime + 2.2f;
        }

        private void StartGame()
        {
            BottleGameController game = FindAnyObjectByType<BottleGameController>();
            if (game == null)
            {
                game = gameObject.AddComponent<BottleGameController>();
            }

            game.Begin();
            enabled = false;
        }

        private bool DrawRoundedButton(
            Rect rect,
            string text,
            Color fill,
            Color border,
            GUIStyle textStyle)
        {
            GUIStyle background = GetPanelStyle(fill, border, 34);
            GUI.color = Shadow;
            GUI.Box(new Rect(rect.x + 6f, rect.y + 10f, rect.width, rect.height), GUIContent.none, background);
            GUI.color = White;
            return GUI.Button(rect, text, MergeBackground(textStyle, background));
        }

        private void DrawRoundedPanel(
            Rect rect,
            Color fill,
            Color border,
            int radius = 28)
        {
            GUI.Box(rect, GUIContent.none, GetPanelStyle(fill, border, radius));
        }

        private void DrawCircle(Rect rect)
        {
            GUI.DrawTexture(rect, GetCircleTexture(), ScaleMode.StretchToFill, true);
        }

        private GUIStyle GetPanelStyle(Color fill, Color border, int radius)
        {
            string key = $"{ColorUtility.ToHtmlStringRGBA(fill)}-{ColorUtility.ToHtmlStringRGBA(border)}-{radius}";
            if (panelStyles.TryGetValue(key, out GUIStyle style))
            {
                return style;
            }

            const int textureSize = 96;
            int textureRadius = Mathf.Clamp(radius, 8, 44);
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
            {
                name = $"Rounded-{key}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            int borderWidth = 4;
            var pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    bool outer = InsideRoundedRect(x, y, textureSize, textureSize, textureRadius);
                    bool inner = InsideRoundedRect(
                        x - borderWidth,
                        y - borderWidth,
                        textureSize - borderWidth * 2,
                        textureSize - borderWidth * 2,
                        Mathf.Max(1, textureRadius - borderWidth));

                    pixels[y * textureSize + x] = !outer
                        ? Color.clear
                        : inner ? fill : border;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            generatedTextures.Add(texture);

            int slice = textureRadius + 2;
            style = new GUIStyle
            {
                normal = { background = texture },
                hover = { background = texture },
                active = { background = texture },
                border = new RectOffset(slice, slice, slice, slice),
                padding = new RectOffset(16, 16, 8, 8)
            };
            panelStyles[key] = style;
            return style;
        }

        private Texture2D GetCircleTexture()
        {
            const string key = "circle-white";
            if (panelStyles.TryGetValue(key, out GUIStyle style))
            {
                return style.normal.background;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Circle",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.49f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - distance + 1f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, true);
            generatedTextures.Add(texture);
            panelStyles[key] = new GUIStyle { normal = { background = texture } };
            return texture;
        }

        private static GUIStyle MergeBackground(GUIStyle textStyle, GUIStyle background)
        {
            var style = new GUIStyle(textStyle)
            {
                normal = { background = background.normal.background },
                hover = { background = background.normal.background },
                active = { background = background.normal.background },
                border = background.border,
                padding = background.padding
            };
            return style;
        }

        private void CreateStyles()
        {
            uiFont = Resources.Load<Font>("Fonts/Inter-Regular");
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            if (uiFont == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(
                    new[] { "Segoe UI", "Arial", "Liberation Sans" },
                    32);
            }

            titleCoral = CreateTextStyle(116, Coral, FontStyle.Bold, TextAnchor.MiddleCenter);
            titleNavy = CreateTextStyle(112, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            topBarText = CreateTextStyle(40, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            buttonText = CreateTextStyle(52, White, FontStyle.Bold, TextAnchor.MiddleCenter);
            secondaryButtonText = CreateTextStyle(39, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            smallButtonText = CreateTextStyle(36, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            bottleBrandText = CreateTextStyle(12, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            bottleNumberText = CreateTextStyle(30, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            bottleFooterText = CreateTextStyle(10, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            bottleMarkText = CreateTextStyle(1, Color.clear, FontStyle.Normal, TextAnchor.MiddleCenter);
            hintText = CreateTextStyle(27, White, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        private GUIStyle CreateTextStyle(
            int size,
            Color color,
            FontStyle fontStyle,
            TextAnchor alignment)
        {
            return new GUIStyle
            {
                font = uiFont,
                fontSize = size,
                fontStyle = fontStyle,
                alignment = alignment,
                normal = { textColor = color },
                hover = { textColor = color },
                active = { textColor = color },
                wordWrap = false,
                clipping = TextClipping.Clip
            };
        }

        private static bool InsideRoundedRect(
            int x,
            int y,
            int width,
            int height,
            int radius)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                return false;
            }

            int nearestX = Mathf.Clamp(x, radius, width - radius - 1);
            int nearestY = Mathf.Clamp(y, radius, height - radius - 1);
            int dx = x - nearestX;
            int dy = y - nearestY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static Color Html(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }

        private static Color Darken(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r - amount),
                Mathf.Clamp01(color.g - amount),
                Mathf.Clamp01(color.b - amount),
                color.a);
        }

        private static Color Lighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a);
        }
    }
}

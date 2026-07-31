using System.Collections.Generic;
using UnityEngine;

namespace BottleBattle
{
    /// <summary>Scrollable five-column map for all Bottle Battle levels.</summary>
    public sealed class LevelSelectController : MonoBehaviour
    {
        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;
        private const int FinalLevel = 100;
        private const string SavedLevelKey = "BottleOrder.CurrentLevel";

        private static readonly Color Cream = Html("#FFF8E8");
        private static readonly Color CreamDark = Html("#F5E5C2");
        private static readonly Color Navy = Html("#123E64");
        private static readonly Color Coral = Html("#FF5266");
        private static readonly Color Cyan = Html("#15AFE0");
        private static readonly Color Lime = Html("#84C62D");
        private static readonly Color Gold = Html("#FFB817");
        private static readonly Color Purple = Html("#9A61C6");
        private static readonly Color Grey = Html("#A9B0B7");
        private static readonly Color GreyDark = Html("#747D86");
        private static readonly Color White = Color.white;
        private static readonly Color Shadow = new(0.08f, 0.18f, 0.24f, 0.22f);
        private static readonly Color[] CardColors = { Coral, Cyan, Lime, Gold, Purple };

        private readonly Dictionary<string, GUIStyle> panelStyles = new();
        private readonly List<Texture2D> generatedTextures = new();
        private Font uiFont;
        private Texture2D starTexture;
        private GUIStyle backStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle levelStyle;
        private GUIStyle lockedStyle;
        private Vector2 scrollPosition;
        private float touchDragDistance;
        private bool pointerScrolling;
        private float lastPointerY;
        private int pointerReleaseFrame = -1;

        public void Begin()
        {
            enabled = true;
            int unlocked = Mathf.Clamp(PlayerPrefs.GetInt(SavedLevelKey, 1), 1, FinalLevel);
            scrollPosition.y = Mathf.Max(0, (unlocked - 1) / 5 - 2) * 210f;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private void OnEnable()
        {
            starTexture ??= Resources.Load<Texture2D>("UI/completion-star");
            CreateStyles();
        }

        private void HandleScrollEvent(Event currentEvent)
        {
            if (currentEvent == null)
            {
                return;
            }

            if (pointerReleaseFrame >= 0 &&
                Time.frameCount > pointerReleaseFrame &&
                currentEvent.type == EventType.Repaint)
            {
                touchDragDistance = 0f;
                pointerReleaseFrame = -1;
            }

            Vector2 pointer = currentEvent.mousePosition;
            bool overGrid = pointer.x >= 38f && pointer.x <= 1042f &&
                            pointer.y >= 255f && pointer.y <= 1845f;

            if (overGrid && currentEvent.type == EventType.ScrollWheel)
            {
                scrollPosition.y = Mathf.Clamp(
                    scrollPosition.y + currentEvent.delta.y * 120f,
                    0f,
                    2628f);
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && overGrid)
            {
                pointerScrolling = true;
                lastPointerY = pointer.y;
                touchDragDistance = 0f;
            }
            else if (pointerScrolling && currentEvent.type == EventType.MouseDrag)
            {
                float delta = pointer.y - lastPointerY;
                scrollPosition.y = Mathf.Clamp(scrollPosition.y - delta, 0f, 2628f);
                touchDragDistance += Mathf.Abs(delta);
                lastPointerY = pointer.y;
                currentEvent.Use();
            }
            else if (pointerScrolling && currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
            {
                pointerScrolling = false;
                pointerReleaseFrame = Time.frameCount;
            }
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in generatedTextures)
            {
                if (texture != null) Destroy(texture);
            }
        }

        private void OnGUI()
        {
            if (uiFont == null) CreateStyles();
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            Rect safeArea = Screen.safeArea;
            float scale = Mathf.Min(safeArea.width / DesignWidth, safeArea.height / DesignHeight);
            float contentWidth = DesignWidth * scale;
            float contentHeight = DesignHeight * scale;
            float safeTop = Screen.height - safeArea.yMax;
            float offsetX = safeArea.x + (safeArea.width - contentWidth) * 0.5f;
            float offsetY = safeTop + (safeArea.height - contentHeight) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));

            HandleScrollEvent(Event.current);
            DrawBackground();
            DrawHeader();
            DrawGrid();

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawBackground()
        {
            GUI.color = Cream;
            GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, DesignHeight), Texture2D.whiteTexture);
            GUI.color = new Color(CreamDark.r, CreamDark.g, CreamDark.b, 0.58f);
            DrawCircle(new Rect(-95f, 390f, 230f, 230f));
            DrawCircle(new Rect(950f, 690f, 210f, 210f));
            DrawCircle(new Rect(-80f, 1450f, 205f, 205f));
            GUI.color = White;
        }

        private void DrawHeader()
        {
            if (DrawRoundedButton(new Rect(48f, 55f, 124f, 104f), "<", Cream, CreamDark, backStyle))
            {
                ReturnToMenu();
            }

            GUI.Label(new Rect(190f, 52f, 700f, 78f), "SELECT LEVEL", titleStyle);
            int totalStars = 0;
            for (int level = 1; level <= FinalLevel; level++)
            {
                totalStars += Mathf.Clamp(PlayerPrefs.GetInt(GetStarsKey(level), 0), 0, 3);
            }

            DrawRoundedPanel(new Rect(365f, 142f, 350f, 78f), Cream, CreamDark, 28);
            if (starTexture != null)
            {
                GUI.DrawTexture(new Rect(390f, 154f, 54f, 54f), starTexture, ScaleMode.ScaleToFit, true);
            }
            GUI.Label(new Rect(450f, 151f, 235f, 58f), $"{totalStars} / 300", subtitleStyle);
        }

        private void DrawGrid()
        {
            const float viewportTop = 255f;
            const float viewportHeight = 1590f;
            const float cardWidth = 168f;
            const float cardHeight = 178f;
            const float horizontalGap = 28f;
            const float rowPitch = 210f;
            const float left = 64f;
            const float contentHeight = 4218f;
            Rect viewport = new(38f, viewportTop, 1004f, viewportHeight);
            Rect content = new(0f, 0f, 1004f, contentHeight);
            scrollPosition = GUI.BeginScrollView(viewport, scrollPosition, content, false, false, GUIStyle.none, GUIStyle.none);

            int unlockedLevel = Mathf.Clamp(PlayerPrefs.GetInt(SavedLevelKey, 1), 1, FinalLevel);
            for (int level = 1; level <= FinalLevel; level++)
            {
                int index = level - 1;
                int column = index % 5;
                int row = index / 5;
                Rect card = new(left - viewport.x + column * (cardWidth + horizontalGap), 12f + row * rowPitch, cardWidth, cardHeight);
                DrawLevelCard(card, level, level <= unlockedLevel);
            }

            GUI.EndScrollView();
            GUI.color = new Color(Navy.r, Navy.g, Navy.b, 0.10f);
            DrawRoundedPanel(new Rect(1018f, 292f, 8f, 1510f), GUI.color, Color.clear, 4);
            float thumbHeight = Mathf.Max(110f, 1510f * viewportHeight / contentHeight);
            float scrollRange = Mathf.Max(1f, contentHeight - viewportHeight);
            float thumbY = 292f + (1510f - thumbHeight) * Mathf.Clamp01(scrollPosition.y / scrollRange);
            GUI.color = new Color(Navy.r, Navy.g, Navy.b, 0.42f);
            DrawRoundedPanel(new Rect(1018f, thumbY, 8f, thumbHeight), GUI.color, Color.clear, 4);
            GUI.color = White;
        }

        private void DrawLevelCard(Rect rect, int level, bool unlocked)
        {
            Color fill = unlocked ? CardColors[(level - 1) % CardColors.Length] : new Color(Grey.r, Grey.g, Grey.b, 0.56f);
            Color border = unlocked ? Darken(fill, 0.25f) : GreyDark;
            GUIStyle panel = GetPanelStyle(fill, border, 30);
            GUI.color = Shadow;
            GUI.Box(new Rect(rect.x + 5f, rect.y + 8f, rect.width, rect.height), GUIContent.none, panel);
            GUI.color = White;

            if (unlocked && GUI.Button(rect, GUIContent.none, panel) && touchDragDistance < 18f)
            {
                StartLevel(level);
                return;
            }
            if (!unlocked) GUI.Box(rect, GUIContent.none, panel);

            GUI.color = new Color(1f, 1f, 1f, unlocked ? 0.22f : 0.10f);
            DrawRoundedPanel(new Rect(rect.x + 16f, rect.y + 14f, rect.width - 32f, 13f), GUI.color, Color.clear, 7);
            GUI.color = White;

            if (unlocked)
            {
                GUI.Label(new Rect(rect.x, rect.y + 18f, rect.width, 91f), level.ToString(), levelStyle);
                int stars = Mathf.Clamp(PlayerPrefs.GetInt(GetStarsKey(level), 0), 0, 3);
                for (int star = 0; star < 3; star++)
                {
                    GUI.color = star < stars ? White : new Color(Navy.r, Navy.g, Navy.b, 0.18f);
                    if (starTexture != null)
                    {
                        GUI.DrawTexture(new Rect(rect.x + 25f + star * 42f, rect.y + 121f, 36f, 36f), starTexture, ScaleMode.ScaleToFit, true);
                    }
                }
                GUI.color = White;
            }
            else
            {
                DrawLock(new Rect(rect.x + 49f, rect.y + 30f, 70f, 82f));
                GUI.Label(new Rect(rect.x + 8f, rect.y + 119f, rect.width - 16f, 38f), "LOCKED", lockedStyle);
            }
        }

        private void DrawLock(Rect rect)
        {
            DrawRoundedPanel(new Rect(rect.x + 13f, rect.y, rect.width - 26f, 54f), Color.clear, Navy, 20);
            DrawRoundedPanel(new Rect(rect.x, rect.y + 34f, rect.width, 52f), Navy, Darken(Navy, 0.12f), 14);
            GUI.color = Cream;
            DrawCircle(new Rect(rect.center.x - 7f, rect.y + 51f, 14f, 14f));
            GUI.DrawTexture(new Rect(rect.center.x - 3f, rect.y + 62f, 6f, 12f), Texture2D.whiteTexture);
            GUI.color = White;
        }

        private void StartLevel(int level)
        {
            BottleGameController game = GetComponent<BottleGameController>();
            if (game == null) game = gameObject.AddComponent<BottleGameController>();
            game.BeginLevel(level);
            enabled = false;
        }

        private void ReturnToMenu()
        {
            MainMenuController menu = GetComponent<MainMenuController>();
            if (menu == null) menu = gameObject.AddComponent<MainMenuController>();
            menu.enabled = true;
            enabled = false;
        }

        private bool DrawRoundedButton(Rect rect, string text, Color fill, Color border, GUIStyle textStyle)
        {
            GUIStyle background = GetPanelStyle(fill, border, 34);
            GUI.color = Shadow;
            GUI.Box(new Rect(rect.x + 6f, rect.y + 10f, rect.width, rect.height), GUIContent.none, background);
            GUI.color = White;
            return GUI.Button(rect, text, MergeBackground(textStyle, background));
        }

        private void DrawRoundedPanel(Rect rect, Color fill, Color border, int radius = 28)
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
            if (panelStyles.TryGetValue(key, out GUIStyle style)) return style;
            const int size = 96;
            int r = Mathf.Clamp(radius, 4, 44);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            const int borderWidth = 4;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool outer = InsideRoundedRect(x, y, size, size, r);
                bool inner = InsideRoundedRect(x - borderWidth, y - borderWidth, size - borderWidth * 2, size - borderWidth * 2, Mathf.Max(1, r - borderWidth));
                pixels[y * size + x] = !outer ? Color.clear : inner ? fill : border;
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            generatedTextures.Add(texture);
            int slice = r + 2;
            style = new GUIStyle
            {
                normal = { background = texture }, hover = { background = texture }, active = { background = texture },
                border = new RectOffset(slice, slice, slice, slice), padding = new RectOffset(8, 8, 8, 8)
            };
            panelStyles[key] = style;
            return style;
        }

        private Texture2D GetCircleTexture()
        {
            const string key = "circle-white";
            if (panelStyles.TryGetValue(key, out GUIStyle style)) return style.normal.background;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
            };
            var pixels = new Color[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.49f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float alpha = Mathf.Clamp01(radius - Vector2.Distance(new Vector2(x, y), center) + 1f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            generatedTextures.Add(texture);
            panelStyles[key] = new GUIStyle { normal = { background = texture } };
            return texture;
        }

        private void CreateStyles()
        {
            uiFont = Resources.Load<Font>("Fonts/Inter-Regular");
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial", "Liberation Sans" }, 32);
            backStyle = CreateTextStyle(58, Navy, FontStyle.Bold);
            titleStyle = CreateTextStyle(60, Navy, FontStyle.Bold);
            subtitleStyle = CreateTextStyle(31, Navy, FontStyle.Bold);
            levelStyle = CreateTextStyle(66, White, FontStyle.Bold);
            lockedStyle = CreateTextStyle(20, Navy, FontStyle.Bold);
        }

        private GUIStyle CreateTextStyle(int size, Color color, FontStyle fontStyle)
        {
            return new GUIStyle
            {
                font = uiFont, fontSize = size, fontStyle = fontStyle, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = color }, hover = { textColor = color }, active = { textColor = color },
                wordWrap = false, clipping = TextClipping.Clip
            };
        }

        private static GUIStyle MergeBackground(GUIStyle textStyle, GUIStyle background)
        {
            return new GUIStyle(textStyle)
            {
                normal = { background = background.normal.background }, hover = { background = background.normal.background },
                active = { background = background.normal.background }, border = background.border, padding = background.padding
            };
        }

        private static bool InsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            int nearestX = Mathf.Clamp(x, radius, width - radius - 1);
            int nearestY = Mathf.Clamp(y, radius, height - radius - 1);
            int dx = x - nearestX;
            int dy = y - nearestY;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static string GetStarsKey(int level) => $"BottleOrder.Level.{level}.Stars";
        private static Color Html(string value) { ColorUtility.TryParseHtmlString(value, out Color color); return color; }
        private static Color Darken(Color color, float amount) => new(Mathf.Clamp01(color.r - amount), Mathf.Clamp01(color.g - amount), Mathf.Clamp01(color.b - amount), color.a);
    }
}

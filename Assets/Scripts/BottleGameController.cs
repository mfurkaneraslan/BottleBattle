using System;
using System.Collections.Generic;
using UnityEngine;

namespace BottleBattle
{
    /// <summary>
    /// The first playable Bottle Battle mode. Players reorder the colored bottles
    /// on the upper shelf to match the hidden order on the lower shelf.
    /// </summary>
    public sealed class BottleGameController : MonoBehaviour
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
        private static readonly Color Orange = Html("#F57C1F");
        private static readonly Color Teal = Html("#36BEAD");
        private static readonly Color Grey = Html("#B9BDC2");
        private static readonly Color GreyDark = Html("#858B92");
        private static readonly Color Oak = Html("#C88538");
        private static readonly Color OakDark = Html("#8B5222");
        private static readonly Color SkyBlue = Html("#35BCE9");
        private static readonly Color DeepBlue = Html("#0879AA");
        private static readonly Color White = Color.white;
        private static readonly Color Shadow = new(0.08f, 0.18f, 0.24f, 0.22f);

        private static readonly Color[] BottleColors =
        {
            Coral, Cyan, Lime, Gold, Purple, Orange, Teal
        };

        private readonly Dictionary<string, GUIStyle> panelStyles = new();
        private readonly List<Texture2D> generatedTextures = new();
        private readonly List<int> currentOrder = new();
        private readonly List<int> targetOrder = new();
        private readonly List<Rect> upperBottleRects = new();

        private Font uiFont;
        private Texture2D completionStarTexture;
        private GUIStyle headerStyle;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle correctCountStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallButtonStyle;
        private GUIStyle bottleBrandStyle;
        private GUIStyle bottleNumberStyle;
        private GUIStyle bottleFooterStyle;
        private GUIStyle bottleMarkStyle;
        private GUIStyle completionStyle;
        private GUIStyle popupTitleStyle;
        private GUIStyle popupStatStyle;
        private GUIStyle popupSmallStyle;
        private GUIStyle earnedStarStyle;
        private GUIStyle emptyStarStyle;
        private GUIStyle depthPopupTitleStyle;
        private GUIStyle depthPopupTitleShadowStyle;
        private GUIStyle depthPopupStatStyle;
        private GUIStyle depthPopupSmallStyle;
        private GUIStyle depthPopupLevelStyle;

        private int currentLevel;
        private int moveCount;
        private int minimumMoves;
        private int earnedStars;
        private int bestMoveCount;
        private int draggedIndex = -1;
        private Vector2 dragPosition;
        private bool completed;
        private bool allLevelsCompleted;

        public void Begin()
        {
            enabled = true;
            int savedLevel = PlayerPrefs.GetInt(SavedLevelKey, 1);
            LoadLevel(Mathf.Clamp(savedLevel, 1, FinalLevel));
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.orientation = ScreenOrientation.Portrait;
        }

        private void OnEnable()
        {
            completionStarTexture ??= Resources.Load<Texture2D>("UI/completion-star");
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

        private void LoadLevel(int level)
        {
            currentLevel = Mathf.Clamp(level, 1, FinalLevel);
            completed = false;
            allLevelsCompleted = false;
            moveCount = 0;
            minimumMoves = 0;
            earnedStars = 0;
            bestMoveCount = PlayerPrefs.GetInt(GetBestMovesKey(currentLevel), 0);
            draggedIndex = -1;
            currentOrder.Clear();
            targetOrder.Clear();

            int bottleCount = GetBottleCount(currentLevel);
            var random = new System.Random(currentLevel * 7919 + 104729);
            int availableTypeCount = GetAvailableBottleTypeCount(currentLevel);

            var availableBottleIds = new List<int>(availableTypeCount);
            for (int index = 0; index < availableTypeCount; index++)
            {
                availableBottleIds.Add(index);
            }

            Shuffle(availableBottleIds, random);
            for (int index = 0; index < bottleCount; index++)
            {
                targetOrder.Add(availableBottleIds[index]);
            }
            Shuffle(targetOrder, random);
            currentOrder.AddRange(targetOrder);

            int maxStartingCorrect = bottleCount <= 3 ? 1 : Mathf.Max(1, bottleCount / 4);
            int shuffleAttempts = 0;
            do
            {
                Shuffle(currentOrder, random);
                shuffleAttempts++;
            }
            while ((OrdersMatch() || GetCorrectCount() > maxStartingCorrect) &&
                   bottleCount > 1 &&
                   shuffleAttempts < 128);

            minimumMoves = CalculateMinimumMoves(currentOrder, targetOrder);
        }

        private static int GetBottleCount(int level)
        {
            if (level == 1)
            {
                return 2;
            }

            if (level == 2)
            {
                return 3;
            }

            if (level <= 5)
            {
                return 4;
            }

            if (level <= 10)
            {
                return 5;
            }

            if (level <= 20)
            {
                return 6;
            }

            if (level <= 35)
            {
                return 7;
            }

            if (level <= 50)
            {
                return 8;
            }

            if (level <= 70)
            {
                return 9;
            }

            return 10;
        }

        private static int GetAvailableBottleTypeCount(int level)
        {
            if (level <= 10)
            {
                return 7;
            }

            if (level <= 30)
            {
                return 10;
            }

            if (level <= 60)
            {
                return 15;
            }

            return BottleSpriteCatalog.BottleCount;
        }

        private static void Shuffle(List<int> values, System.Random random)
        {
            for (int index = values.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                (values[index], values[other]) = (values[other], values[index]);
            }
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
            DrawHeader();
            DrawPuzzle();
            HandleDrag(Event.current);
            DrawBottomAction();
            if (completed)
            {
                DrawDepthCompletionPopup();
            }

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void DrawBackground()
        {
            GUI.color = Cream;
            GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, DesignHeight), Texture2D.whiteTexture);

            GUI.color = new Color(CreamDark.r, CreamDark.g, CreamDark.b, 0.55f);
            DrawCircle(new Rect(-90f, 410f, 220f, 220f));
            DrawCircle(new Rect(955f, 740f, 190f, 190f));
            DrawCircle(new Rect(-80f, 1370f, 210f, 210f));
            DrawCircle(new Rect(950f, 1560f, 200f, 200f));
            GUI.color = White;
        }

        private void DrawHeader()
        {
            if (DrawRoundedButton(
                    new Rect(48f, 55f, 124f, 104f),
                    "‹",
                    Cream,
                    CreamDark,
                    headerStyle))
            {
                ReturnToMenu();
            }

            GUI.Label(new Rect(210f, 62f, 660f, 72f), $"LEVEL {currentLevel}", titleStyle);
            GUI.Label(
                new Rect(210f, 128f, 660f, 48f),
                $"{currentLevel} / {FinalLevel}",
                subtitleStyle);

            DrawRoundedPanel(new Rect(894f, 66f, 132f, 76f), Navy, Darken(Navy, 0.16f), 28);
            GUI.Label(new Rect(902f, 77f, 116f, 50f), $"{currentOrder.Count}", smallButtonStyle);
        }

        private void DrawPuzzle()
        {
            GUI.Label(
                new Rect(100f, 225f, 880f, 60f),
                "DRAG ONTO ANOTHER BOTTLE",
                subtitleStyle);
            GUI.Label(
                new Rect(100f, 285f, 880f, 64f),
                $"{GetCorrectCount()} CORRECT",
                correctCountStyle);
            GUI.Label(
                new Rect(100f, 342f, 880f, 48f),
                $"MOVES: {moveCount}     MINIMUM: {minimumMoves}",
                subtitleStyle);
            DrawShelfArea(
                top: 355f,
                shelfTop: 730f,
                order: currentOrder,
                revealColors: true,
                interactive: true);

            GUI.Label(
                new Rect(100f, 900f, 880f, 60f),
                completed ? "PERFECT ORDER!" : "MATCH THIS ORDER",
                completed ? completionStyle : subtitleStyle);

            DrawShelfArea(
                top: 1010f,
                shelfTop: 1385f,
                order: targetOrder,
                revealColors: completed,
                interactive: false);
        }

        private void DrawShelfArea(
            float top,
            float shelfTop,
            List<int> order,
            bool revealColors,
            bool interactive)
        {
            const float left = 105f;
            const float right = 975f;
            float density = Mathf.InverseLerp(2f, 10f, order.Count);
            float gap = Mathf.Lerp(48f, 10f, density);
            float bottleWidth = Mathf.Min(
                142f,
                (right - left - gap * (order.Count - 1)) / order.Count);
            float usedWidth = bottleWidth * order.Count + gap * (order.Count - 1);
            float startX = left + (right - left - usedWidth) * 0.5f;
            float bottleHeight = Mathf.Lerp(310f, 230f, density);

            if (interactive)
            {
                upperBottleRects.Clear();
            }

            for (int index = 0; index < order.Count; index++)
            {
                float x = startX + index * (bottleWidth + gap);
                var bottleRect = new Rect(x, shelfTop - bottleHeight, bottleWidth, bottleHeight);

                if (interactive)
                {
                    upperBottleRects.Add(bottleRect);
                }

                if (interactive && draggedIndex == index)
                {
                    GUI.color = new Color(Navy.r, Navy.g, Navy.b, 0.12f);
                    DrawRoundedPanel(
                        new Rect(bottleRect.x, bottleRect.y + 35f, bottleRect.width, bottleRect.height - 35f),
                        new Color(Navy.r, Navy.g, Navy.b, 0.08f),
                        new Color(Navy.r, Navy.g, Navy.b, 0.18f),
                        25);
                    GUI.color = White;
                    continue;
                }

                Color color = revealColors
                    ? BottleColors[order[index] % BottleColors.Length]
                    : Grey;
                DrawBottle(bottleRect, color, order[index], revealColors);
            }

            DrawShelf(shelfTop);

            if (interactive && draggedIndex >= 0 && draggedIndex < order.Count)
            {
                Rect original = upperBottleRects[draggedIndex];
                var draggedRect = new Rect(
                    dragPosition.x - original.width * 0.5f,
                    Mathf.Clamp(dragPosition.y - original.height * 0.55f, top, shelfTop - original.height),
                    original.width,
                    original.height);
                GUI.color = new Color(1f, 1f, 1f, 0.94f);
                DrawBottle(
                    draggedRect,
                    BottleColors[order[draggedIndex] % BottleColors.Length],
                    order[draggedIndex],
                    true);
                GUI.color = White;
            }
        }

        private void DrawShelf(float shelfTop)
        {
            GUI.color = Shadow;
            DrawRoundedPanel(new Rect(72f, shelfTop + 15f, 936f, 72f), Shadow, Shadow, 24);
            GUI.color = White;
            DrawRoundedPanel(new Rect(60f, shelfTop, 960f, 70f), Oak, OakDark, 24);
            DrawRoundedPanel(new Rect(110f, shelfTop + 57f, 50f, 40f), Oak, OakDark, 12);
            DrawRoundedPanel(new Rect(920f, shelfTop + 57f, 50f, 40f), Oak, OakDark, 12);
        }

        private void DrawBottle(Rect rect, Color color, int identity, bool colored)
        {
            if (colored && DrawSpriteBottle(rect, identity))
            {
                return;
            }

            float capHeight = Mathf.Max(22f, rect.height * 0.10f);
            float neckWidth = rect.width * 0.48f;
            float neckHeight = rect.height * 0.13f;
            float neckX = rect.center.x - neckWidth * 0.5f;
            float bodyTop = rect.y + capHeight + neckHeight * 0.62f;
            float bodyHeight = rect.yMax - bodyTop;
            Color edge = colored ? Darken(color, 0.28f) : GreyDark;

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
                colored ? Darken(color, 0.13f) : Darken(Grey, 0.09f),
                Color.clear,
                Mathf.RoundToInt(rect.width * 0.07f));
            DrawRoundedPanel(
                new Rect(
                    rect.x + rect.width * 0.10f,
                    rect.yMax - bodyHeight * 0.13f,
                    rect.width * 0.80f,
                    bodyHeight * 0.09f),
                colored ? Darken(color, 0.17f) : Darken(Grey, 0.10f),
                Color.clear,
                10);

            DrawRoundedPanel(
                new Rect(neckX, rect.y + capHeight * 0.75f, neckWidth, neckHeight),
                color,
                edge,
                Mathf.RoundToInt(neckWidth * 0.25f));
            DrawRoundedPanel(
                new Rect(neckX - 5f, rect.y, neckWidth + 10f, capHeight),
                colored ? Lighten(color, 0.08f) : Lighten(Grey, 0.06f),
                edge,
                12);

            GUI.color = new Color(1f, 1f, 1f, colored ? 0.30f : 0.16f);
            for (int groove = 1; groove <= 3; groove++)
            {
                float grooveX = neckX - 5f + (neckWidth + 10f) * groove / 4f;
                GUI.DrawTexture(
                    new Rect(grooveX, rect.y + 4f, 2f, Mathf.Max(8f, capHeight - 8f)),
                    Texture2D.whiteTexture);
            }

            GUI.color = new Color(1f, 1f, 1f, colored ? 0.22f : 0.12f);
            DrawRoundedPanel(
                new Rect(rect.x + rect.width * 0.12f, bodyTop + 10f, rect.width * 0.17f, rect.height * 0.52f),
                GUI.color,
                Color.clear,
                10);
            GUI.color = new Color(1f, 1f, 1f, colored ? 0.46f : 0.20f);
            DrawCircle(
                new Rect(
                    rect.x + rect.width * 0.27f,
                    bodyTop + bodyHeight * 0.09f,
                    rect.width * 0.10f,
                    rect.width * 0.10f));
            GUI.color = White;

            if (colored)
            {
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
                    bottleBrandStyle);
                GUI.Label(
                    new Rect(labelRect.x, labelRect.y + labelHeight * 0.20f, labelRect.width, labelHeight * 0.54f),
                    $"{identity + 1:00}",
                    bottleNumberStyle);
                GUI.Label(
                    new Rect(labelRect.x, labelRect.y + labelHeight * 0.69f, labelRect.width, labelHeight * 0.23f),
                    "ORDER",
                    bottleFooterStyle);

                string mark = (identity % 4) switch
                {
                    0 => "●",
                    1 => "≈",
                    2 => "◆",
                    _ => "☀"
                };
                GUI.Label(
                    new Rect(rect.x, bodyTop + (rect.yMax - bodyTop) * 0.39f, rect.width, rect.height * 0.25f),
                    mark,
                    bottleMarkStyle);
            }
        }

        private bool DrawSpriteBottle(Rect rect, int identity)
        {
            if (!BottleSpriteCatalog.TryGet(
                    identity,
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

        private void HandleDrag(Event guiEvent)
        {
            if (completed || upperBottleRects.Count == 0)
            {
                return;
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
            {
                for (int index = 0; index < upperBottleRects.Count; index++)
                {
                    if (!upperBottleRects[index].Contains(guiEvent.mousePosition))
                    {
                        continue;
                    }

                    draggedIndex = index;
                    dragPosition = guiEvent.mousePosition;
                    guiEvent.Use();
                    break;
                }
            }
            else if (guiEvent.type == EventType.MouseDrag && draggedIndex >= 0)
            {
                dragPosition = guiEvent.mousePosition;
                guiEvent.Use();
            }
            else if (guiEvent.type == EventType.MouseUp && draggedIndex >= 0)
            {
                int droppedOnIndex = FindBottleAt(guiEvent.mousePosition);
                if (droppedOnIndex >= 0 && droppedOnIndex != draggedIndex)
                {
                    (currentOrder[draggedIndex], currentOrder[droppedOnIndex]) =
                        (currentOrder[droppedOnIndex], currentOrder[draggedIndex]);
                    moveCount++;
                }

                draggedIndex = -1;
                guiEvent.Use();
                CheckCompletion();
            }
        }

        private int FindBottleAt(Vector2 position)
        {
            for (int index = 0; index < upperBottleRects.Count; index++)
            {
                if (upperBottleRects[index].Contains(position))
                {
                    return index;
                }
            }

            return -1;
        }

        private void CheckCompletion()
        {
            completed = OrdersMatch();
            if (!completed)
            {
                return;
            }

            earnedStars = CalculateStars(moveCount, minimumMoves);
            int savedStars = PlayerPrefs.GetInt(GetStarsKey(currentLevel), 0);
            if (earnedStars > savedStars)
            {
                PlayerPrefs.SetInt(GetStarsKey(currentLevel), earnedStars);
            }

            if (bestMoveCount <= 0 || moveCount < bestMoveCount)
            {
                bestMoveCount = moveCount;
                PlayerPrefs.SetInt(GetBestMovesKey(currentLevel), bestMoveCount);
            }

            if (currentLevel < FinalLevel)
            {
                PlayerPrefs.SetInt(SavedLevelKey, currentLevel + 1);
            }
            else
            {
                PlayerPrefs.SetInt(SavedLevelKey, FinalLevel);
                allLevelsCompleted = true;
            }

            PlayerPrefs.Save();
        }

        private bool OrdersMatch()
        {
            if (currentOrder.Count != targetOrder.Count)
            {
                return false;
            }

            for (int index = 0; index < currentOrder.Count; index++)
            {
                if (currentOrder[index] != targetOrder[index])
                {
                    return false;
                }
            }

            return true;
        }

        private int GetCorrectCount()
        {
            int correctCount = 0;
            int comparedCount = Mathf.Min(currentOrder.Count, targetOrder.Count);
            for (int index = 0; index < comparedCount; index++)
            {
                if (currentOrder[index] == targetOrder[index])
                {
                    correctCount++;
                }
            }

            return correctCount;
        }

        private static int CalculateMinimumMoves(List<int> start, List<int> target)
        {
            int count = Mathf.Min(start.Count, target.Count);
            var targetPositions = new Dictionary<int, int>(count);
            for (int index = 0; index < count; index++)
            {
                targetPositions[target[index]] = index;
            }

            var visited = new bool[count];
            int minimum = 0;
            for (int startIndex = 0; startIndex < count; startIndex++)
            {
                if (visited[startIndex])
                {
                    continue;
                }

                int cycleLength = 0;
                int currentIndex = startIndex;
                while (!visited[currentIndex])
                {
                    visited[currentIndex] = true;
                    currentIndex = targetPositions[start[currentIndex]];
                    cycleLength++;
                }

                if (cycleLength > 1)
                {
                    minimum += cycleLength - 1;
                }
            }

            return minimum;
        }

        private static int CalculateStars(int moves, int minimum)
        {
            int extraMoves = Mathf.Max(0, moves - minimum);
            if (extraMoves <= 1)
            {
                return 3;
            }

            if (extraMoves <= 4)
            {
                return 2;
            }

            return 1;
        }

        private static string GetStarsKey(int level)
        {
            return $"BottleOrder.Level.{level}.Stars";
        }

        private static string GetBestMovesKey(int level)
        {
            return $"BottleOrder.Level.{level}.BestMoves";
        }

        private void DrawBottomAction()
        {
            if (completed)
            {
                return;
            }

            GUI.Label(
                new Rect(130f, 1570f, 820f, 80f),
                "Arrange the upper bottles in the correct order",
                subtitleStyle);
        }

        private void DrawCompletionPopup()
        {
            GUI.color = new Color(0.03f, 0.08f, 0.12f, 0.48f);
            GUI.DrawTexture(
                new Rect(0f, 0f, DesignWidth, DesignHeight),
                Texture2D.whiteTexture);
            GUI.color = White;

            Rect popupRect = new(130f, 375f, 820f, 1080f);
            GUI.color = Shadow;
            DrawRoundedPanel(
                new Rect(popupRect.x + 10f, popupRect.y + 16f, popupRect.width, popupRect.height),
                Shadow,
                Shadow,
                42);
            GUI.color = White;
            DrawRoundedPanel(popupRect, Cream, Gold, 42);

            GUI.Label(new Rect(185f, 445f, 710f, 90f), "LEVEL COMPLETE", popupTitleStyle);
            GUI.Label(new Rect(235f, 530f, 610f, 52f), $"LEVEL {currentLevel}", popupSmallStyle);

            for (int starIndex = 0; starIndex < 3; starIndex++)
            {
                GUI.Label(
                    new Rect(300f + starIndex * 160f, 610f, 160f, 150f),
                    "★",
                    starIndex < earnedStars ? earnedStarStyle : emptyStarStyle);
            }

            DrawRoundedPanel(new Rect(220f, 790f, 640f, 250f), CreamDark, Darken(CreamDark, 0.10f), 30);
            GUI.Label(
                new Rect(245f, 820f, 590f, 70f),
                $"SOLVED IN {moveCount} MOVES",
                popupStatStyle);
            GUI.Label(
                new Rect(245f, 890f, 590f, 58f),
                $"MINIMUM: {minimumMoves} MOVES",
                popupSmallStyle);
            GUI.Label(
                new Rect(245f, 948f, 590f, 58f),
                $"BEST: {bestMoveCount} MOVES",
                popupSmallStyle);

            string praise = earnedStars switch
            {
                3 => "BRILLIANT!",
                2 => "GREAT JOB!",
                _ => "LEVEL CLEARED!"
            };
            GUI.Label(new Rect(235f, 1060f, 610f, 66f), praise, completionStyle);

            string label = allLevelsCompleted ? "BACK TO MENU" : "NEXT LEVEL";
            if (DrawRoundedButton(
                    new Rect(230f, 1180f, 620f, 142f),
                    label,
                    Lime,
                    Darken(Lime, 0.24f),
                    buttonStyle))
            {
                if (allLevelsCompleted)
                {
                    ReturnToMenu();
                }
                else
                {
                    LoadLevel(currentLevel + 1);
                }
            }
        }

        private void DrawDepthCompletionPopup()
        {
            GUI.color = new Color(0.03f, 0.06f, 0.13f, 0.64f);
            GUI.DrawTexture(
                new Rect(0f, 0f, DesignWidth, DesignHeight),
                Texture2D.whiteTexture);
            GUI.color = White;

            Rect popupRect = new(120f, 365f, 840f, 1100f);
            GUI.color = Shadow;
            DrawRoundedPanel(
                new Rect(popupRect.x + 16f, popupRect.y + 24f, popupRect.width, popupRect.height),
                Shadow,
                Shadow,
                42);
            GUI.color = White;
            DrawRoundedPanel(popupRect, DeepBlue, Darken(Navy, 0.04f), 42);
            DrawRoundedPanel(
                new Rect(popupRect.x + 12f, popupRect.y + 14f, popupRect.width - 24f, popupRect.height - 28f),
                SkyBlue,
                Lighten(SkyBlue, 0.16f),
                36);

            GUI.BeginGroup(new Rect(132f, 379f, 816f, 1072f));
            DrawSunburst(new Vector2(408f, 391f));
            GUI.EndGroup();

            GUI.color = new Color(1f, 1f, 1f, 0.22f);
            DrawRoundedPanel(new Rect(156f, 397f, 768f, 22f), GUI.color, Color.clear, 10);
            GUI.color = White;

            Rect bannerRect = new(190f, 325f, 700f, 170f);
            GUI.color = Shadow;
            DrawRoundedPanel(
                new Rect(bannerRect.x + 9f, bannerRect.y + 13f, bannerRect.width, bannerRect.height),
                Shadow,
                Shadow,
                38);
            GUI.color = White;
            DrawRoundedPanel(bannerRect, Lighten(SkyBlue, 0.18f), DeepBlue, 38);
            GUI.Label(new Rect(194f, 355f, 700f, 105f), "COMPLETE", depthPopupTitleShadowStyle);
            GUI.Label(new Rect(190f, 348f, 700f, 105f), "COMPLETE", depthPopupTitleStyle);

            GUI.Label(
                new Rect(235f, 510f, 610f, 58f),
                $"LEVEL {currentLevel}",
                depthPopupLevelStyle);

            for (int starIndex = 0; starIndex < 3; starIndex++)
            {
                Rect starRect = starIndex switch
                {
                    0 => new Rect(255f, 640f, 150f, 150f),
                    1 => new Rect(452.5f, 557.5f, 175f, 175f),
                    _ => new Rect(675f, 640f, 150f, 150f)
                };
                DrawDepthStar(
                    starRect,
                    starIndex < earnedStars);
            }

            DrawRoundedPanel(
                new Rect(205f, 825f, 670f, 260f),
                DeepBlue,
                Darken(DeepBlue, 0.15f),
                32);
            GUI.color = new Color(1f, 1f, 1f, 0.16f);
            DrawRoundedPanel(new Rect(223f, 842f, 634f, 18f), GUI.color, Color.clear, 9);
            GUI.color = White;
            GUI.Label(
                new Rect(240f, 858f, 600f, 70f),
                $"SOLVED IN {moveCount} MOVES",
                depthPopupStatStyle);
            GUI.Label(
                new Rect(240f, 930f, 600f, 58f),
                $"MINIMUM: {minimumMoves} MOVES",
                depthPopupSmallStyle);
            GUI.Label(
                new Rect(240f, 990f, 600f, 58f),
                $"BEST: {bestMoveCount} MOVES",
                depthPopupSmallStyle);

            string praise = earnedStars switch
            {
                3 => "BRILLIANT!",
                2 => "GREAT JOB!",
                _ => "LEVEL CLEARED!"
            };
            GUI.Label(
                new Rect(235f, 1100f, 610f, 66f),
                praise,
                depthPopupStatStyle);

            if (DrawGlossyPopupButton(
                    new Rect(185f, 1215f, 320f, 140f),
                    "RETRY",
                    Coral,
                    Darken(Coral, 0.24f)))
            {
                LoadLevel(currentLevel);
                return;
            }

            string nextLabel = allLevelsCompleted ? "MENU" : "NEXT";
            if (DrawGlossyPopupButton(
                    new Rect(575f, 1215f, 320f, 140f),
                    nextLabel,
                    Gold,
                    Darken(Gold, 0.25f)))
            {
                if (allLevelsCompleted)
                {
                    ReturnToMenu();
                }
                else
                {
                    LoadLevel(currentLevel + 1);
                }
            }
        }

        private void DrawSunburst(Vector2 center)
        {
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.color = new Color(1f, 1f, 1f, 0.075f);
            for (int ray = 0; ray < 18; ray++)
            {
                GUI.matrix = previousMatrix;
                GUIUtility.RotateAroundPivot(ray * 20f, center);
                GUI.DrawTexture(
                    new Rect(center.x - 18f, center.y - 185f, 36f, 185f),
                    Texture2D.whiteTexture);
            }

            GUI.matrix = previousMatrix;
            GUI.color = White;
        }

        private void DrawDepthStar(Rect rect, bool earned)
        {
            if (completionStarTexture == null)
            {
                GUI.Label(rect, "★", earned ? earnedStarStyle : emptyStarStyle);
                return;
            }

            GUI.color = earned ? White : new Color(1f, 1f, 1f, 0.20f);
            GUI.DrawTexture(rect, completionStarTexture, ScaleMode.ScaleToFit, true);
            GUI.color = White;
        }

        private bool DrawGlossyPopupButton(
            Rect rect,
            string text,
            Color fill,
            Color border)
        {
            bool clicked = DrawRoundedButton(rect, text, fill, border, buttonStyle);
            GUI.color = new Color(1f, 1f, 1f, 0.20f);
            DrawRoundedPanel(
                new Rect(rect.x + 22f, rect.y + 16f, rect.width - 44f, 18f),
                GUI.color,
                Color.clear,
                9);
            GUI.color = White;
            return clicked;
        }

        private void ReturnToMenu()
        {
            MainMenuController menu = GetComponent<MainMenuController>();
            if (menu == null)
            {
                menu = gameObject.AddComponent<MainMenuController>();
            }

            menu.enabled = true;
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

            const int borderWidth = 4;
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
            uiFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Liberation Sans" },
                32);

            headerStyle = CreateTextStyle(58, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            titleStyle = CreateTextStyle(62, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            subtitleStyle = CreateTextStyle(29, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            correctCountStyle = CreateTextStyle(42, Coral, FontStyle.Bold, TextAnchor.MiddleCenter);
            buttonStyle = CreateTextStyle(46, White, FontStyle.Bold, TextAnchor.MiddleCenter);
            smallButtonStyle = CreateTextStyle(34, White, FontStyle.Bold, TextAnchor.MiddleCenter);
            bottleBrandStyle = CreateTextStyle(14, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            bottleNumberStyle = CreateTextStyle(38, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            bottleFooterStyle = CreateTextStyle(12, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            bottleMarkStyle = CreateTextStyle(1, Color.clear, FontStyle.Normal, TextAnchor.MiddleCenter);
            completionStyle = CreateTextStyle(34, Lime, FontStyle.Bold, TextAnchor.MiddleCenter);
            popupTitleStyle = CreateTextStyle(54, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            popupStatStyle = CreateTextStyle(34, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            popupSmallStyle = CreateTextStyle(27, Navy, FontStyle.Bold, TextAnchor.MiddleCenter);
            earnedStarStyle = CreateTextStyle(112, Gold, FontStyle.Bold, TextAnchor.MiddleCenter);
            emptyStarStyle = CreateTextStyle(112, Grey, FontStyle.Bold, TextAnchor.MiddleCenter);
            depthPopupTitleStyle = CreateTextStyle(66, White, FontStyle.Bold, TextAnchor.MiddleCenter);
            depthPopupTitleShadowStyle = CreateTextStyle(66, DeepBlue, FontStyle.Bold, TextAnchor.MiddleCenter);
            depthPopupStatStyle = CreateTextStyle(35, White, FontStyle.Bold, TextAnchor.MiddleCenter);
            depthPopupSmallStyle = CreateTextStyle(27, White, FontStyle.Bold, TextAnchor.MiddleCenter);
            depthPopupLevelStyle = CreateTextStyle(32, DeepBlue, FontStyle.Bold, TextAnchor.MiddleCenter);
        }

        private GUIStyle CreateTextStyle(int size, Color color, FontStyle fontStyle, TextAnchor alignment)
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

        private static bool InsideRoundedRect(int x, int y, int width, int height, int radius)
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

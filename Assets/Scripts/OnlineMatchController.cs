using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace BottleBattle
{
    /// <summary>
    /// Lightweight Firebase Realtime Database matchmaking for the WebGL prototype.
    /// One waiting slot pairs two players, then both clients receive the same seed.
    /// </summary>
    public sealed class OnlineMatchController : MonoBehaviour
    {
        private const string DatabaseUrl =
            "https://bottle-battle-ec459-default-rtdb.europe-west1.firebasedatabase.app";
        private const string PlayerIdKey = "BottleBattle.Online.PlayerId";
        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;
        private const long WaitingTimeoutMs = 120000;

        private enum ScreenState
        {
            Hidden,
            Searching,
            Error,
            Playing
        }

        [Serializable]
        private sealed class WaitingRecord
        {
            public string playerId;
            public long createdAt;
        }

        [Serializable]
        private sealed class AssignmentRecord
        {
            public string roomId;
            public int seed;
            public int bottleCount;
            public string opponentId;
        }

        [Serializable]
        private sealed class RoomRecord
        {
            public string player1;
            public string player2;
            public int seed;
            public int bottleCount;
            public string winner;
            public long createdAt;
        }

        private readonly struct FirebaseResponse
        {
            public readonly bool Success;
            public readonly long StatusCode;
            public readonly string Text;
            public readonly string ETag;

            public FirebaseResponse(bool success, long statusCode, string text, string etag)
            {
                Success = success;
                StatusCode = statusCode;
                Text = text;
                ETag = etag;
            }
        }

        private ScreenState state;
        private string playerId;
        private string roomId;
        private string opponentId;
        private string errorMessage = string.Empty;
        private int searchVersion;
        private bool activeRoom;
        private bool completionSubmitted;
        private float searchStartedAt;

        private Font uiFont;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statusStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallStyle;
        private readonly Dictionary<string, Texture2D> roundedTextures = new();
        private Texture2D circleTexture;

        private static readonly Color Cream = Html("#FFF8E8");
        private static readonly Color CreamDark = Html("#F5E5C2");
        private static readonly Color Navy = Html("#123E64");
        private static readonly Color Coral = Html("#FF5266");
        private static readonly Color Cyan = Html("#15AFE0");
        private static readonly Color Gold = Html("#FFB817");
        private static readonly Color White = Color.white;
        private static readonly Color Shadow = new(0.08f, 0.18f, 0.24f, 0.22f);

        public void Begin()
        {
            enabled = true;
            EnsureStyles();
            playerId = PlayerPrefs.GetString(PlayerIdKey, string.Empty);
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(PlayerIdKey, playerId);
                PlayerPrefs.Save();
            }

            BeginSearch();
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in roundedTextures.Values)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
            if (circleTexture != null)
            {
                Destroy(circleTexture);
            }
        }

        public void SubmitCompletion(int moves)
        {
            if (!activeRoom || completionSubmitted)
            {
                return;
            }

            completionSubmitted = true;
            StartCoroutine(ClaimWinner());
        }

        public void ForfeitAndExit()
        {
            if (activeRoom && !string.IsNullOrEmpty(opponentId))
            {
                StartCoroutine(SetWinnerIfEmpty(opponentId));
            }
            ExitToMenu();
        }

        public void ExitToMenu()
        {
            activeRoom = false;
            completionSubmitted = false;
            searchVersion++;
            state = ScreenState.Hidden;

            BottleGameController game = GetComponent<BottleGameController>();
            if (game != null)
            {
                game.enabled = false;
            }

            LevelSelectController levelSelect = GetComponent<LevelSelectController>();
            if (levelSelect != null)
            {
                levelSelect.enabled = false;
            }

            MainMenuController menu = GetComponent<MainMenuController>();
            if (menu == null)
            {
                menu = gameObject.AddComponent<MainMenuController>();
            }
            menu.enabled = true;
        }

        private void BeginSearch()
        {
            StopAllCoroutines();
            searchVersion++;
            int version = searchVersion;
            state = ScreenState.Searching;
            errorMessage = string.Empty;
            activeRoom = false;
            completionSubmitted = false;
            roomId = string.Empty;
            opponentId = string.Empty;
            searchStartedAt = Time.unscaledTime;
            StartCoroutine(MatchmakingLoop(version));
        }

        private IEnumerator MatchmakingLoop(int version)
        {
            while (version == searchVersion && state == ScreenState.Searching)
            {
                FirebaseResponse assignmentResponse = default;
                yield return Send("GET", AssignmentPath(playerId), null, null,
                    response => assignmentResponse = response);
                if (assignmentResponse.Success && !IsNullJson(assignmentResponse.Text))
                {
                    AssignmentRecord assignment = JsonUtility.FromJson<AssignmentRecord>(assignmentResponse.Text);
                    if (assignment != null && !string.IsNullOrEmpty(assignment.roomId))
                    {
                        StartMatchedGame(assignment);
                        yield break;
                    }
                }

                FirebaseResponse waitingResponse = default;
                yield return Send("GET", "/online/waiting.json", null, "true",
                    response => waitingResponse = response);
                if (!waitingResponse.Success)
                {
                    SetError("Unable to reach matchmaking. Please try again.");
                    yield break;
                }

                if (IsNullJson(waitingResponse.Text))
                {
                    var mine = new WaitingRecord
                    {
                        playerId = playerId,
                        createdAt = NowMilliseconds()
                    };
                    FirebaseResponse placeResponse = default;
                    yield return Send(
                        "PUT",
                        "/online/waiting.json",
                        JsonUtility.ToJson(mine),
                        waitingResponse.ETag,
                        response => placeResponse = response);
                    if (!placeResponse.Success && placeResponse.StatusCode != 412)
                    {
                        SetError("Unable to enter matchmaking. Please try again.");
                        yield break;
                    }
                }
                else
                {
                    WaitingRecord waiting = JsonUtility.FromJson<WaitingRecord>(waitingResponse.Text);
                    if (waiting != null && waiting.playerId == playerId)
                    {
                        // Already waiting for the second player.
                    }
                    else if (waiting == null ||
                             string.IsNullOrEmpty(waiting.playerId) ||
                             NowMilliseconds() - waiting.createdAt > WaitingTimeoutMs)
                    {
                        FirebaseResponse cleanupResponse = default;
                        yield return Send("DELETE", "/online/waiting.json", null, waitingResponse.ETag,
                            response => cleanupResponse = response);
                    }
                    else
                    {
                        FirebaseResponse claimResponse = default;
                        yield return Send("DELETE", "/online/waiting.json", null, waitingResponse.ETag,
                            response => claimResponse = response);
                        if (claimResponse.Success)
                        {
                            yield return CreateRoom(waiting.playerId);
                            yield break;
                        }
                    }
                }

                yield return new WaitForSecondsRealtime(1.1f);
            }
        }

        private IEnumerator CreateRoom(string waitingPlayerId)
        {
            string newRoomId = Guid.NewGuid().ToString("N");
            int seed = Guid.NewGuid().GetHashCode() & int.MaxValue;
            if (seed == 0)
            {
                seed = 104729;
            }
            int bottleCount = 10 + seed % 3;
            var room = new RoomRecord
            {
                player1 = waitingPlayerId,
                player2 = playerId,
                seed = seed,
                bottleCount = bottleCount,
                winner = string.Empty,
                createdAt = NowMilliseconds()
            };
            var waitingAssignment = new AssignmentRecord
            {
                roomId = newRoomId,
                seed = seed,
                bottleCount = bottleCount,
                opponentId = playerId
            };
            var myAssignment = new AssignmentRecord
            {
                roomId = newRoomId,
                seed = seed,
                bottleCount = bottleCount,
                opponentId = waitingPlayerId
            };

            FirebaseResponse roomResponse = default;
            yield return Send("PUT", RoomPath(newRoomId), JsonUtility.ToJson(room), null,
                response => roomResponse = response);
            if (!roomResponse.Success)
            {
                SetError("Unable to create the match room.");
                yield break;
            }

            FirebaseResponse firstAssignment = default;
            yield return Send("PUT", AssignmentPath(waitingPlayerId), JsonUtility.ToJson(waitingAssignment), null,
                response => firstAssignment = response);
            FirebaseResponse secondAssignment = default;
            yield return Send("PUT", AssignmentPath(playerId), JsonUtility.ToJson(myAssignment), null,
                response => secondAssignment = response);
            if (!firstAssignment.Success || !secondAssignment.Success)
            {
                SetError("Unable to connect both players to the room.");
                yield break;
            }

            StartMatchedGame(myAssignment);
        }

        private void StartMatchedGame(AssignmentRecord assignment)
        {
            roomId = assignment.roomId;
            opponentId = assignment.opponentId;
            activeRoom = true;
            completionSubmitted = false;
            state = ScreenState.Playing;
            StartCoroutine(Send("DELETE", AssignmentPath(playerId), null, null, _ => { }));
            StartCoroutine(PollWinner());

            BottleGameController game = GetComponent<BottleGameController>();
            if (game == null)
            {
                game = gameObject.AddComponent<BottleGameController>();
            }
            game.BeginOnlineMatch(assignment.seed, assignment.bottleCount, this);
        }

        private IEnumerator PollWinner()
        {
            while (activeRoom && !string.IsNullOrEmpty(roomId))
            {
                FirebaseResponse response = default;
                yield return Send("GET", WinnerPath(roomId), null, null, value => response = value);
                if (response.Success)
                {
                    string winner = ParseJsonString(response.Text);
                    if (!string.IsNullOrEmpty(winner))
                    {
                        ResolveResult(winner == playerId);
                        yield break;
                    }
                }
                yield return new WaitForSecondsRealtime(0.75f);
            }
        }

        private IEnumerator ClaimWinner()
        {
            FirebaseResponse current = default;
            yield return Send("GET", WinnerPath(roomId), null, "true", response => current = response);
            if (!current.Success)
            {
                completionSubmitted = false;
                yield break;
            }

            string winner = ParseJsonString(current.Text);
            if (!string.IsNullOrEmpty(winner))
            {
                ResolveResult(winner == playerId);
                yield break;
            }

            FirebaseResponse claim = default;
            yield return Send("PUT", WinnerPath(roomId), QuoteJson(playerId), current.ETag,
                response => claim = response);
            if (claim.Success)
            {
                ResolveResult(true);
            }
            else
            {
                completionSubmitted = false;
            }
        }

        private IEnumerator SetWinnerIfEmpty(string winnerId)
        {
            FirebaseResponse current = default;
            yield return Send("GET", WinnerPath(roomId), null, "true", response => current = response);
            if (current.Success && string.IsNullOrEmpty(ParseJsonString(current.Text)))
            {
                yield return Send("PUT", WinnerPath(roomId), QuoteJson(winnerId), current.ETag, _ => { });
            }
        }

        private void ResolveResult(bool won)
        {
            if (!activeRoom)
            {
                return;
            }
            activeRoom = false;
            BottleGameController game = GetComponent<BottleGameController>();
            if (game != null)
            {
                game.ReceiveOnlineResult(won);
            }
        }

        private void CancelSearch()
        {
            searchVersion++;
            StopAllCoroutines();
            state = ScreenState.Hidden;
            StartCoroutine(RemoveOwnWaitingRecord());
            ExitToMenu();
        }

        private IEnumerator RemoveOwnWaitingRecord()
        {
            FirebaseResponse waiting = default;
            yield return Send("GET", "/online/waiting.json", null, "true", response => waiting = response);
            if (waiting.Success && !IsNullJson(waiting.Text))
            {
                WaitingRecord record = JsonUtility.FromJson<WaitingRecord>(waiting.Text);
                if (record != null && record.playerId == playerId)
                {
                    yield return Send("DELETE", "/online/waiting.json", null, waiting.ETag, _ => { });
                }
            }
        }

        private void SetError(string message)
        {
            errorMessage = message;
            state = ScreenState.Error;
        }

        private IEnumerator Send(
            string method,
            string path,
            string json,
            string conditionalEtag,
            Action<FirebaseResponse> completed)
        {
            using var request = new UnityWebRequest(DatabaseUrl + path, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 15
            };
            if (json != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            if (conditionalEtag == "true")
            {
                request.SetRequestHeader("X-Firebase-ETag", "true");
            }
            else if (!string.IsNullOrEmpty(conditionalEtag))
            {
                request.SetRequestHeader("If-Match", conditionalEtag);
            }

            yield return request.SendWebRequest();
            bool success = request.result == UnityWebRequest.Result.Success;
            completed?.Invoke(new FirebaseResponse(
                success,
                request.responseCode,
                request.downloadHandler?.text ?? string.Empty,
                request.GetResponseHeader("ETag")));
        }

        private void OnGUI()
        {
            if (state == ScreenState.Hidden || state == ScreenState.Playing)
            {
                return;
            }
            EnsureStyles();

            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            Rect safeArea = Screen.safeArea;
            float scale = Mathf.Min(safeArea.width / DesignWidth, safeArea.height / DesignHeight);
            float offsetX = safeArea.x + (safeArea.width - DesignWidth * scale) * 0.5f;
            float safeTop = Screen.height - safeArea.yMax;
            float offsetY = safeTop + (safeArea.height - DesignHeight * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY), Quaternion.identity, new Vector3(scale, scale, 1f));

            GUI.color = Cream;
            GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, DesignHeight), Texture2D.whiteTexture);
            GUI.color = new Color(CreamDark.r, CreamDark.g, CreamDark.b, 0.65f);
            DrawCircle(new Rect(-90f, 420f, 230f, 230f));
            DrawCircle(new Rect(940f, 1280f, 220f, 220f));
            GUI.color = White;

            GUI.Label(new Rect(150f, 230f, 780f, 100f), "ONLINE MATCH", titleStyle);
            DrawPanel(new Rect(120f, 440f, 840f, 800f), White, CreamDark);

            if (state == ScreenState.Searching)
            {
                float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4f) * 0.5f;
                GUI.color = Color.Lerp(Cyan, Gold, pulse);
                DrawCircle(new Rect(430f, 555f, 220f, 220f));
                GUI.color = Cream;
                DrawCircle(new Rect(475f, 600f, 130f, 130f));
                GUI.color = White;
                GUI.Label(new Rect(190f, 825f, 700f, 75f), "SEARCHING FOR AN OPPONENT", statusStyle);
                int dots = 1 + Mathf.FloorToInt(Time.unscaledTime * 1.5f) % 3;
                GUI.Label(new Rect(250f, 905f, 580f, 65f), new string('.', dots), titleStyle);
                GUI.Label(
                    new Rect(225f, 1000f, 630f, 60f),
                    $"WAITING: {Mathf.FloorToInt(Time.unscaledTime - searchStartedAt)}s",
                    smallStyle);
                if (DrawButton(new Rect(280f, 1100f, 520f, 120f), "CANCEL", Coral))
                {
                    CancelSearch();
                }
            }
            else
            {
                GUI.Label(new Rect(210f, 600f, 660f, 90f), "CONNECTION ERROR", statusStyle);
                GUI.Label(new Rect(210f, 730f, 660f, 160f), errorMessage, smallStyle);
                if (DrawButton(new Rect(220f, 1000f, 300f, 120f), "MENU", Coral))
                {
                    CancelSearch();
                }
                if (DrawButton(new Rect(560f, 1000f, 300f, 120f), "RETRY", Cyan))
                {
                    BeginSearch();
                }
            }

            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        private void EnsureStyles()
        {
            if (uiFont != null)
            {
                return;
            }
            uiFont = Resources.Load<Font>("Fonts/Inter-Regular");
            if (uiFont == null)
            {
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            titleStyle = Style(62, Navy, TextAnchor.MiddleCenter);
            subtitleStyle = Style(34, Navy, TextAnchor.MiddleCenter);
            statusStyle = Style(36, Navy, TextAnchor.MiddleCenter);
            buttonStyle = Style(40, White, TextAnchor.MiddleCenter);
            smallStyle = Style(27, Navy, TextAnchor.MiddleCenter);
            smallStyle.wordWrap = true;
        }

        private bool DrawButton(Rect rect, string text, Color fill)
        {
            GUI.color = Shadow;
            DrawPanel(new Rect(rect.x + 7f, rect.y + 10f, rect.width, rect.height), Shadow, Shadow);
            GUI.color = White;
            GUIStyle style = new(buttonStyle) { normal = { background = GetRoundedTexture(fill, Darken(fill, 0.18f)) } };
            return GUI.Button(rect, text, style);
        }

        private void DrawPanel(Rect rect, Color fill, Color border)
        {
            GUI.DrawTexture(rect, GetRoundedTexture(fill, border), ScaleMode.StretchToFill, true);
        }

        private Texture2D GetRoundedTexture(Color fill, Color border)
        {
            string key = ColorUtility.ToHtmlStringRGBA(fill) + ColorUtility.ToHtmlStringRGBA(border);
            if (roundedTextures.TryGetValue(key, out Texture2D existing))
            {
                return existing;
            }
            const int size = 96;
            const int radius = 26;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool outer = InsideRounded(x, y, size, radius);
                    bool inner = InsideRounded(x - 4, y - 4, size - 8, radius - 4);
                    pixels[y * size + x] = !outer ? Color.clear : inner ? fill : border;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            roundedTextures[key] = texture;
            return texture;
        }

        private void DrawCircle(Rect rect)
        {
            if (circleTexture == null)
            {
                const int size = 64;
                circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear
                };
                var pixels = new Color[size * size];
                Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float alpha = Mathf.Clamp01(size * 0.49f - Vector2.Distance(new Vector2(x, y), center) + 1f);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
                circleTexture.SetPixels(pixels);
                circleTexture.Apply(false, true);
            }
            GUI.DrawTexture(rect, circleTexture, ScaleMode.StretchToFill, true);
        }

        private GUIStyle Style(int size, Color color, TextAnchor anchor)
        {
            return new GUIStyle
            {
                font = uiFont,
                fontSize = size,
                fontStyle = FontStyle.Bold,
                alignment = anchor,
                normal = { textColor = color },
                hover = { textColor = color },
                active = { textColor = color }
            };
        }

        private static bool InsideRounded(int x, int y, int size, int radius)
        {
            if (x < 0 || y < 0 || x >= size || y >= size)
            {
                return false;
            }
            int nx = Mathf.Clamp(x, radius, size - radius - 1);
            int ny = Mathf.Clamp(y, radius, size - radius - 1);
            int dx = x - nx;
            int dy = y - ny;
            return dx * dx + dy * dy <= radius * radius;
        }

        private static bool IsNullJson(string text)
        {
            return string.IsNullOrWhiteSpace(text) || text.Trim() == "null";
        }

        private static string ParseJsonString(string json)
        {
            if (IsNullJson(json))
            {
                return string.Empty;
            }
            string value = json.Trim();
            return value.Length >= 2 && value[0] == '"' && value[^1] == '"'
                ? value.Substring(1, value.Length - 2)
                : value;
        }

        private static string QuoteJson(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static long NowMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private static string AssignmentPath(string id) => $"/online/assignments/{id}.json";
        private static string RoomPath(string id) => $"/online/rooms/{id}.json";
        private static string WinnerPath(string id) => $"/online/rooms/{id}/winner.json";

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
    }
}

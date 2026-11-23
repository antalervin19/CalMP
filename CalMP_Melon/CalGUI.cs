using Steamworks;
using Steamworks.Data;
using UnityEngine;
using Color = UnityEngine.Color;

namespace CalMP_Melon
{
    public class CalGUI
    {
        private static Steam steam => Steam.Instance;

        private static readonly Color BgColor = new Color(0.14f, 0.14f, 0.15f, 0.95f);
        private static readonly Color BorderColor = new Color(0.35f, 0.35f, 0.37f, 1f);
        private static readonly Color LineColor = new Color(0.28f, 0.28f, 0.30f, 1f);
        private static readonly Color BtnNormal = new Color(0.24f, 0.24f, 0.26f, 1f);
        private static readonly Color BtnHover = new Color(0.32f, 0.32f, 0.35f, 1f);
        private static readonly Color BtnActive = new Color(0.18f, 0.18f, 0.20f, 1f);

        private static readonly Color TextMain = new Color(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color TextDim = new Color(0.68f, 0.68f, 0.70f, 1f);

        private const int WindowId = 0xCA11;
        private const float WindowWidth = 310f;
        private static Rect windowRect = new Rect(20, 20, WindowWidth, 0);

        private const bool UseSystemFont = true;
        private const string SystemFontName = "Segoe UI";

        private static bool initialized;
        private static Font uiFont;
        private static Texture2D whiteTex;
        private static Texture2D lineTex;
        private static GUIStyle windowStyle;
        private static GUIStyle headerStyle, bodyStyle, dimStyle, buttonStyle, toastStyle;

        private static string lastToastMessage;
        private static float toastTotalTime = 5f;
        private static string copiedLobbyId;
        private static float copiedUntil;

        public static void DrawInterface()
        {
            EnsureStyles();
            float scale = UiScale();
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            try
            {
                windowRect.height = 0f;

                windowRect = GUILayout.Window(
                    WindowId, windowRect, DrawWindow, GUIContent.none, windowStyle,
                    GUILayout.Width(WindowWidth));

                float vw = Screen.width / scale;
                float vh = Screen.height / scale;
                windowRect.x = Mathf.Clamp(windowRect.x, 0, Mathf.Max(0, vw - 60));
                windowRect.y = Mathf.Clamp(windowRect.y, 0, Mathf.Max(0, vh - 40));
            }
            finally
            {
                GUI.matrix = oldMatrix;
            }
        }

        private static void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cats are Liquid MP", headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("[F1] hide", headerStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            DrawRule();
            GUILayout.Space(10);

            if (steam == null)
            {
                if (Steam.InitializationError != null)
                {
                    GUILayout.Label("Steam init failed:", bodyStyle);
                    GUILayout.Label(Steam.InitializationError, dimStyle);
                }
                else
                {
                    GUILayout.Label("Waiting for Steam...", dimStyle);
                }
            }
            else if (!Steam.IsInLobby)
            {
                GUILayout.Label("Not in a lobby.", dimStyle);
                GUILayout.Space(8);

                if (GUILayout.Button("Host Lobby", buttonStyle))
                    _ = steam.CreateLobbyAsync(2, "Cats are Liquid co-op");
            }
            else
            {
                DrawLobby();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 36));
        }

        private static void DrawLobby()
        {
            Lobby lobby = Steam.CurrentLobby;
            string lobbyId = lobby.Id.ToString();

            GUILayout.Label("Lobby ID", dimStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(lobbyId, bodyStyle);
            GUILayout.FlexibleSpace();

            bool justCopied = copiedLobbyId == lobbyId && Time.realtimeSinceStartup < copiedUntil;
            if (GUILayout.Button(justCopied ? "Copied" : "Copy", buttonStyle, GUILayout.Width(64)))
            {
                GUIUtility.systemCopyBuffer = lobbyId;
                copiedLobbyId = lobbyId;
                copiedUntil = Time.realtimeSinceStartup + 1.5f;
                Core.ShowNotification("Lobby ID copied.", 2f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label($"Players ({lobby.MemberCount}/{lobby.MaxMembers})", dimStyle);
            GUILayout.Space(4);

            SteamId ownerId = lobby.Owner.Id;
            foreach (Friend player in lobby.Members)
            {
                string name = string.IsNullOrEmpty(player.Name) ? "?" : player.Name;
                string tags = "";
                if (player.Id == ownerId) tags += " (host)";
                if (player.IsMe) tags += " (you)";

                GUILayout.Label(Truncate(name, bodyStyle, WindowWidth - 110f) + tags, bodyStyle);
            }

            GUILayout.Space(8);
            GUILayout.Label("Remote cat: Green (needs pre-placed NPC)", dimStyle);
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Invite Friend", buttonStyle))
                SteamFriends.OpenGameInviteOverlay(lobby.Id);
            GUILayout.Space(6);
            if (GUILayout.Button("Leave", buttonStyle, GUILayout.Width(72)))
                steam.LeaveLobby();
            GUILayout.EndHorizontal();
        }

        public static void ShowNotificationInterface(string message, float timeRemaining)
        {
            EnsureStyles();

            if (message != lastToastMessage || timeRemaining > toastTotalTime)
            {
                lastToastMessage = message;
                toastTotalTime = Mathf.Max(0.1f, timeRemaining);
            }

            float scale = UiScale();
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            try
            {
                const float width = 300f;
                const float pad = 10f;
                float textWidth = width - pad * 2f;
                float textHeight = toastStyle.CalcHeight(new GUIContent(message), textWidth);
                float height = textHeight + pad * 2f;

                float elapsed = toastTotalTime - timeRemaining;
                float alpha = Mathf.Clamp01(Mathf.Min(elapsed / 0.2f, timeRemaining / 0.35f));

                float virtualHeight = Screen.height / scale;
                Rect rect = new Rect(20f, virtualHeight - height - 20f, width, height);

                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Box(rect, GUIContent.none, windowStyle);
                GUI.Label(new Rect(rect.x + pad, rect.y + pad, textWidth, textHeight), message, toastStyle);
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }

        private static void DrawRule()
        {
            GUILayout.Box(GUIContent.none, new GUIStyle { normal = { background = lineTex } },
                GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        private static string Truncate(string text, GUIStyle style, float maxWidth)
        {
            if (style.CalcSize(new GUIContent(text)).x <= maxWidth)
                return text;

            int len = text.Length;
            while (len > 1 && style.CalcSize(new GUIContent(text.Substring(0, len) + "…")).x > maxWidth)
                len--;
            return text.Substring(0, len).TrimEnd() + "…";
        }

        private static float UiScale()
        {
            return Mathf.Clamp(Screen.height / 1080f, 0.85f, 2.5f);
        }

        private static void EnsureStyles()
        {
            if (initialized && whiteTex != null)
                return;

            uiFont = null;
            if (UseSystemFont)
            {
                try { uiFont = Font.CreateDynamicFontFromOSFont(SystemFontName, 16); }
                catch { uiFont = null; }
            }

            whiteTex = MakeTexture(Color.white);
            lineTex = MakeTexture(LineColor);

            Texture2D boxTex = MakeBox(BgColor, BorderColor, 1);

            windowStyle = new GUIStyle
            {
                padding = new RectOffset(12, 12, 12, 12),
                border = new RectOffset(1, 1, 1, 1),
                normal = { background = boxTex },
                onNormal = { background = boxTex },
            };

            headerStyle = MakeLabel(14, FontStyle.Bold, TextMain);
            bodyStyle = MakeLabel(13, FontStyle.Normal, TextMain);
            dimStyle = MakeLabel(12, FontStyle.Normal, TextDim);
            dimStyle.wordWrap = true;

            toastStyle = MakeLabel(12, FontStyle.Normal, TextMain);
            toastStyle.wordWrap = true;

            buttonStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = 28,
                padding = new RectOffset(10, 10, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(1, 1, 1, 1),
                normal = { background = MakeBox(BtnNormal, BorderColor, 1), textColor = TextMain },
                hover = { background = MakeBox(BtnHover, BorderColor, 1), textColor = TextMain },
                active = { background = MakeBox(BtnActive, BorderColor, 1), textColor = TextMain },
            };
            if (uiFont != null) buttonStyle.font = uiFont;

            initialized = true;
        }

        private static GUIStyle MakeLabel(int size, FontStyle fontStyle, Color color)
        {
            GUIStyle style = new GUIStyle
            {
                fontSize = size,
                fontStyle = fontStyle,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                margin = new RectOffset(0, 0, 0, 2),
                padding = new RectOffset(0, 0, 0, 0),
            };
            style.normal.textColor = color;
            if (uiFont != null) style.font = uiFont;
            return style;
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }

        private static Texture2D MakeBox(Color fill, Color border, int borderPx)
        {
            int size = borderPx * 2 + 2;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < borderPx || y < borderPx || x >= size - borderPx || y >= size - borderPx;
                    pixels[y * size + x] = edge ? border : fill;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
using Steamworks;
using Steamworks.Data;
using System;
using UnityEngine;
using Color = UnityEngine.Color;

namespace CalMP_Melon
{
    public class CalGUI
    {
        private static Steam steam => Steam.Instance;

        // ---------- Palette ---------- (writer: antalervin19, i shot myself in the dick with this task...)
        private static readonly Color PanelColor = new Color(0.09f, 0.09f, 0.09f, 0.97f);
        private static readonly Color CardColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        private static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.07f);

        private static readonly Color TextColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        private static readonly Color SecondaryTextColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        private static readonly Color MutedColor = new Color(0.60f, 0.60f, 0.60f, 1f);

        private static readonly Color AccentColor = new Color(0.90f, 0.54f, 0.26f, 1f);
        private static readonly Color DangerColor = new Color(0.90f, 0.54f, 0.26f, 1f);
        private static readonly Color NeutralColor = new Color(1f, 1f, 1f, 0.08f);

        private static readonly Color GoodColor = TextColor;
        private static readonly Color WarnColor = AccentColor;

        private const int WindowId = 0xCA11;
        private const float WindowWidth = 320f;
        private static Rect windowRect = new Rect(20, 20, WindowWidth, 0);

        private static bool initialized;
        private static Texture2D roundedTex, circleTex, whiteTex;
        private static GUIStyle windowStyle, titleStyle, subtitleStyle, labelStyle, mutedStyle, sectionStyle, cardStyle, badgeStyle, avatarStyle, dividerStyle, toastStyle, toastTitleStyle;
        private static GUIStyle accentButton, dangerButton, neutralButton, smallButton; // should i stack these too into the line above? xD

        private static string lastToastMessage;
        private static float toastTotalTime = 5f;

        public static void DrawInterface()
        {
            EnsureStyles();
            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale(), UiScale(), 1f));

            try
            {
                windowRect.height = 0f;

                windowRect = GUILayout.Window(
                    WindowId, windowRect, DrawWindow, GUIContent.none, windowStyle,
                    GUILayout.Width(WindowWidth));

                float vw = Screen.width / UiScale();
                float vh = Screen.height / UiScale();
                windowRect.x = Mathf.Clamp(windowRect.x, 0, Mathf.Max(0, vw - 60));
                windowRect.y = Mathf.Clamp(windowRect.y, 0, Mathf.Max(0, vh - 40));
            }
            finally
            {
                GUI.matrix = oldMatrix;
            }
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
                // didnt really have time to make a nice toast system, so this is just a quick and dirty ahh implementation

                const float width = 300f;
                const float pad = 12f;
                const float stripeW = 3f;
                const float textGap = 10f;
                const float barArea = 14f;

                toastStyle.alignment = TextAnchor.UpperLeft;
                float textX = pad + stripeW + textGap;
                float textWidth = width - textX - pad;
                float textHeight = toastStyle.CalcHeight(new GUIContent(message), textWidth);
                float height = pad + textHeight + barArea;

                float elapsed = toastTotalTime - timeRemaining;
                float alpha = Mathf.Clamp01(Mathf.Min(elapsed / 0.25f, timeRemaining / 0.4f));
                float slide = (1f - alpha) * 10f;

                float virtualHeight = Screen.height / scale;
                Rect rect = new Rect(20f, virtualHeight - height - 24f + slide, width, height);

                GUI.color = new Color(1f, 1f, 1f, alpha);
                GUI.Box(rect, GUIContent.none, windowStyle);

                DrawTinted(new Rect(rect.x + pad, rect.y + pad, stripeW, textHeight), whiteTex,
                    new Color(AccentColor.r, AccentColor.g, AccentColor.b, alpha));

                GUI.Label(new Rect(rect.x + textX, rect.y + pad, textWidth, textHeight), message, toastStyle);

                float progress = Mathf.Clamp01(timeRemaining / toastTotalTime);
                DrawTinted(new Rect(rect.x + pad, rect.yMax - 8f, (rect.width - pad * 2f) * progress, 2f), whiteTex,
                    new Color(AccentColor.r, AccentColor.g, AccentColor.b, alpha * 0.8f));
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }

        private static void DrawWindow(int id)
        {
            DrawHeader();
            GUILayout.Space(8);
            Divider();
            GUILayout.Space(8);

            if (steam == null)
            {
                bool failed = Steam.InitializationError != null;
                DrawStatusRow(failed ? "Steam unavailable" : "Waiting for Steam...",
                    failed ? Steam.InitializationError : "The game is still initializing Steam.",
                    failed ? DangerColor : WarnColor);
            }
            else if (!Steam.IsInLobby)
            {
                DrawStatusRow("Not in a lobby", "Host a lobby, or accept a Steam invite from a friend.", WarnColor);
                GUILayout.Space(10);

                if (GUILayout.Button("Host Lobby", accentButton))
                    _ = steam.CreateLobbyAsync(2, "Cats are Liquid co-op");
            }
            else
            {
                DrawLobby();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 48));
        }

        private static void DrawHeader()
        {
            GUILayout.BeginHorizontal(GUILayout.Height(30));

            Color dot = steam == null
                ? (Steam.InitializationError != null ? DangerColor : WarnColor)
                : (Steam.IsInLobby ? GoodColor : WarnColor);
            Rect dotRect = GUILayoutUtility.GetRect(10, 10, GUILayout.Width(10), GUILayout.Height(30));
            DrawTinted(new Rect(dotRect.x, dotRect.y + 10, 10, 10), circleTex, dot);

            GUILayout.Space(4);
            GUILayout.BeginVertical();
            GUILayout.Label("Cats are Liquid", titleStyle);
            GUILayout.Label("Multiplayer", subtitleStyle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.Label("F1 to hide", subtitleStyle, GUILayout.Height(30));
            GUILayout.EndHorizontal();
        }

        private static void DrawStatusRow(string title, string detail, Color color)
        {
            GUILayout.BeginVertical(cardStyle);
            Color old = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(title, labelStyle);
            GUI.contentColor = old;
            GUILayout.Label(detail, mutedStyle);
            GUILayout.EndVertical();
        }

        private static void DrawLobby()
        {
            Lobby lobby = Steam.CurrentLobby;

            GUILayout.BeginVertical(cardStyle);
            GUILayout.Label("LOBBY ID", sectionStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label(lobby.Id.ToString(), labelStyle, GUILayout.Height(24));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Copy", smallButton, GUILayout.Width(54)))
            {
                GUIUtility.systemCopyBuffer = lobby.Id.ToString();
                Core.ShowNotification("Lobby ID copied to clipboard.", 2f);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(8);

            GUILayout.Label($"PLAYERS  {lobby.MemberCount}/{lobby.MaxMembers}", sectionStyle);
            SteamId ownerId = lobby.Owner.Id;
            foreach (Friend player in lobby.Members)
                DrawPlayerRow(player, player.Id == ownerId);

            GUILayout.Space(6);
            GUILayout.Label("Remote cat: Green (pre-placed NPC required)", mutedStyle);
            GUILayout.Space(8);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Invite Friend", accentButton))
                SteamFriends.OpenGameInviteOverlay(lobby.Id);
            GUILayout.Space(6);
            if (GUILayout.Button("Leave", dangerButton, GUILayout.Width(80)))
                steam.LeaveLobby();
            GUILayout.EndHorizontal();
        }

        private static void DrawPlayerRow(Friend player, bool isHost)
        {
            GUILayout.BeginHorizontal(cardStyle, GUILayout.Height(40));

            Rect avatar = GUILayoutUtility.GetRect(28, 28, GUILayout.Width(28), GUILayout.Height(28));
            float hue = (player.Id.Value % 360UL) / 360f;
            DrawTinted(avatar, circleTex, Color.HSVToRGB(hue, 0.45f, 0.80f));
            string name = player.Name ?? "?";
            GUI.Label(avatar, name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "?", avatarStyle);

            GUILayout.Space(6);
            GUILayout.Label(name, labelStyle, GUILayout.Height(28));
            GUILayout.FlexibleSpace();

            if (isHost) DrawBadge("HOST", WarnColor);
            if (player.IsMe) DrawBadge("YOU", AccentColor);

            GUILayout.EndHorizontal();
        }

        private static void DrawBadge(string text, Color color)
        {
            Color oldBg = GUI.backgroundColor;
            Color oldContent = GUI.contentColor;
            GUI.backgroundColor = new Color(color.r, color.g, color.b, 0.25f);
            GUI.contentColor = color;
            GUILayout.Label(text, badgeStyle, GUILayout.Height(18));
            GUI.backgroundColor = oldBg;
            GUI.contentColor = oldContent;
        }

        private static void Divider()
        {
            GUILayout.Box(GUIContent.none, dividerStyle, GUILayout.Height(1), GUILayout.ExpandWidth(true));
        }

        private static void DrawTinted(Rect rect, Texture tex, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, tex);
            GUI.color = old;
        }

        private static float UiScale()
        {
            return Mathf.Clamp(Screen.height / 1080f, 0.85f, 2.5f);
        }

        private static void EnsureStyles()
        {
            if (initialized && roundedTex != null)
                return;

            whiteTex = MakeTexture(Color.white);
            roundedTex = MakeRounded(Color.white, 32, 10);
            circleTex = MakeRounded(Color.white, 32, 16);
            Texture2D panelTex = MakeRounded(PanelColor, 32, 12);
            Texture2D cardTex = MakeRounded(CardColor, 32, 8);
            Texture2D badgeTex = MakeRounded(Color.white, 16, 6);

            windowStyle = new GUIStyle
            {
                padding = new RectOffset(14, 14, 12, 14),
                border = new RectOffset(12, 12, 12, 12),
                normal = { background = panelTex },
                onNormal = { background = panelTex },
            };

            titleStyle = MakeLabel(15, FontStyle.Bold, TextColor);
            subtitleStyle = MakeLabel(11, FontStyle.Normal, SecondaryTextColor);
            labelStyle = MakeLabel(13, FontStyle.Normal, TextColor);
            mutedStyle = MakeLabel(11, FontStyle.Normal, SecondaryTextColor);
            sectionStyle = MakeLabel(10, FontStyle.Bold, MutedColor);
            mutedStyle.wordWrap = true;
            sectionStyle.margin = new RectOffset(2, 2, 0, 4);

            cardStyle = new GUIStyle
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 0, 4),
                border = new RectOffset(8, 8, 8, 8),
                normal = { background = cardTex },
            };

            badgeStyle = new GUIStyle
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(7, 7, 0, 0),
                margin = new RectOffset(3, 0, 5, 5),
                border = new RectOffset(6, 6, 6, 6),
                normal = { background = badgeTex, textColor = Color.white },
            };

            avatarStyle = MakeLabel(13, FontStyle.Bold, new Color(0.08f, 0.09f, 0.12f, 1f));
            avatarStyle.alignment = TextAnchor.MiddleCenter;

            dividerStyle = new GUIStyle { normal = { background = MakeTexture(DividerColor) } };

            toastStyle = MakeLabel(12, FontStyle.Normal, TextColor);
            toastStyle.wordWrap = true;
            toastTitleStyle = MakeLabel(9, FontStyle.Bold, MutedColor);
            toastTitleStyle.fixedHeight = 16;

            accentButton = MakeButton(AccentColor, 34, 13);
            dangerButton = MakeButton(DangerColor, 34, 13);
            neutralButton = MakeButton(NeutralColor, 34, 13);
            smallButton = MakeButton(NeutralColor, 22, 11);

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
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
            };
            style.normal.textColor = color;
            return style;
        }

        private static GUIStyle MakeButton(Color baseColor, float height, int fontSize)
        {
            Color hover = Color.Lerp(baseColor, Color.white, 0.18f);
            Color pressed = Color.Lerp(baseColor, Color.black, 0.22f);
            hover.a = Mathf.Min(1f, baseColor.a + 0.10f);
            pressed.a = baseColor.a;

            GUIStyle style = new GUIStyle
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                fixedHeight = height,
                padding = new RectOffset(10, 10, 4, 4),
                margin = new RectOffset(0, 0, 2, 2),
                border = new RectOffset(10, 10, 10, 10),
            };
            style.normal.background = MakeRounded(baseColor, 32, 10);
            style.hover.background = MakeRounded(hover, 32, 10);
            style.active.background = MakeRounded(pressed, 32, 10);
            style.normal.textColor = style.hover.textColor = style.active.textColor = Color.white;
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

        private static Texture2D MakeRounded(Color color, int size, int radius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;
                    float cx = Mathf.Clamp(px, radius, size - radius);
                    float cy = Mathf.Clamp(py, radius, size - radius);
                    float dist = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                    float a = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, color.a * a);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
using System;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ParrySense;

[BepInPlugin(
    MyPluginInfo.PLUGIN_GUID,
    MyPluginInfo.PLUGIN_NAME,
    MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private static Plugin _instance;
    private Harmony _harmony;

    // =====================================================================
    // State
    // =====================================================================

    private static float _lastTimingMs;
    private static TimingResult _lastResult = TimingResult.None;
    private static float _displayUntil;

    private static bool _hasPendingMissedImpact;
    private static float _lastMissedImpactTime;

    private static bool _processingLocalDamage;
    private static bool _blockAttackSeenDuringDamage;

    // Toggle notification
    private static string _toggleMessage = "";
    private static float _toggleMessageUntil;

    // =====================================================================
    // Configuration
    // =====================================================================

    private ConfigEntry<bool> _enabled;
    private ConfigEntry<KeyboardShortcut> _toggleKey;

    private ConfigEntry<float> _positionX;
    private ConfigEntry<float> _positionY;
    private ConfigEntry<float> _displayDuration;

    private ConfigEntry<float> _panelWidth;
    private ConfigEntry<float> _panelHeight;
    private ConfigEntry<float> _backgroundOpacity;

    private ConfigEntry<float> _tooEarlyLimit;
    private ConfigEntry<float> _tooLateLimit;
    private ConfigEntry<float> _outsideDisplayRange;

    // =====================================================================
    // GUI
    // =====================================================================

    private GUIStyle _titleStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _toggleMessageStyle;

    private enum TimingResult
    {
        None,
        TooEarly,
        Parry,
        TooLate
    }

    // =====================================================================
    // Lifecycle
    // =====================================================================

    private void Awake()
    {
        Logger = base.Logger;
        _instance = this;

        BindConfig();

        _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _harmony.PatchAll();

        Logger.LogInfo(
            $"ParryTrainer {MyPluginInfo.PLUGIN_VERSION} loaded"
        );
    }

    private void Update()
    {
        /*
         * For a simple key such as F8, read the Unity key directly.
         * This avoids relying on KeyboardShortcut.IsDown().
         */
        if (Input.GetKeyDown(_toggleKey.Value.MainKey))
        {
            _enabled.Value = !_enabled.Value;

            ClearRuntimeState();

            ShowToggleMessage(
                _enabled.Value
            );

            string state =
                _enabled.Value
                    ? "enabled"
                    : "disabled";

            Logger.LogInfo(
                $"ParryTrainer {state}"
            );
        }

        /*
         * Expire an old pending impact even if the player never blocks.
         */
        if (_hasPendingMissedImpact)
        {
            float elapsed =
                Time.unscaledTime -
                _lastMissedImpactTime;

            if (elapsed > GetTooLateLimitSeconds())
            {
                _hasPendingMissedImpact = false;
            }
        }
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }

    // =====================================================================
    // Configuration
    // =====================================================================

    private void BindConfig()
    {
        _enabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "Enable or disable ParryTrainer."
        );

        _toggleKey = Config.Bind(
            "General",
            "ToggleKey",
            new KeyboardShortcut(KeyCode.F8),
            "Keyboard shortcut used to enable or disable ParryTrainer."
        );

        _positionX = Config.Bind(
            "HUD",
            "PositionX",
            45f,
            "Horizontal HUD position in pixels from the left edge of the screen."
        );

        _positionY = Config.Bind(
            "HUD",
            "PositionY",
            160f,
            "Vertical HUD position in pixels from the top edge of the screen."
        );

        _displayDuration = Config.Bind(
            "HUD",
            "DisplayDuration",
            3.0f,
            "How long the timing result remains visible, in seconds."
        );

        _panelWidth = Config.Bind(
            "HUD",
            "PanelWidth",
            360f,
            "Width of the HUD panel in pixels."
        );

        _panelHeight = Config.Bind(
            "HUD",
            "PanelHeight",
            75f,
            "Height of the HUD panel in pixels."
        );

        _backgroundOpacity = Config.Bind(
            "HUD",
            "BackgroundOpacity",
            0.45f,
            "Background opacity of the HUD panel. Valid range: 0 to 1."
        );

        _tooEarlyLimit = Config.Bind(
            "Training",
            "TooEarlyLimit",
            1.0f,
            "Maximum time in seconds before impact that can be reported as TOO EARLY. Blocks started earlier than this are ignored."
        );

        _tooLateLimit = Config.Bind(
            "Training",
            "TooLateLimit",
            0.5f,
            "Maximum time in seconds after impact that can be reported as TOO LATE."
        );

        _outsideDisplayRange = Config.Bind(
            "Training",
            "OutsideDisplayRangeMs",
            250f,
            "Amount of time represented by each dotted area outside the parry window. Larger errors are visually clamped, but the real timing value is still displayed."
        );
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    internal static bool IsEnabled()
    {
        return
            _instance != null &&
            _instance._enabled.Value;
    }

    private static float GetTooLateLimitSeconds()
    {
        if (_instance == null)
            return 0.5f;

        return Mathf.Max(
            0f,
            _instance._tooLateLimit.Value
        );
    }

    private static float GetTooEarlyLimitSeconds()
    {
        if (_instance == null)
            return 1.0f;

        return Mathf.Max(
            0.25f,
            _instance._tooEarlyLimit.Value
        );
    }

    private static void ClearRuntimeState()
    {
        _lastResult = TimingResult.None;
        _displayUntil = 0f;

        _hasPendingMissedImpact = false;
        _lastMissedImpactTime = 0f;

        _processingLocalDamage = false;
        _blockAttackSeenDuringDamage = false;
    }

    private static void ShowToggleMessage(
        bool enabled)
    {
        bool french =
            IsFrench();

        if (french)
        {
            _toggleMessage =
                enabled
                    ? "ENTRAÎNEMENT À LA PARADE : ACTIVÉ"
                    : "ENTRAÎNEMENT À LA PARADE : DÉSACTIVÉ";
        }
        else
        {
            _toggleMessage =
                enabled
                    ? "PARRY TRAINING: ENABLED"
                    : "PARRY TRAINING: DISABLED";
        }

        _toggleMessageUntil =
            Time.unscaledTime +
            1.5f;
    }

    // =====================================================================
    // Timing logic
    // =====================================================================

    internal static void ShowBlockTiming(
        float milliseconds)
    {
        if (!IsEnabled())
            return;

        _hasPendingMissedImpact = false;

        /*
         * Valheim's timed-block window:
         *
         * 0 ms <= timing < 250 ms
         */
        if (milliseconds < 250f)
        {
            ShowResult(
                TimingResult.Parry,
                milliseconds
            );

            return;
        }

        /*
         * Ignore ordinary long-held blocks.
         */
        float earlyLimitMs =
            GetTooEarlyLimitSeconds() *
            1000f;

        if (milliseconds <= earlyLimitMs)
        {
            ShowResult(
                TimingResult.TooEarly,
                milliseconds
            );
        }
    }

    internal static void BeginLocalDamage()
    {
        if (!IsEnabled())
            return;

        _processingLocalDamage = true;
        _blockAttackSeenDuringDamage = false;
    }

    internal static void MarkBlockAttackSeen()
    {
        if (!IsEnabled())
            return;

        if (_processingLocalDamage)
        {
            _blockAttackSeenDuringDamage = true;
        }
    }

    internal static void EndLocalDamage(
        bool hasAttacker)
    {
        if (!IsEnabled())
            return;

        if (!_processingLocalDamage)
            return;

        /*
         * No BlockAttack during this damage event:
         *
         * remember the actual impact briefly so that a new block
         * started immediately afterwards can be classified as TOO LATE.
         */
        if (!_blockAttackSeenDuringDamage &&
            hasAttacker)
        {
            _lastMissedImpactTime =
                Time.unscaledTime;

            _hasPendingMissedImpact =
                true;
        }

        _processingLocalDamage = false;
        _blockAttackSeenDuringDamage = false;
    }

    internal static void OnBlockStarted()
    {
        if (!IsEnabled())
            return;

        if (!_hasPendingMissedImpact)
            return;

        float elapsed =
            Time.unscaledTime -
            _lastMissedImpactTime;

        float maxDelay =
            GetTooLateLimitSeconds();

        if (elapsed < 0f ||
            elapsed > maxDelay)
        {
            _hasPendingMissedImpact = false;
            return;
        }

        /*
         * Signed timing convention:
         *
         * +360 ms = block started 360 ms before impact
         * +100 ms = block started 100 ms before impact
         *    0 ms = impact
         * -150 ms = block started 150 ms after impact
         */
        float milliseconds =
            -(elapsed * 1000f);

        _hasPendingMissedImpact = false;

        ShowResult(
            TimingResult.TooLate,
            milliseconds
        );
    }

    private static void ShowResult(
        TimingResult result,
        float milliseconds)
    {
        if (!IsEnabled())
            return;

        _lastResult = result;

        _lastTimingMs =
            Mathf.Round(
                milliseconds
            );

        _displayUntil =
            Time.unscaledTime +
            Mathf.Max(
                0.1f,
                _instance._displayDuration.Value
            );

        Logger.LogInfo(
            $"{result} | timing = {_lastTimingMs:F0} ms"
        );
    }

    // =====================================================================
    // HUD
    // =====================================================================

    private void OnGUI()
    {
        bool showToggleMessage =
            !string.IsNullOrEmpty(_toggleMessage) &&
            Time.unscaledTime <= _toggleMessageUntil;

        bool showTrainingResult =
            _enabled.Value &&
            _lastResult != TimingResult.None &&
            Time.unscaledTime <= _displayUntil;

        /*
         * Important:
         *
         * Do not return merely because the trainer is disabled.
         * The "DISABLED" notification must remain visible for 1.5 seconds.
         */
        if (!showToggleMessage &&
            !showTrainingResult)
        {
            return;
        }

        EnsureStyles();

        if (showToggleMessage)
        {
            DrawToggleMessage();
        }

        if (!showTrainingResult)
            return;

        float panelX =
            _positionX.Value;

        float panelY =
            _positionY.Value;

        float panelWidth =
            Mathf.Max(
                280f,
                _panelWidth.Value
            );

        float panelHeight =
            Mathf.Max(
                70f,
                _panelHeight.Value
            );

        DrawBackground(
            panelX,
            panelY,
            panelWidth,
            panelHeight
        );

        DrawTitle(
            panelX,
            panelY,
            panelWidth
        );

        DrawTimeline(
            panelX,
            panelY,
            panelWidth
        );
    }

    private void DrawToggleMessage()
    {
        const float width =
            600f;

        const float height =
            50f;

        float x =
            (Screen.width - width) *
            0.5f;

        float y =
            (Screen.height - height) *
            0.5f;

        GUI.Label(
            new Rect(
                x,
                y,
                width,
                height
            ),
            _toggleMessage,
            _toggleMessageStyle
        );
    }

    private void DrawBackground(
        float x,
        float y,
        float width,
        float height)
    {
        Color previousColor =
            GUI.color;

        float opacity =
            Mathf.Clamp01(
                _backgroundOpacity.Value
            );

        GUI.color =
            new Color(
                0f,
                0f,
                0f,
                opacity
            );

        GUI.DrawTexture(
            new Rect(
                x,
                y,
                width,
                height
            ),
            Texture2D.whiteTexture
        );

        GUI.color =
            previousColor;
    }

    private void DrawTitle(
        float panelX,
        float panelY,
        float panelWidth)
    {
        string title =
            GetResultText(
                _lastResult
            );

        string value =
            $"{_lastTimingMs:F0} ms";

        _statusStyle.normal.textColor =
            GetResultColor(
                _lastResult
            );

        GUI.Label(
            new Rect(
                panelX + 8f,
                panelY + 4f,
                panelWidth - 16f,
                25f
            ),
            $"{title}  {value}",
            _statusStyle
        );
    }

    private void DrawTimeline(
        float panelX,
        float panelY,
        float panelWidth)
    {
        /*
         *
         *   TOO EARLY             PARRY WINDOW              TOO LATE
         *
         *   . . . . . |===============================| . . . . .
         *             250 ms                        Impact
         *
         */

        const float sideMargin =
            14f;

        float timelineLeft =
            panelX +
            sideMargin;

        float timelineRight =
            panelX +
            panelWidth -
            sideMargin;

        float totalWidth =
            timelineRight -
            timelineLeft;

        /*
         * Layout:
         *
         * 20% early dotted zone
         * 60% real parry window
         * 20% late dotted zone
         */
        float outsideWidth =
            totalWidth *
            0.20f;

        float windowWidth =
            totalWidth *
            0.60f;

        float windowStart =
            timelineLeft +
            outsideWidth;

        float impactX =
            windowStart +
            windowWidth;

        float lineY =
            panelY +
            43f;

        Color previousColor =
            GUI.color;

        // -----------------------------------------------------------------
        // Early dotted zone
        // -----------------------------------------------------------------

        GUI.color =
            GetResultColor(
                TimingResult.TooEarly
            );

        DrawDottedLine(
            timelineLeft,
            windowStart,
            lineY
        );

        // -----------------------------------------------------------------
        // Parry window
        // -----------------------------------------------------------------

        GUI.color =
            GetResultColor(
                TimingResult.Parry
            );

        GUI.DrawTexture(
            new Rect(
                windowStart,
                lineY,
                windowWidth,
                2f
            ),
            Texture2D.whiteTexture
        );

        // -----------------------------------------------------------------
        // Late dotted zone
        // -----------------------------------------------------------------

        GUI.color =
            GetResultColor(
                TimingResult.TooLate
            );

        DrawDottedLine(
            impactX,
            timelineRight,
            lineY
        );

        // -----------------------------------------------------------------
        // Boundaries
        // -----------------------------------------------------------------

        GUI.color =
            Color.white;

        GUI.DrawTexture(
            new Rect(
                windowStart,
                lineY - 4f,
                2f,
                10f
            ),
            Texture2D.whiteTexture
        );

        GUI.DrawTexture(
            new Rect(
                impactX,
                lineY - 4f,
                2f,
                10f
            ),
            Texture2D.whiteTexture
        );

        // -----------------------------------------------------------------
        // Timing marker
        // -----------------------------------------------------------------

        float markerX =
            CalculateMarkerPosition(
                timelineLeft,
                windowStart,
                impactX,
                timelineRight
            );

        GUI.color =
            GetResultColor(
                _lastResult
            );

        GUI.DrawTexture(
            new Rect(
                markerX - 4f,
                lineY - 4f,
                8f,
                8f
            ),
            Texture2D.whiteTexture
        );

        GUI.color =
            previousColor;

        // -----------------------------------------------------------------
        // Labels
        // -----------------------------------------------------------------

        GUI.Label(
            new Rect(
                windowStart - 27f,
                lineY + 6f,
                58f,
                18f
            ),
            "250 ms",
            _labelStyle
        );

        GUI.Label(
            new Rect(
                impactX - 28f,
                lineY + 6f,
                58f,
                18f
            ),
            "Impact",
            _labelStyle
        );
    }

    private float CalculateMarkerPosition(
        float timelineLeft,
        float windowStart,
        float impactX,
        float timelineRight)
    {
        // -----------------------------------------------------------------
        // PARRY
        //
        // 250 ms -> start of solid line
        //   0 ms -> Impact
        // -----------------------------------------------------------------

        if (_lastTimingMs >= 0f &&
            _lastTimingMs <= 250f)
        {
            float normalized =
                _lastTimingMs /
                250f;

            return Mathf.Lerp(
                impactX,
                windowStart,
                normalized
            );
        }

        // -----------------------------------------------------------------
        // TOO EARLY
        // -----------------------------------------------------------------

        if (_lastTimingMs > 250f)
        {
            float earlyAmount =
                _lastTimingMs -
                250f;

            float displayRange =
                Mathf.Max(
                    1f,
                    _outsideDisplayRange.Value
                );

            float normalized =
                Mathf.Clamp01(
                    earlyAmount /
                    displayRange
                );

            return Mathf.Lerp(
                windowStart,
                timelineLeft,
                normalized
            );
        }

        // -----------------------------------------------------------------
        // TOO LATE
        // -----------------------------------------------------------------

        float lateAmount =
            Mathf.Abs(
                _lastTimingMs
            );

        float lateDisplayRange =
            Mathf.Max(
                1f,
                _outsideDisplayRange.Value
            );

        float lateNormalized =
            Mathf.Clamp01(
                lateAmount /
                lateDisplayRange
            );

        return Mathf.Lerp(
            impactX,
            timelineRight,
            lateNormalized
        );
    }

    private static void DrawDottedLine(
        float startX,
        float endX,
        float y)
    {
        const float dotWidth =
            3f;

        const float spacing =
            8f;

        for (
            float x = startX;
            x < endX;
            x += spacing)
        {
            float width =
                Mathf.Min(
                    dotWidth,
                    endX - x
                );

            GUI.DrawTexture(
                new Rect(
                    x,
                    y,
                    width,
                    2f
                ),
                Texture2D.whiteTexture
            );
        }
    }

    private void EnsureStyles()
    {
        if (_titleStyle == null)
        {
            _titleStyle =
                new GUIStyle(
                    GUI.skin.label
                )
                {
                    alignment =
                        TextAnchor.MiddleLeft,

                    fontSize =
                        16,

                    fontStyle =
                        FontStyle.Bold
                };
        }

        if (_statusStyle == null)
        {
            _statusStyle =
                new GUIStyle(
                    GUI.skin.label
                )
                {
                    alignment =
                        TextAnchor.MiddleLeft,

                    fontSize =
                        16,

                    fontStyle =
                        FontStyle.Bold
                };
        }

        if (_labelStyle == null)
        {
            _labelStyle =
                new GUIStyle(
                    GUI.skin.label
                )
                {
                    alignment =
                        TextAnchor.MiddleCenter,

                    fontSize =
                        10
                };
        }

        if (_toggleMessageStyle == null)
        {
            _toggleMessageStyle =
                new GUIStyle(
                    GUI.skin.label
                )
                {
                    alignment =
                        TextAnchor.MiddleCenter,

                    fontSize =
                        20,

                    fontStyle =
                        FontStyle.Bold
                };

            _toggleMessageStyle.normal.textColor =
                Color.yellow;
        }
    }

    private static Color GetResultColor(
        TimingResult result)
    {
        return result switch
        {
            TimingResult.TooEarly =>
                new Color(
                    0.35f,
                    0.70f,
                    1.00f,
                    1f
                ),

            TimingResult.Parry =>
                new Color(
                    0.35f,
                    1.00f,
                    0.45f,
                    1f
                ),

            TimingResult.TooLate =>
                new Color(
                    1.00f,
                    0.35f,
                    0.35f,
                    1f
                ),

            _ =>
                Color.white
        };
    }

    // =====================================================================
    // Localization
    // =====================================================================

    private static string GetResultText(
        TimingResult result)
    {
        bool french =
            IsFrench();

        if (french)
        {
            return result switch
            {
                TimingResult.TooEarly =>
                    "TROP TÔT",

                TimingResult.Parry =>
                    "PARADE",

                TimingResult.TooLate =>
                    "TROP TARD",

                _ =>
                    ""
            };
        }

        return result switch
        {
            TimingResult.TooEarly =>
                "TOO EARLY",

            TimingResult.Parry =>
                "PARRY",

            TimingResult.TooLate =>
                "TOO LATE",

            _ =>
                ""
        };
    }

    private static bool IsFrench()
    {
        try
        {
            Type localizationType =
                null;

            foreach (
                Assembly assembly
                in AppDomain.CurrentDomain.GetAssemblies())
            {
                localizationType =
                    assembly.GetType(
                        "Localization"
                    );

                if (localizationType != null)
                    break;
            }

            if (localizationType == null)
                return false;

            object instance =
                null;

            const BindingFlags staticFlags =
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Static;

            FieldInfo instanceField =
                localizationType.GetField(
                    "instance",
                    staticFlags
                );

            if (instanceField != null)
            {
                instance =
                    instanceField.GetValue(
                        null
                    );
            }
            else
            {
                PropertyInfo instanceProperty =
                    localizationType.GetProperty(
                        "instance",
                        staticFlags
                    );

                if (instanceProperty != null)
                {
                    instance =
                        instanceProperty.GetValue(
                            null,
                            null
                        );
                }
            }

            if (instance == null)
                return false;

            MethodInfo getSelectedLanguage =
                localizationType.GetMethod(
                    "GetSelectedLanguage",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance
                );

            if (getSelectedLanguage == null)
                return false;

            string language =
                getSelectedLanguage.Invoke(
                    instance,
                    null
                ) as string;

            if (string.IsNullOrEmpty(language))
                return false;

            return language.IndexOf(
                "French",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
        }
        catch (Exception exception)
        {
            Logger?.LogWarning(
                $"Could not detect Valheim language: {exception.Message}"
            );

            return false;
        }
    }
}

// ========================================================================
// Humanoid.BlockAttack
// ========================================================================

[HarmonyPatch(
    typeof(Humanoid),
    "BlockAttack"
)]
internal static class HumanoidBlockAttackPatch
{
    private static void Prefix(
        Humanoid __instance,
        float ___m_blockTimer,
        out float __state)
    {
        __state =
            -1f;

        if (!Plugin.IsEnabled())
            return;

        if (__instance != Player.m_localPlayer)
            return;

        Plugin.MarkBlockAttackSeen();

        __state =
            ___m_blockTimer;
    }

    private static void Postfix(
        Humanoid __instance,
        bool __result,
        float __state)
    {
        if (!Plugin.IsEnabled())
            return;

        if (__instance != Player.m_localPlayer)
            return;

        if (!__result)
            return;

        if (__state < 0f)
            return;

        Plugin.ShowBlockTiming(
            __state *
            1000f
        );
    }
}

// ========================================================================
// Humanoid.UpdateBlock
// ========================================================================

[HarmonyPatch(
    typeof(Humanoid),
    "UpdateBlock"
)]
internal static class HumanoidUpdateBlockPatch
{
    private static void Prefix(
        Humanoid __instance,
        float ___m_blockTimer,
        out float __state)
    {
        __state =
            ___m_blockTimer;
    }

    private static void Postfix(
        Humanoid __instance,
        float ___m_blockTimer,
        float __state)
    {
        if (!Plugin.IsEnabled())
            return;

        if (__instance != Player.m_localPlayer)
            return;

        /*
         * Valheim:
         *
         * if (m_blockTimer < 0f)
         *     m_blockTimer = 0f;
         *
         * Therefore this transition identifies the start of blocking.
         */
        bool blockJustStarted =
            __state < 0f &&
            ___m_blockTimer >= 0f;

        if (blockJustStarted)
        {
            Plugin.OnBlockStarted();
        }
    }
}

// ========================================================================
// Character.RPC_Damage
// ========================================================================

[HarmonyPatch(
    typeof(Character),
    "RPC_Damage",
    new Type[]
    {
        typeof(long),
        typeof(HitData)
    }
)]
internal static class CharacterRpcDamagePatch
{
    private static void Prefix(
        Character __instance)
    {
        if (!Plugin.IsEnabled())
            return;

        if (__instance != Player.m_localPlayer)
            return;

        Plugin.BeginLocalDamage();
    }

    private static void Postfix(
        Character __instance,
        HitData hit)
    {
        if (!Plugin.IsEnabled())
            return;

        if (__instance != Player.m_localPlayer)
            return;

        bool hasAttacker =
            false;

        try
        {
            Character attacker =
                hit?.GetAttacker();

            hasAttacker =
                attacker != null &&
                attacker != __instance;
        }
        catch
        {
            hasAttacker =
                false;
        }

        Plugin.EndLocalDamage(
            hasAttacker
        );
    }
}
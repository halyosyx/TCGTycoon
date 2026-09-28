using System;
using Game.Core.Common;
using Game.Core.Content;
using Game.Core.Inventory;
using Game.Core.Packs;
using Game.Unity.DebugTools;
using Game.Unity.Definitions;
using Game.Unity.UI.Hud;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Editor-hosted console for <see cref="PackDebugCommands"/>: open packs and inspect inventory
    /// without a scene. The inventory lives as long as the session; changing pack or seed resets it.
    /// </summary>
    public sealed class DebugConsoleWindow : EditorWindow
    {
        private const int DefaultSeed = 1;
        private const int MaxLogLength = 200_000;
        private const string CommandControlName = "TcgDebugConsoleCommand";
        private const string MonospaceFontName = "Consolas";
        private const int MonospaceFontSize = 12;
        private const float ButtonWidth = 90f;

        [SerializeField] private PackConfigDefinition _pack;
        [SerializeField] private int _seed = DefaultSeed;
        [SerializeField] private string _command = string.Empty;
        [SerializeField] private string _log = string.Empty;

        private PackDebugCommands _commands;
        private Vector2 _logScroll;
        private bool _refocusCommand;
        private Font _monospaceFont;
        private GUIStyle _logStyle;

        [MenuItem("TCG/Debug Console")]
        public static void Open() => GetWindow<DebugConsoleWindow>("TCG Debug Console");

        private GUIStyle LogStyle
        {
            get
            {
                if (_logStyle == null)
                {
                    _monospaceFont = Font.CreateDynamicFontFromOSFont(MonospaceFontName, MonospaceFontSize);
                    _monospaceFont.hideFlags = HideFlags.HideAndDontSave;
                    _logStyle = new GUIStyle(EditorStyles.textArea) { font = _monospaceFont, wordWrap = false, richText = false };
                }

                return _logStyle;
            }
        }

        private void OnEnable()
        {
            if (_pack == null)
            {
                _pack = PackAssets.FindDefault();
            }
        }

        private void OnDisable()
        {
            if (_monospaceFont != null)
            {
                DestroyImmediate(_monospaceFont);
            }

            _monospaceFont = null;
            _logStyle = null;
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _pack = (PackConfigDefinition)EditorGUILayout.ObjectField("Pack", _pack, typeof(PackConfigDefinition), false);
            _seed = EditorGUILayout.IntField("Seed", _seed);
            if (EditorGUI.EndChangeCheck())
            {
                ResetSession("Pack or seed changed");
            }

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_log, LogStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            DrawCommandRow();
        }

        private void DrawCommandRow()
        {
            Event current = Event.current;
            bool submitted = current.type == EventType.KeyDown
                && (current.keyCode == KeyCode.Return || current.keyCode == KeyCode.KeypadEnter)
                && GUI.GetNameOfFocusedControl() == CommandControlName;

            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName(CommandControlName);
            _command = EditorGUILayout.TextField(_command);
            bool runClicked = GUILayout.Button("Run", GUILayout.Width(ButtonWidth));
            if (GUILayout.Button("Reset session", GUILayout.Width(ButtonWidth)))
            {
                ResetSession("Reset requested");
            }

            if (GUILayout.Button("Clear log", GUILayout.Width(ButtonWidth)))
            {
                _log = string.Empty;
            }

            EditorGUILayout.EndHorizontal();

            if (submitted || runClicked)
            {
                RunCommand();
                if (submitted)
                {
                    current.Use();
                }
            }

            if (_refocusCommand && current.type == EventType.Repaint)
            {
                _refocusCommand = false;
                EditorGUI.FocusTextInControl(CommandControlName);
            }
        }

        private void RunCommand()
        {
            string command = (_command ?? string.Empty).Trim();
            _command = string.Empty;
            _refocusCommand = true;
            GUI.FocusControl(null);
            if (command.Length == 0)
            {
                return;
            }

            if (HudDebugCommands.Handles(command))
            {
                AppendLog($"> {command}\n{RunHudCommand(command)}");
                return;
            }

            if (_commands == null && !TryStartSession(out string error))
            {
                AppendLog($"> {command}\n{error}");
                return;
            }

            string output = _commands.Execute(command);
            if (string.Equals(command, "help", StringComparison.OrdinalIgnoreCase))
            {
                output += "\n\n" + HudDebugCommands.HelpText;
            }

            AppendLog($"> {command}\n{output}");
        }

        // HUD commands act on the HUD of the scene in Play Mode, not on this window's own session.
        private static string RunHudCommand(string command)
        {
            if (!EditorApplication.isPlaying)
            {
                return "HUD commands need Play Mode.";
            }

            var hud = UnityEngine.Object.FindFirstObjectByType<HudPresenter>();
            return hud == null ? "No HUD in the open scene." : new HudDebugCommands(hud).Execute(command);
        }

        private bool TryStartSession(out string error)
        {
            if (!PackAssets.TryGetPool(_pack, out CardPool pool, out error))
            {
                return false;
            }

            try
            {
                PackConfig config = _pack.ToPackConfig();
                var opener = new PackOpener(config, pool, new SeededRng(_seed));
                _commands = new PackDebugCommands(opener, new InventoryService());
                return true;
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        private void ResetSession(string reason)
        {
            _commands = null;
            AppendLog($"[{reason}: session reset, inventory cleared. The pack asset is re-read on the next command.]");
        }

        private void AppendLog(string text)
        {
            _log = _log.Length == 0 ? text : _log + "\n\n" + text;
            if (_log.Length > MaxLogLength)
            {
                int cut = _log.IndexOf('\n', _log.Length - MaxLogLength);
                _log = cut < 0 ? _log.Substring(_log.Length - MaxLogLength) : _log.Substring(cut + 1);
            }

            _logScroll.y = float.MaxValue;
            Repaint();
        }
    }
}

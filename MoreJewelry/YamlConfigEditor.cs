using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Configuration;
using ServerSync;
using UnityEngine;
using UnityEngine.UIElements;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using Cursor = UnityEngine.Cursor;
using Logger = Managers.Logger;


namespace MoreJewelry
{

    public class YamlConfigEditor : MonoBehaviour
    {
        public static string EditButtonName => Localization.instance.Localize("$mj_yaml_editor_edit");

        private const int WindowId = -669;
        private static Rect yamlWindowRect;

        private string currentYamlInput = null!;
        private bool collapsed = false;
        private Vector2 yamlTextareaScrollPosition;
        private Vector2 yamlErrorsScrollPosition;
        private bool hasErrors = false;

        private YamlConfig yamlConfig = null!;
        private CustomSyncedValue<string>? activeConfig = null;
        private Func<string, List<string>> errorChecker = null!;
        private ConfigSync configSync = null!;
        private string modName;

        public void Initialize(string modName, ConfigSync configSync, YamlConfig yaml, Func<string, List<string>> errorCheck) {

            this.modName = modName;
            this.configSync = configSync;
            this.yamlConfig = yaml;
            activeConfig = yaml.SyncedValue;
            errorChecker = errorCheck;
            Logger.LogDebugOnly($"Setting up {modName} YAML Editor");
        }

        public void DrawYamlEditorButton(ConfigEntryBase _)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            GUILayout.Space(236f);
            if (!string.IsNullOrWhiteSpace(activeConfig.Value) && GUILayout.Button(EditButtonName, GUILayout.ExpandWidth(true)))
            {
                currentYamlInput = activeConfig.Value;
                yamlTextareaScrollPosition = new();
            }
            GUILayout.Space(59f);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void Update()
        {
            if (currentYamlInput != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void LateUpdate() => Update();

        private void Start() {}

        private void OnGUI()
        {
            if (currentYamlInput == null)
            {
                return;
            }

            Update();

            yamlWindowRect = new Rect(10, 10, Screen.width - 20, Screen.height - 20);
            GUILayout.Window(WindowId, yamlWindowRect, yamlWindowDrawer, $"{modName} YAML Editor", GUI.skin.window);
        }

        private void yamlWindowDrawer(int id)
        {
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), Texture2D.blackTexture);
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUI.enabled = !hasErrors && (!configSync.IsLocked || configSync.IsSourceOfTruth);

            void save() => activeConfig!.Value = currentYamlInput;

            if (GUILayout.Button(Localization.instance.Localize("$mj_yaml_editor_save")) && !hasErrors)
            {
                save();
                currentYamlInput = null;
            }

            if (configSync.IsSourceOfTruth && GUILayout.Button(Localization.instance.Localize("$mj_yaml_editor_apply")) && !hasErrors)
            {
                yamlConfig.SkipSavingOfValueChange = true;
                save();
                yamlConfig.SkipSavingOfValueChange = false;
                currentYamlInput = null;
            }

            GUI.enabled = true;
            if (GUILayout.Button(Localization.instance.Localize("$mj_yaml_editor_discard")))
            {
                currentYamlInput = null;
            }

            GUILayout.EndHorizontal();

            hasErrors = false;
            string yamlErrorContent = "";

            if (GUI.GetNameOfFocusedControl() == $"{modName} yaml textarea" && Event.current.type is EventType.KeyDown or EventType.KeyUp && Event.current.isKey)
            {
                if (Event.current.keyCode == KeyCode.Tab || Event.current.character == '\t')
                {
                    if (Event.current.type == EventType.KeyUp)
                    {
                        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        editor.Insert(' ');
                        editor.Insert(' ');
                        currentYamlInput = editor.text;
                    }

                    Event.current.Use();
                }

                // repeat indent of previous line on enter
                if (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return || Event.current.character == '\n')
                {
                    if (Event.current.type == EventType.KeyUp)
                    {
                        TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        string text = editor.text;
                        int lineStartIndex = editor.cursorIndex;
                        if (lineStartIndex > 0)
                        {
                            do
                            {
                                if (text[lineStartIndex - 1] == '\n')
                                {
                                    break;
                                }
                            } while (--lineStartIndex > 0);
                        }

                        int lastSpaceIndex = lineStartIndex;
                        if (lastSpaceIndex < text.Length && text[lastSpaceIndex] == ' ')
                        {
                            while (true)
                            {
                                if (lastSpaceIndex >= text.Length || text[lastSpaceIndex] == '\n')
                                {
                                    lastSpaceIndex = lineStartIndex;
                                    break;
                                }

                                if (text[lastSpaceIndex] != ' ')
                                {
                                    break;
                                }

                                ++lastSpaceIndex;
                            }
                        }

                        if (lastSpaceIndex > editor.cursorIndex)
                        {
                            lastSpaceIndex = lineStartIndex;
                        }

                        editor.Insert('\n');
                        for (int i = lastSpaceIndex - lineStartIndex; i > 0; --i)
                        {
                            editor.Insert(' ');
                        }

                        currentYamlInput = editor.text;
                    }

                    Event.current.Use();
                }
            }

            if (currentYamlInput != null)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                yamlTextareaScrollPosition = GUILayout.BeginScrollView(yamlTextareaScrollPosition, GUILayout.ExpandHeight(true));
                GUI.SetNextControlName($"{modName} yaml textarea");
                currentYamlInput = GUILayout.TextArea(currentYamlInput, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                GUILayout.EndScrollView();

                GUILayout.EndVertical();
            }

            try
            {
                List<string> errors = errorChecker(currentYamlInput);
                if (errors.Count > 0)
                {
                    hasErrors = true;
                    yamlErrorContent = $"{yamlErrorContent}There are errors in your yaml config:\n{string.Join("\n", errors)}\n";
                }
            }
            catch (YamlException e)
            {
                Logger.LogWarning($"Parsing your yaml config failed with an error:\n{e.Message + (e.InnerException != null ? ": " + e.InnerException.Message : "")}");
                hasErrors = true;
            }            

            GUILayout.BeginVertical(GUI.skin.box);
            yamlErrorsScrollPosition = GUILayout.BeginScrollView(yamlErrorsScrollPosition, GUILayout.Height(100));

            if (yamlErrorContent != "")
            {
                GUIStyle labelStyle = new(GUI.skin.label)
                {
                    normal =
                    {
                        textColor = new Color(200, 50, 50),
                    },
                };
                Color oldColor = GUI.contentColor;
                GUI.contentColor = new Color(200, 50, 50);
                GUILayout.Label(yamlErrorContent, labelStyle);
                GUI.contentColor = oldColor;
            }
            else
            {
                GUILayout.Label("Configuration syntax is valid.");
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

    }
}
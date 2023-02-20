using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UniTyped.Generator.Manual.Editor
{

    [CustomPropertyDrawer(typeof(GenerationItem))]
    public class GenerationItemDrawer : PropertyDrawer
    {
        
        private void DoPathField(Rect position, SerializedProperty property, bool save, string extension)
        {
            position.width -= 25f;
            EditorGUI.PropertyField(position, property);

            position.x += position.width + 5f;
            position.width = 20f;

            if (GUI.Button(position, "..."))
            {
                var path = save
                    ? EditorUtility.SaveFilePanelInProject("", "", extension, "")
                    : EditorUtility.OpenFilePanel("", "", extension);

                #if NET_STANDARD_2_1
                property.stringValue = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
                #else
                property.stringValue = path;
                #endif
                
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var projectPathProp = property.FindPropertyRelative("projectPath");
            var outputFileProp = property.FindPropertyRelative("outputFile");

            position.height = EditorGUIUtility.singleLineHeight;
            position.y += EditorGUIUtility.standardVerticalSpacing;

            DoPathField(position, projectPathProp, false, "csproj");

            position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            DoPathField(position, outputFileProp, true, "cs");
        }
    }

    [CustomEditor(typeof(UniTypedManualGeneratorProfile))]
    public class UniTypedManualGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            EditorGUI.BeginDisabledGroup(isBusy);

            if (GUILayout.Button(isBusy ? "Generating..." : "Generate"))
            {
                GenerateAsync();
            }
            EditorGUI.EndDisabledGroup();
        }
        
        private static bool isBusy = false;

        private async Task GenerateAsync()
        {
            
            if (!(target is UniTypedManualGeneratorProfile profile)) return;
                
            if (isBusy) return;
            isBusy = true;
            EditorApplication.LockReloadAssemblies();

            try
            {
                List<GeneratorRunner> runners = new List<GeneratorRunner>();
                foreach (var item in profile.items)
                {
                    runners.Add(new GeneratorRunner(item));
                }

                for (var i = 0; i < runners.Count; i++)
                {
                    var runner = runners[i];
                    var displayIndex = i + 1;
                    string progress = $"({displayIndex.ToString()}/{runners.Count.ToString()})";
                    using (runner)
                    {
                        Debug.Log($"{progress} Generating for project...");

                        var exitCode = await runner.RunAsync();

                        if (exitCode != 0)
                        {
                            Debug.LogError(
                                $"{progress} Failed to generate: {runner.StandardError}");
                        }
                        else
                        {
                            Debug.Log(
                                $"{progress} Successfully generated: {runner.StandardOutput}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                EditorApplication.UnlockReloadAssemblies();
                isBusy = false;
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

        }
    }

    public class GeneratorRunner : IDisposable
    {
        private static string generatorPath =
            "Packages/com.ruccho.unityped.manualgenerator/Editor/Executable~/netcoreapp3.1/UniTyped.Generator.Standalone.dll";

        private readonly Process process = default;

        public string StandardOutput { get; private set; }
        public string StandardError { get; private set; }
        public GeneratorRunner(GenerationItem item)
        {
            this.process = new Process();
            var fullGeneratorPath = Path.GetFullPath(generatorPath);
            process.StartInfo = new ProcessStartInfo("dotnet",
                $"\"{fullGeneratorPath}\" --project=\"{item.projectPath}\" --output=\"{item.outputFile}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
        }

        public async Task<int> RunAsync()
        {
            await Task.Run(() =>
            {
                process.Start();
                process.WaitForExit();
            });

            StandardOutput = await process.StandardOutput.ReadToEndAsync();
            StandardError = await process.StandardError.ReadToEndAsync();

            return process.ExitCode;
        }

        public void Dispose()
        {
            process?.Dispose();
        }
    }
}
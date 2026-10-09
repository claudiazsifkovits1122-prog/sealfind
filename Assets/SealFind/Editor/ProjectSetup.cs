using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SealFind.EditorTools
{
    /// <summary>
    /// Richtet das Projekt beim ersten Öffnen automatisch ein:
    /// Hauptszene anlegen, in die Build-Settings eintragen, App-Name und Querformat setzen.
    /// Manuell erneut ausführbar über das Menü "Robben-Suche".
    /// </summary>
    [InitializeOnLoad]
    static class ProjectSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string DoneKey = "SealFind.SetupChecked";

        static ProjectSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SessionState.GetBool(DoneKey, false)) return;
                SessionState.SetBool(DoneKey, true);
                if (!File.Exists(ScenePath)) Setup(openScene: true);
                else EnsureBuildSettings();
            };
        }

        [MenuItem("Robben-Suche/Projekt einrichten", priority = 0)]
        static void SetupMenu() => Setup(openScene: true);

        [MenuItem("Robben-Suche/Spielstand löschen", priority = 20)]
        static void DeleteSave()
        {
            if (EditorUtility.DisplayDialog("Spielstand löschen", "Wirklich den lokalen Spielstand löschen?", "Löschen", "Abbrechen"))
            {
                SaveSystem.Delete();
                Debug.Log("[SealFind] Spielstand gelöscht.");
            }
        }

        [MenuItem("Robben-Suche/Spielstand-Ordner öffnen", priority = 21)]
        static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);

        static void Setup(bool openScene)
        {
            if (!File.Exists(ScenePath))
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                CreateScene();
            }
            else if (openScene && EditorSceneManager.GetActiveScene().path != ScenePath &&
                     EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(ScenePath);
            }
            EnsureBuildSettings();
            ApplyPlayerSettings();
            Debug.Log("[SealFind] Projekt eingerichtet – einfach auf Play drücken!");
        }

        static void CreateScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.12f, 0.16f);
            camGo.AddComponent<AudioListener>();

            new GameObject("SealFind").AddComponent<GameManager>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }

        static void EnsureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes)
                if (s.path == ScenePath) return;

            var list = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            list.AddRange(scenes);
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void ApplyPlayerSettings()
        {
            if (PlayerSettings.productName == "sealfind" || string.IsNullOrEmpty(PlayerSettings.productName) || PlayerSettings.productName == "New Unity Project")
                PlayerSettings.productName = "Robben-Suche";
            if (PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = "SealFind";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android).Contains("DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.sealfind.robbensuche");
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS).Contains("DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.sealfind.robbensuche");
        }
    }
}

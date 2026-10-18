using Kaname.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kaname.EditorTools
{
    // Builds the pieces of the test environment that live as assets: the tuning asset and the TestStage scene. Re-running it is safe (it keeps
    // the tuning asset, and so any numbers the designer has changed in it, and rebuilds the scene). From the menu: Kaname > Set up the test stage.
    // From a shell:  unity ... -batchmode -quit -executeMethod Kaname.EditorTools.ProjectSetup.Run
    public static class ProjectSetup
    {
        public const string TuningPath = "Assets/Settings/KanameTuning.asset";
        public const string ScenePath = "Assets/Scenes/TestStage.unity";
        public const string InkMaterialPath = "Assets/Settings/InkVertexColor.mat";

        [MenuItem("Kaname/Set up the test stage")]
        public static void Run()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<KanameTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = ScriptableObject.CreateInstance<KanameTuning>();
                AssetDatabase.CreateAsset(tuning, TuningPath);
            }

            // the ink drawing is a mesh with vertex colours: a URP 2D unlit sprite material takes them
            var ink = AssetDatabase.LoadAssetAtPath<Material>(InkMaterialPath);
            if (ink == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                ink = new Material(shader) { name = "InkVertexColor" };
                AssetDatabase.CreateAsset(ink, InkMaterialPath);
            }
            // smooth edges for the ink shapes: 4x multisampling on the render pipeline
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp && urp.msaaSampleCount < 4)
            {
                urp.msaaSampleCount = 4;
                EditorUtility.SetDirty(urp);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = cameraObject.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.white;
            cam.nearClipPlane = 0.3f; cam.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0, 0, -10);

            var game = new GameObject("Game");
            var driver = game.AddComponent<GameDriver>();
            var so = new SerializedObject(driver);
            so.FindProperty("tuning").objectReferenceValue = tuning;
            so.FindProperty("viewCamera").objectReferenceValue = cam;
            so.FindProperty("inkMaterial").objectReferenceValue = ink;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/SampleScene.unity") != null)
                AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");      // the template's sample scene is not part of the game
            PlayerSettings.productName = "Untitled Ninja Game";
            // gamma colour space: the ink look is flat colours blended the way the JavaScript game's canvas blends them (a linear project would
            // lighten every vertex colour and every glow)
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            AssetDatabase.SaveAssets();
            Debug.Log("Kaname: the test stage is set up (" + ScenePath + ", " + TuningPath + ").");
        }
    }
}

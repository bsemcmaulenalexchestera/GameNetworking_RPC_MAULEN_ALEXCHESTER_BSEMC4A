using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;

namespace CrystalRush.Editor
{
    public static class CrystalRushSetup
    {
        private const string Root = "Assets/CrystalRush/Generated";
        public const string ArenaScenePath = "Assets/Scenes/CrystalArena.unity";
        [InitializeOnLoadMethod]
        private static void ScheduleFirstSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode ||
                    File.Exists(ArenaScenePath)) return;
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty) return;
                CreateArena();
            };
        }
        [MenuItem("Crystal Rush/Create Arena and Assets")]
        public static void CreateArena()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(Root + "/CrystalArena.unity") && !File.Exists(ArenaScenePath))
            {
                MoveArenaToScenes();
                return;
            }
            // Never replace an existing generated scene or discard an unsaved scene.
            if (File.Exists(ArenaScenePath))
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ArenaScenePath);
                Debug.Log("Opened arena: " + ArenaScenePath);
                return;
            }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var teal = Material("CrystalTeal", new Color(.05f, .95f, .8f), true);
            var dark = Material("ArenaSlate", new Color(.035f, .065f, .12f));
            var edge = Material("EdgeBlue", new Color(.08f, .35f, .6f), true);
            var playerMat = Material("Player", Color.white);

            // Original faceted octahedral crystal mesh; reusable without networking.
            var mesh = new Mesh { name = "FacetedCrystal" };
            Vector3 top = new Vector3(0, 1, 0), bottom = new Vector3(0, -.75f, 0);
            Vector3[] ring = { new Vector3(.5f, 0, 0), new Vector3(0, 0, .5f), new Vector3(-.5f, 0, 0), new Vector3(0, 0, -.5f) };
            var vertices = new Vector3[24];
            var triangles = new int[24];
            for (int i = 0; i < 4; i++)
            {
                int n = i * 6;
                vertices[n] = top; vertices[n + 1] = ring[(i + 1) % 4]; vertices[n + 2] = ring[i];
                vertices[n + 3] = bottom; vertices[n + 4] = ring[i]; vertices[n + 5] = ring[(i + 1) % 4];
                for (int j = 0; j < 6; j++) triangles[n + j] = n + j;
            }
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Root + "/FacetedCrystal.asset");
            var art = new GameObject("CrystalVisual");
            art.AddComponent<MeshFilter>().sharedMesh = mesh;
            art.AddComponent<MeshRenderer>().sharedMaterial = teal;
            PrefabUtility.SaveAsPrefabAsset(art, Root + "/CrystalVisual.prefab");
            var crystal = new GameObject("NetworkCrystal");
            art.transform.SetParent(crystal.transform, false);
            crystal.AddComponent<NetworkObject>();
            crystal.AddComponent<NetworkCrystal>();
            var crystalPrefab = PrefabUtility.SaveAsPrefabAsset(crystal, Root + "/NetworkCrystal.prefab");
            Object.DestroyImmediate(crystal);

            var player = new GameObject("NetworkPlayer");
            player.AddComponent<NetworkObject>();
            player.AddComponent<NetworkTransform>();
            player.AddComponent<CrystalPlayer>();
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(player.transform, false);
            body.GetComponent<Renderer>().sharedMaterial = playerMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());
            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(player, Root + "/NetworkPlayer.prefab");
            Object.DestroyImmediate(player);

            var network = new GameObject("NetworkManager");
            var transport = network.AddComponent<UnityTransport>();
            var manager = network.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig { NetworkTransport = transport, PlayerPrefab = playerPrefab };
            var prefabs = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            prefabs.Add(new NetworkPrefab { Prefab = playerPrefab });
            prefabs.Add(new NetworkPrefab { Prefab = crystalPrefab });
            AssetDatabase.CreateAsset(prefabs, Root + "/NetworkPrefabs.asset");
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabs);
            var game = new GameObject("CrystalGame").AddComponent<CrystalGame>();
            game.crystalPrefab = crystalPrefab.GetComponent<NetworkObject>();

            Cube("Arena", new Vector3(0, -.3f, 0), new Vector3(22, .6f, 18), dark);
            for (int x = -10; x <= 10; x += 2)
                Cube("Grid", new Vector3(x, .012f, 0), new Vector3(.025f, .015f, 18), edge);
            for (int z = -8; z <= 8; z += 2)
                Cube("Grid", new Vector3(0, .012f, z), new Vector3(22, .015f, .025f), edge);
            Cube("North border", new Vector3(0, .25f, 9), new Vector3(22, .5f, .2f), edge);
            Cube("South border", new Vector3(0, .25f, -9), new Vector3(22, .5f, .2f), edge);
            Cube("East border", new Vector3(11, .25f, 0), new Vector3(.2f, .5f, 18), edge);
            Cube("West border", new Vector3(-11, .25f, 0), new Vector3(.2f, .5f, 18), edge);
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(-3, 24, -19);
            camera.transform.LookAt(new Vector3(-3, 0, 0));
            camera.orthographic = true; camera.orthographicSize = 14;
            camera.backgroundColor = new Color(.018f, .025f, .055f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.gameObject.AddComponent<AudioListener>();
            var light = new GameObject("Sun").AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.5f;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(.45f, .5f, .65f);
            EditorSceneManager.SaveScene(scene, ArenaScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ArenaScenePath, true) };
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("CRYSTAL_RUSH_SETUP_OK: arena, 2 network prefabs, visual prefab and mesh created.");
        }

        [MenuItem("Crystal Rush/Move Arena to Scenes")]
        public static void MoveArenaToScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string oldPath = Root + "/CrystalArena.unity";
            if (!File.Exists(oldPath) || File.Exists(ArenaScenePath)) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            string error = AssetDatabase.MoveAsset(oldPath, ArenaScenePath);
            if (!string.IsNullOrEmpty(error)) throw new System.IO.IOException(error);
            var scenes = EditorBuildSettings.scenes;
            foreach (var entry in scenes)
                if (entry.path == oldPath) entry.path = ArenaScenePath;
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ArenaScenePath);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ArenaScenePath);
            EditorGUIUtility.PingObject(Selection.activeObject);
            Debug.Log("CRYSTAL_RUSH_SCENE_MOVED: " + ArenaScenePath);
        }

        private static Material Material(string name, Color color, bool glow = false)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            if (glow) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", color * .5f); }
            AssetDatabase.CreateAsset(mat, Root + "/" + name + ".mat");
            return mat;
        }
        private static void Cube(string name, Vector3 position, Vector3 scale, Material mat)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.position = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}

using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CrystalRush
{
    // Opt-in development-build integration test. Moves through the normal movement
    // RPC and pickup path; never changes scores, object positions or collectible state.
    public class EvidenceRunner : MonoBehaviour
    {
        private string role, output;
        private DateTime start;
        private int stage;
        private NetworkManager manager;
        private static readonly double[] CaptureTimes = { 0, 2.3, 5, 8.3, 11, 15 };
        private static readonly string[] Names = { "01-connected", "02-host-moving", "03-host-collected", "04-client-moving", "05-client-collected", "06-no-repeat" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Debug.isDebugBuild && Environment.GetCommandLineArgs().Any(a => a.StartsWith("--cr-evidence=")))
                new GameObject("Automated integration test").AddComponent<EvidenceRunner>();
        }
        private string Arg(string key) => Environment.GetCommandLineArgs().First(a => a.StartsWith(key + "=")).Substring(key.Length + 1);
        private IEnumerator Start()
        {
            role = Arg("--cr-evidence"); output = Arg("--cr-output");
            start = new DateTime(long.Parse(Arg("--cr-start")), DateTimeKind.Utc);
            Directory.CreateDirectory(output);
            Application.runInBackground = true;
            yield return null; // Let CrystalGame subscribe before starting the server.
            manager = NetworkManager.Singleton;
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777, "127.0.0.1");
            bool started = role == "host" ? manager.StartHost() : manager.StartClient();
            Debug.Log("EVIDENCE_STARTED " + role + " " + started);
        }
        private void Update()
        {
            if (manager == null) return;
            double t = (DateTime.UtcNow - start).TotalSeconds;
            var local = FindObjectsByType<CrystalPlayer>(FindObjectsSortMode.None).FirstOrDefault(p => p.IsSpawned && p.IsOwner);
            if (local != null)
            {
                double moveStart = role == "host" ? 2 : 8;
                local.EvidenceInput = t >= moveStart && t < moveStart + .7 ? Vector2.up : Vector2.zero;
            }
            if (stage < CaptureTimes.Length && t >= CaptureTimes[stage])
            {
                StartCoroutine(Capture(Names[stage], stage));
                stage++;
            }
            if (t > 19) Application.Quit();
        }
        private IEnumerator Capture(string name, int index)
        {
            yield return new WaitForEndOfFrame();
            var players = FindObjectsByType<CrystalPlayer>(FindObjectsSortMode.None).Where(p => p.IsSpawned).OrderBy(p => p.OwnerClientId).ToArray();
            var crystals = FindObjectsByType<NetworkCrystal>(FindObjectsSortMode.None).Count(c => c.IsSpawned);
            var snapshot = new Snapshot
            {
                role = role, stage = name, utc = DateTime.UtcNow.ToString("O"),
                connected = manager.IsConnectedClient, host = manager.IsHost, crystals = crystals,
                players = players.Select(p => new PlayerState { id = p.OwnerClientId, owner = p.IsOwner, score = p.Score.Value, position = p.transform.position }).ToArray()
            };
            File.WriteAllText(Path.Combine(output, role + "-" + name + ".json"), JsonUtility.ToJson(snapshot, true));
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenshot == null) throw new InvalidOperationException("Screenshot capture returned no texture.");
            File.WriteAllBytes(Path.Combine(output, role + "-" + name + ".png"), screenshot.EncodeToPNG());
            Destroy(screenshot);
            int expectedHost = index >= 2 ? 25 : 0;
            int expectedClient = index >= 4 ? 25 : 0;
            // Moving screenshots have variable frame timing; assert settled stages only.
            if (index == 0 || index == 2 || index >= 4)
            {
                bool passed = snapshot.connected && players.Length == 2 && players[0].Score.Value == expectedHost &&
                    players[1].Score.Value == expectedClient && crystals == 15 - (expectedHost + expectedClient) / 25;
                Debug.Log((passed ? "EVIDENCE_PASS " : "EVIDENCE_FAIL ") + role + " " + name);
            }
        }
        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.box) { fontSize = 16 };
            GUI.Box(new Rect(355, Screen.height - 45, 580, 32), "AUTOMATED TEST / " + role + " / real local host-client session", style);
        }
        [Serializable] private class Snapshot { public string role, stage, utc; public bool connected, host; public int crystals; public PlayerState[] players; }
        [Serializable] private class PlayerState { public ulong id; public bool owner; public int score; public Vector3 position; }
    }
}

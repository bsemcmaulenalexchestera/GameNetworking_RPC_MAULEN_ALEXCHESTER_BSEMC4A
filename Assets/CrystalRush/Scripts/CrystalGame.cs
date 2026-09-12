using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CrystalRush
{
    public class CrystalGame : MonoBehaviour
    {
        public NetworkObject crystalPrefab;
        private string address = "127.0.0.1";
        private static string announcement = "Walk into a crystal to collect it.";
        private static float announcementUntil;
        private string status = "Ready to connect";
        private NetworkManager manager;
        private GUIStyle title, heading, text;

        private void Start()
        {
            Application.runInBackground = true;
            manager = NetworkManager.Singleton;
            manager.OnServerStarted += SpawnCrystals;
            manager.OnClientConnectedCallback += Connected;
            manager.OnClientDisconnectCallback += Disconnected;
        }
        private void OnDestroy()
        {
            if (manager == null) return;
            manager.OnServerStarted -= SpawnCrystals;
            manager.OnClientConnectedCallback -= Connected;
            manager.OnClientDisconnectCallback -= Disconnected;
        }
        private void Connected(ulong id) { status = $"Player {id + 1} connected"; }
        private void Disconnected(ulong id) { status = $"Player {id + 1} disconnected. {manager.DisconnectReason}"; }
        public static void Announce(string message)
        {
            announcement = message;
            announcementUntil = Time.unscaledTime + 6;
            Debug.Log(message);
        }
        private void SpawnCrystals()
        {
            for (int x = -8; x <= 8; x += 4)
                for (int z = -3; z <= 5; z += 4)
                    Instantiate(crystalPrefab, new Vector3(x, 1, z), Quaternion.identity).Spawn();
        }
        private void OnGUI()
        {
            if (manager == null) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
                heading = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
                text = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            }
            GUI.matrix = Matrix4x4.Scale(Vector3.one * Mathf.Max(.65f, Screen.height / 800f));
            GUILayout.BeginArea(new Rect(20, 20, 315, 440), GUI.skin.box);
            GUILayout.Label("CRYSTAL RUSH", title);
            GUILayout.Label("MULTIPLAYER COLLECTION LAB", text);
            GUILayout.Space(15);
            if (!manager.IsListening)
            {
                GUILayout.Label("Host address", heading);
                address = GUILayout.TextField(address, 64);
                GUILayout.Space(8);
                if (GUILayout.Button("HOST GAME", GUILayout.Height(40)))
                {
                    manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777, "0.0.0.0");
                    status = manager.StartHost() ? "Host started" : "Could not start host";
                }
                if (GUILayout.Button("JOIN GAME", GUILayout.Height(40)))
                {
                    manager.GetComponent<UnityTransport>().SetConnectionData(address.Trim(), 7777);
                    status = manager.StartClient() ? "Connecting..." : "Could not start client";
                }
                GUILayout.Label("Same PC: 127.0.0.1\nLAN: enter the host's IPv4 address.", text);
            }
            else
            {
                GUILayout.Label(manager.IsHost ? "HOST / ONLINE" : manager.IsConnectedClient ? "CLIENT / ONLINE" : "CONNECTING...", heading);
                GUILayout.Space(10);
                GUILayout.Label("SCOREBOARD", heading);
                foreach (var player in FindObjectsByType<CrystalPlayer>(FindObjectsSortMode.None).OrderBy(p => p.OwnerClientId))
                    if (player.IsSpawned) GUILayout.Label($"Player {player.OwnerClientId + 1}{(player.IsOwner ? " (you)" : "")}     {player.Score.Value} pts", text);
                GUILayout.Space(10);
                GUILayout.Label("WASD / Arrow keys to move\nWalk into crystals: +25 points", text);
                if (FindObjectsByType<NetworkCrystal>(FindObjectsSortMode.None).Length == 0 && manager.IsConnectedClient)
                    GUILayout.Label("Arena cleared! Disconnect and host again for a new round.", text);
                if (GUILayout.Button("DISCONNECT", GUILayout.Height(32))) manager.Shutdown();
            }
            GUILayout.Space(10);
            GUILayout.Label(status, text);
            GUILayout.EndArea();
            if (manager.IsConnectedClient)
            {
                GUILayout.BeginArea(new Rect(355, 20, 580, 85), GUI.skin.box);
                GUILayout.Label(Time.unscaledTime < announcementUntil ? announcement : "Collect crystals. Watch everyone's scores update.", heading);
                GUILayout.EndArea();
            }
        }
    }
}

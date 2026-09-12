using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CrystalRush
{
    public class CrystalPlayer : NetworkBehaviour
    {
        public readonly NetworkVariable<int> Score = new NetworkVariable<int>(0,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private Vector2 move;
        private float lastInput, nextSend, nextCollect;
        internal Vector2? EvidenceInput;

        public override void OnNetworkSpawn()
        {
            if (IsServer) transform.position = new Vector3(-7 + (int)(OwnerClientId % 4) * 4, 1, -6);
            var body = GetComponentInChildren<Renderer>();
            if (body != null) body.material.color = Color.HSVToRGB((OwnerClientId * .27f + .48f) % 1, .65f, 1);
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner) return;
            var k = Keyboard.current;
            Vector2 input = k == null ? Vector2.zero : new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) -
                (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0),
                (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) -
                (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0));
            if (Debug.isDebugBuild && EvidenceInput.HasValue) input = EvidenceInput.Value;
            if (Time.unscaledTime >= nextSend)
            {
                MoveRpc(input);
                nextSend = Time.unscaledTime + .05f;
            }
            if (Time.unscaledTime < nextCollect) return;
            foreach (var crystal in FindObjectsByType<NetworkCrystal>(FindObjectsSortMode.None))
            {
                if (crystal.IsSpawned && Vector3.Distance(transform.position, crystal.transform.position) < 1.65f)
                {
                    CollectRpc(crystal.NetworkObjectId);
                    nextCollect = Time.unscaledTime + .2f;
                    break;
                }
            }
        }

        [Rpc(SendTo.Server)]
        private void MoveRpc(Vector2 input, RpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != OwnerClientId || !float.IsFinite(input.x) || !float.IsFinite(input.y)) return;
            move = Vector2.ClampMagnitude(input, 1);
            lastInput = Time.unscaledTime;
        }

        private void FixedUpdate()
        {
            if (!IsSpawned || !IsServer) return;
            if (Time.unscaledTime - lastInput > .3f) move = Vector2.zero;
            Vector3 p = transform.position + new Vector3(move.x, 0, move.y) * (5 * Time.fixedDeltaTime);
            transform.position = new Vector3(Mathf.Clamp(p.x, -10, 10), 1, Mathf.Clamp(p.z, -8, 8));
        }

        [Rpc(SendTo.Server)]
        private void CollectRpc(ulong objectId, RpcParams rpc = default)
        {
            // Never trust client-supplied points, positions, or player identities.
            if (rpc.Receive.SenderClientId != OwnerClientId) return;
            if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var obj)) return;
            var crystal = obj.GetComponent<NetworkCrystal>();
            if (crystal == null || Vector3.Distance(transform.position, obj.transform.position) > 1.9f) return;
            if (!crystal.TryClaim()) return;
            Score.Value += NetworkCrystal.Points;
            AnnounceRpc(OwnerClientId, NetworkCrystal.Points);
            obj.Despawn(true);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void AnnounceRpc(ulong playerId, int points)
        {
            CrystalGame.Announce($"Player {playerId + 1} collected a Crystal! +{points} Points");
        }
    }
}

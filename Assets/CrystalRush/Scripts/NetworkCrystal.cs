using Unity.Netcode;
using UnityEngine;

namespace CrystalRush
{
    public class NetworkCrystal : NetworkBehaviour
    {
        public const int Points = 25;
        private bool claimed;
        public bool TryClaim()
        {
            if (!IsServer || !IsSpawned || claimed) return false;
            claimed = true;
            return true;
        }
        private void Update()
        {
            if (transform.childCount > 0)
                transform.GetChild(0).Rotate(0, 55 * Time.deltaTime, 0, Space.Self);
        }
    }
}

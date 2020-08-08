using UnityEngine;

namespace ArithFeather.CustomItemSpawner.EndlessSpawning {
	public class PickupDisableTrigger : MonoBehaviour
	{
		private ItemSpawnPoint _itemSpawnPoint;

		public void Initialize(ItemSpawnPoint itemSpawnPoint) => _itemSpawnPoint = itemSpawnPoint;

		public void PickedUp() => _itemSpawnPoint.SetFree();
	}
}

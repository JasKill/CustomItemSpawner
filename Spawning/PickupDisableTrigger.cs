using System.Collections.Generic;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.Spawning {
	public class PickupDisableTrigger : MonoBehaviour {
		private ItemSpawnPoint _itemSpawnPoint;

		public List<PickupDisableTrigger> Copies = new List<PickupDisableTrigger>();

		public void Initialize(ItemSpawnPoint itemSpawnPoint) => _itemSpawnPoint = itemSpawnPoint;

		public void PickedUp() {
			_itemSpawnPoint?.SetFree();

			var copyCount = Copies.Count;
			for (int i = 0; i < copyCount; i++) {
				Destroy(Copies[i]);
			}
		}

		public void KillTrigger() => _itemSpawnPoint = null;
	}
}

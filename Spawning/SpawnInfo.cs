namespace ArithFeather.CustomItemSpawner.Spawning {
	public readonly struct SpawnInfo {

		public readonly ItemSpawnPoint ItemSpawnPoint;
		public readonly ItemData ItemData;

		public SpawnInfo(ItemSpawnPoint point, ItemData itemData) {
			ItemSpawnPoint = point;
			ItemData = itemData;
		}
	}
}

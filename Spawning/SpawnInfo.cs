namespace ArithFeather.CustomItemSpawner.Spawning {
	public readonly struct SpawnInfo {

		public readonly ItemSpawnPoint ItemSpawnPoint;
		public readonly ItemType ItemType;

		public SpawnInfo(ItemSpawnPoint point, ItemType itemType) {
			ItemSpawnPoint = point;
			ItemType = itemType;
		}
	}
}

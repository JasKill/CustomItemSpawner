using UnityEngine;

namespace ArithFeather.CustomItemSpawner.ItemListTypes {
	public class SpawnChanceWrapper : IItemObtainable {
		public readonly IItemObtainable Items;
		public readonly int ChanceToSpawn;

		public SpawnChanceWrapper(int chanceToSpawn, IItemObtainable items) {
			ChanceToSpawn = chanceToSpawn;
			Items = items;
		}

		public ItemType GetItem() => Random.Range(0, 100) <= ChanceToSpawn ? Items.GetItem() : ItemType.None;

		public bool HasItems => Items.HasItems;
	}
}

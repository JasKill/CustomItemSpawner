using UnityEngine;

namespace ArithFeather.CustomItemSpawner.ItemListTypes
{
	public class SpawnChanceWrapper : IItemObtainable
	{
		private readonly int _copies;

		public readonly IItemObtainable Items;
		public readonly int ChanceToSpawn;

		public SpawnChanceWrapper(IItemObtainable items, int chanceToSpawn, int copies)
		{
			Items = items;
			ChanceToSpawn = chanceToSpawn;
			_copies = copies;
		}

		public ItemData GetItem()
		{
			if (Random.Range(0, 100) <= ChanceToSpawn)
			{
				var item = Items.GetItem();
				return new ItemData(item.Item, _copies * item.Copies);
			}

			return new ItemData(ItemType.None, 0);
		}
	}
}

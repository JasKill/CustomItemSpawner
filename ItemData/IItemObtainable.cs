using System.Collections.Generic;

namespace ArithFeather.CustomItemSpawner.ItemData {
	public interface IItemObtainable
	{
		ItemType GetItem();

		bool HasItems { get; }
	}
}
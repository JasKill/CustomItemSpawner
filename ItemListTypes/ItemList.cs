using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace ArithFeather.CustomItemSpawner.ItemListTypes {

	public class ItemList : IItemObtainable {
		private List<IItemObtainable> _items;
		private int _itemSize;

		public List<IItemObtainable> Items {
			set {
				_items = value;
				_itemSize = value.Count;
			}
		}

		public ItemData GetItem() => _items[Random.Range(0, _itemSize)].GetItem();

		public bool HasItems => (_itemSize != 0);
	}
}
using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace ArithFeather.CustomItemSpawner.ItemData {

	public class ItemList : IItemObtainable {
		private List<SavedItemType> _items;
		private int _itemSize;

		public List<SavedItemType> Items {
			set {
				_items = value;
				_itemSize = value.Count;
			}
		}

		public ItemType GetItem() => _items[Random.Range(0, _itemSize)].GetItem();

		public bool HasItems => (_itemSize != 0);
	}
}

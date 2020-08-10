using System;
using Random = UnityEngine.Random;

namespace ArithFeather.CustomItemSpawner.ItemListTypes {
	public class SavedItemType : IItemObtainable {
		public SavedItemType(ItemType itemType = ItemType.None) {
			_itemType = itemType;
		}

		/// <summary>
		/// WIld card constructor
		/// </summary>
		public SavedItemType() {
			_wildCard = true;
		}

		private readonly bool _wildCard;
		private readonly ItemType _itemType;

		public static readonly int ItemTypeLength = Enum.GetNames(typeof(ItemType)).Length;

		public ItemData GetItem()
		{
			if (!_wildCard) {
				return new ItemData(_itemType, 1);
			}

			var i = Random.Range(0, ItemTypeLength);
			var itemType = i == 36 ? ItemType.None : (ItemType) i;
			return new ItemData(itemType, 1);
		}

		public bool HasItems => true;
	}
}

using System;
using Random = UnityEngine.Random;

namespace ArithFeather.CustomItemSpawner.ItemData {
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

		public ItemType GetItem()
		{
			if (!_wildCard)
			{
				return _itemType;
			}

			var i = Random.Range(0, ItemTypeLength);
			return (i == 36) ? ItemType.None : (ItemType) i;
		}

		public bool HasItems => true;
	}
}

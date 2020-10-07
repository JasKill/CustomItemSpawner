using System.Collections.Generic;
using ArithFeather.Points;
using ArithFeather.Points.Tools;

namespace ArithFeather.CustomItemSpawner.ItemListTypes
{
	public class QueuedList : IItemObtainable
	{
		private List<IItemObtainable> _items;
		private int _itemSize;
		private int _index;

		public List<IItemObtainable> Items
		{
			set
			{
				_items = value;
				_itemSize = value.Count;
			}
		}

		public void Reset()
		{
			_items.UnityShuffle();
			_index = 0;
		}

		public ItemData GetItem()
		{
			if (_index == _itemSize) _index = 0;

			var itemType = _items[_index];
			_index++;
			return itemType.GetItem();
		}

		public bool HasItems => (_index == _itemSize);
	}
}

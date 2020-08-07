using System.Collections.Generic;

namespace ArithFeather.CustomItemSpawner.ItemData {
	public class RoomItemData {
		public List<IItemObtainable> QueuedLists = new List<IItemObtainable>();
		public List<IItemObtainable> ItemLists = new List<IItemObtainable>();
		public List<IItemObtainable> Items = new List<IItemObtainable>();
		public int MaxItemsAllowed { get; set; }
	}
}

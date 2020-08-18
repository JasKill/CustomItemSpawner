namespace ArithFeather.CustomItemSpawner.ItemListTypes {
	public class ContainerItem : IItemObtainable {
		public readonly int Chance;
		public readonly string ContainerId;

		private readonly int _copies;
		private readonly IItemObtainable _item;

		public ContainerItem(string containerId, IItemObtainable item, int chance, int copies)
		{
			_copies = copies;
			ContainerId = containerId;
			Chance = chance;
			_item = item;
		}


		public ItemData GetItem()
		{
			var item = _item.GetItem();
			return new ItemData(item.Item, item.Copies * _copies);
		}
	}
}

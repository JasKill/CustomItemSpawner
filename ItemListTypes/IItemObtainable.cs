namespace ArithFeather.CustomItemSpawner.ItemListTypes {
	public interface IItemObtainable
	{
		ItemType GetItem();

		bool HasItems { get; }
	}
}
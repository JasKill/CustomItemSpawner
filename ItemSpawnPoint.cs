using ArithFeather.ArithSpawningKit.SpawnPointTools;
using Smod2.API;

namespace ArithFeather.RandomItemSpawner
{
	public class ItemSpawnPoint : SpawnPoint
    {
        public ItemSpawnPoint(string roomType, ZoneType zoneType, Vector position, Vector rotation) : base(roomType, zoneType, position, rotation) { }

        private Pickup itemPickup;
        public Pickup ItemPickup
        {
            set
            {
                itemPickup = value;
                IsFree = false;
            }
            get => itemPickup;
        }
        public bool IsFree = false;
    }
}
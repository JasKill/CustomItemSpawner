using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ArithFeather.ArithsToolKit.SpawnPointTools;
using Smod2.API;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ArithFeather.RandomItemSpawner
{
	public class ItemSpawning
	{
		public int[] BaseItemSpawnQueue;
		private int baseItemPointer;

		public int[] SafeItemsSpawnQueue;

		public int NumberItemsOnDeath;
		public int NumberItemsOnStart;

		public List<ItemRoom> Rooms { get; } = new List<ItemRoom>();
		public readonly List<ItemRoom> FreeRooms = new List<ItemRoom>();

		private Inventory cachedInventory;

		public void Reset()
		{
			baseItemPointer = 0;
			Rooms.Clear();
			FreeRooms.Clear();
			cachedInventory = GameObject.Find("Host").GetComponent<Inventory>();
		}

		public void AddRoomData(string room, int maxSpawns)
		{
			var rooms = CustomRoomManager.Instance.Rooms;
			var roomCount = rooms.Count;

			for (int i = 0; i < roomCount; i++)
			{
				var r = rooms[i];

				if (r.Name == room)
				{
					var ir = new ItemRoom(r, maxSpawns);
					FreeRooms.Add(ir);
					Rooms.Add(ir);
				}
			}
		}

		private ItemType GetNextItem()
		{
			var rarity = (ItemRarity)BaseItemSpawnQueue[baseItemPointer];

			baseItemPointer++;
			if (baseItemPointer == BaseItemSpawnQueue.Length)
			{
				BaseItemSpawnQueue.Shuffle();
				baseItemPointer = 0;
			}

			return GetRandomRarityItem(rarity);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ItemType GetRandomRarityItem(ItemRarity rarity)
		{
			switch (rarity)
			{
				case ItemRarity.KeyCheckpoint:
					return ItemType.MAJOR_SCIENTIST_KEYCARD;
				case ItemRarity.KeyWeapons12Escape:
					var rng = Random.Range(0f, 1f);
					if (rng < 0.334)
					{
						return ItemType.SENIOR_GUARD_KEYCARD;
					}
					else if (rng > 0.667)
					{
						return ItemType.GUARD_KEYCARD;
					}
					else
					{
						return ItemType.CONTAINMENT_ENGINEER_KEYCARD;
					}
				case ItemRarity.KeyManager:
					return Random.Range(0f, 1f) > 0.5f ? ItemType.FACILITY_MANAGER_KEYCARD : ItemType.MTF_LIEUTENANT_KEYCARD;
				case ItemRarity.KeyAdmin:
					return Random.Range(0f, 1f) > 0.5f ? ItemType.MTF_COMMANDER_KEYCARD : ItemType.O5_LEVEL_KEYCARD;
				case ItemRarity.RadioMedkit:
					return Random.Range(0f, 1f) > 0.5f ? ItemType.MEDKIT : ItemType.RADIO;
				case ItemRarity.Pistol:
					return ItemType.COM15;
				case ItemRarity.SMG:
					return Random.Range(0f, 1f) > 0.5f ? ItemType.MP4 : ItemType.P90;
				case ItemRarity.Rifles:
					return Random.Range(0f, 1f) > 0.5f ? ItemType.LOGICER : ItemType.E11_STANDARD_RIFLE;
				case ItemRarity.HID:
					return ItemType.MICROHID;
				case ItemRarity.Grenade:
					return ItemType.FRAG_GRENADE;
			}
			return default;
		}

		/// <summary>
		/// Spawns start-game items.
		/// </summary>
		public void LevelLoaded()
		{
			FreeRooms.AddRange(Rooms);
			var roomCount = FreeRooms.Count;

			for (int i = roomCount - 1; i >= 0; i--)
			{
				var room = FreeRooms[i];
				if (room.ItemSpawnPoints.Count == 0)
				{
					room.IsFree = false;
					FreeRooms.RemoveAt(i);
				}
				else
				{
					room.ItemSpawnPoints.Shuffle();
				}
			}

			// Shuffle rooms
			FreeRooms.Shuffle();

			SpawnItems(NumberItemsOnStart);
		}

		/// <summary>
		/// spawns number of times on death.
		/// </summary>
		public void PlayerDead() => SpawnItems(NumberItemsOnDeath);

		/// <summary>
		/// Goes through all the non-free item spawn points to see if they are free, adds them to free list and sets room as free.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CheckSpawns()
		{
			var roomCount = Rooms.Count;

			for (int i = 0; i < roomCount; i++)
			{
				var room = Rooms[i];
				var spawns = room.ItemSpawnPoints;
				var spawnsCount = spawns.Count;

				for (int j = 0; j < spawnsCount; j++)
				{
					var spawn = spawns[j];

					if (!spawn.IsFree && spawn.ItemPickup == null)
					{
						spawn.IsFree = true;
						room.CurrentItemsSpawned--;

						if (!room.IsFree)
						{
							FreeRooms.Add(room);
							room.IsFree = true;
						}
					}
				}

				// Shuffle spawns
				room.ItemSpawnPoints.Shuffle();
			}

			// Shuffle rooms
			FreeRooms.Shuffle();
		}

		/// <summary>
		/// Will spawn x number of items in random rooms.
		/// Items spawn in one room at a time as long as the room has an open spawn.
		/// </summary>
		private void SpawnItems(int numberOfItems)
		{
			var roomCount = FreeRooms.Count;
			int i = 0;
			bool spawnedItem = false;

			while (roomCount > 0 && numberOfItems > 0)
			{
				var room = FreeRooms[i];

				var spawnPoints = room.ItemSpawnPoints;
				var pointsLength = spawnPoints.Count;

				for (int n = 0; n < pointsLength; n++)
				{
					var spawnPoint = spawnPoints[n];

					if (spawnPoint.IsFree)
					{
						var itemType = GetNextItem();
						SpawnItem(itemType, spawnPoint);
						spawnedItem = true;

						room.CurrentItemsSpawned++;
						if (room.AtMaxItemSpawns)
						{
							room.IsFree = false;
							FreeRooms.RemoveAt(i);
							roomCount--;
							i--;
						}

						numberOfItems--;

						if (numberOfItems == 0)
						{
							return;
						}

						break;
					}
				}

				i++;
				if (i == roomCount)
				{
					if (spawnedItem)
					{
						i = 0;
						spawnedItem = false;
					}
					else
					{
						return;
					}
				}
			}

		}

		/// <summary>
		/// Will spawn all the items in the SafeSpawnQueue in the zone in accessible rooms.
		/// </summary>
		public void SpawnSafeItems(ZoneType zone, int numberOfSpawns = 1)
		{
			var roomCount = FreeRooms.Count;
			int i = 0;
			bool spawnedItem = false;

			for (int j = 0; j < numberOfSpawns; j++)
			{
				var numberOfItems = SafeItemsSpawnQueue.Length;

				while (roomCount > 0 && numberOfItems > 0)
				{
					var room = FreeRooms[i];

					if (room.Room.Zone == zone && room.Room.IsSafe)
					{
						var spawnPoints = room.ItemSpawnPoints;
						var pointsLength = spawnPoints.Count;

						for (int n = 0; n < pointsLength; n++)
						{
							var spawnPoint = spawnPoints[n];

							if (spawnPoint.IsFree)
							{
								var itemType = GetRandomRarityItem((ItemRarity)(SafeItemsSpawnQueue[numberOfItems - 1]));
								SpawnItem(itemType, spawnPoint);
								spawnedItem = true;

								room.CurrentItemsSpawned++;
								if (room.AtMaxItemSpawns)
								{
									room.IsFree = false;
									FreeRooms.RemoveAt(i);
									roomCount--;
									i--;
								}

								numberOfItems--;
								break;
							}
						}
					}

					// Stop checking rooms too.
					i++;

					if (i == roomCount)
					{
						if (spawnedItem)
						{
							i = 0;
							spawnedItem = false;
						}
						else
						{
							return;
						}
					}

					if (numberOfItems == 0)
					{
						break;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SpawnItem(ItemType itemType, ItemSpawnPoint sp)
		{
			var itemGo = cachedInventory.SetPickup((int)itemType, -4.65664672E+11f, Tools.VecToVec3(sp.Position), Quaternion.Euler(Tools.VecToVec3(sp.Rotation)), 0, 0, 0) ?? throw new ArgumentNullException(nameof(sp));
			var item = itemGo.GetComponent<Pickup>();
			sp.ItemPickup = item;
		}
	}
}

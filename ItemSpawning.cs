using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ArithFeather.ArithSpawningKit.SpawnPointTools;
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

		private int cachedRoomIndex;
		private bool cachedDidWeSpawnItem;

		private Inventory cachedInventory;

		public void Reset()
		{
			baseItemPointer = 0;
			ResetRoomIndexer();
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

		public static ItemType GetRandomRarityItem(ItemRarity rarity)
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
		/// Spawns number of items on death.
		/// </summary>
		public void PlayerDead() => SpawnItems(NumberItemsOnDeath);
		/// <summary>
		/// Spawns number of items on start.
		/// </summary>
		public void RoundStart() => SpawnItems(NumberItemsOnStart);

		/// <summary>
		/// Resets the index of the rooms to start from the beginning.
		/// </summary>
		public void ResetRoomIndexer()
		{
			cachedRoomIndex = 0;
			cachedDidWeSpawnItem = false;
		}

		/// <summary>
		/// Goes through all the non-free item spawn points to see if they are free, adds them to free list and sets room as free.
		/// </summary>
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
				if (room.IsFree) room.ItemSpawnPoints.Shuffle();
			}

			// Shuffle rooms
			FreeRooms.Shuffle();
			ResetRoomIndexer();
		}

		/// <param name="numberOfItems">Number of Items to spawn from base spawn queue.</param>
		/// <param name="zone">Zone to spawn items in. Leave undefined for all.</param>
		/// <param name="itemType">Optionally spawn a single item type. Leave null to use base spawn queue.</param>
		/// <param name="onlySafeRooms">Only spawn items in rooms that are accessible without a card.</param>
		public void SpawnItems(int numberOfItems, ZoneType zone = ZoneType.UNDEFINED, ItemType itemType = ItemType.NULL, bool onlySafeRooms = false)
		{
			var usingZone = zone != ZoneType.UNDEFINED;
			var usingItem = itemType != ItemType.NULL;

			while (FreeRooms.Count > 0 && numberOfItems > 0)
			{
				var room = FreeRooms[cachedRoomIndex];

				if ((!onlySafeRooms || (onlySafeRooms && room.Room.IsSafe)) &&
					(!usingZone || (usingZone && room.Room.Zone == zone)))
				{
					if (!usingItem)
					{
						itemType = GetNextItem();
					}

					SpawnItemInRoom(itemType, room);

					numberOfItems--;
				}

				cachedRoomIndex++;

				if (cachedRoomIndex == FreeRooms.Count)
				{
					if (cachedDidWeSpawnItem)
					{
						ResetRoomIndexer();
					}
					else
					{
						cachedRoomIndex = 0;
						return;
					}
				}
			}
		}

		/// <summary>
		/// Use this to spawn a bunch of item types in an array.
		/// </summary>
		public void SpawnItems(ItemType[] itemIDs, ZoneType zone = ZoneType.UNDEFINED, bool onlySafeRooms = false)
		{
			var usingZone = zone != ZoneType.UNDEFINED;
			var numberOfItems = itemIDs.Length;

			while (FreeRooms.Count > 0 && numberOfItems > 0)
			{
				var room = FreeRooms[cachedRoomIndex];

				if ((!onlySafeRooms || (onlySafeRooms && room.Room.IsSafe)) &&
					(!usingZone || (usingZone && room.Room.Zone == zone)))
				{
					numberOfItems--;
					SpawnItemInRoom(itemIDs[numberOfItems], room);
				}

				cachedRoomIndex++;

				if (cachedRoomIndex == FreeRooms.Count)
				{
					if (cachedDidWeSpawnItem)
					{
						ResetRoomIndexer();
					}
					else
					{
						cachedRoomIndex = 0;
						return;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SpawnItemInRoom(ItemType itemType, ItemRoom room)
		{
			var spawnPoints = room.ItemSpawnPoints;
			var pointsLength = spawnPoints.Count;

			for (int n = 0; n < pointsLength; n++)
			{
				var spawnPoint = spawnPoints[n];

				if (spawnPoint.IsFree)
				{
					var itemGo = cachedInventory.SetPickup((int)itemType, -4.65664672E+11f, Tools.VecToVec3(spawnPoint.Position), Quaternion.Euler(Tools.VecToVec3(spawnPoint.Rotation)), 0, 0, 0) ?? throw new ArgumentNullException(nameof(spawnPoint));
					var item = itemGo.GetComponent<Pickup>();
					spawnPoint.ItemPickup = item;
					cachedDidWeSpawnItem = true;

					room.CurrentItemsSpawned--;
					if (room.AtMaxItemSpawns)
					{
						room.IsFree = false;
						FreeRooms.RemoveAt(cachedRoomIndex);
						cachedRoomIndex--;
					}

					return;
				}
			}
		}
	}
}
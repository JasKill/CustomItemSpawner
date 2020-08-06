using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ArithFeather.AriToolKit;
using ArithFeather.AriToolKit.Components;
using ArithFeather.AriToolKit.PointEditor;
using Exiled.API.Features;
using UnityEngine;

namespace ArithFeather.CustomItemSpawner.ItemData {
	internal static class SpawnPointCreator {
		private const string PositionDataFileName = "ItemSpawnPoints";

		private static readonly string SavedItemFilePath = Path.Combine(Paths.Configs, "ItemSpawnTypes.txt");
		private static readonly string SavedSpawnPointPath = Path.Combine(PointIO.FolderPath, PositionDataFileName) + ".txt";

		private static readonly Vector3 ItemSpawnOffset = new Vector3(0, 0.05f, 0);

		private static readonly List<SpawnPoint> SpawnPoints = PointAPI.GetPointList(PositionDataFileName);
		private static readonly Dictionary<string, List<SpawnPoint>> RoomGroupedSpawnPoints = new Dictionary<string, List<SpawnPoint>>();

		private static readonly Dictionary<string, QueuedList> QueuedLists = new Dictionary<string, QueuedList>();
		private static readonly List<QueuedList> QueuedListsList = new List<QueuedList>();
		private static readonly Dictionary<string, ItemList> ItemLists = new Dictionary<string, ItemList>();
		private static readonly List<SavedItemType> SavedItemTypes = new List<SavedItemType>();
		private static readonly Dictionary<string, RoomItems> RawDataRoomItems = new Dictionary<string, RoomItems>();

		public static readonly Dictionary<byte, RoomItemComponent> RoomItems = new Dictionary<byte, RoomItemComponent>();
		public static readonly List<RoomItemComponent> RoomItemsList = new List<RoomItemComponent>();

		public static void Reload() {
			RoomGroupedSpawnPoints.Clear();
			QueuedLists.Clear();
			QueuedListsList.Clear();
			ItemLists.Clear();
			SavedItemTypes.Clear();
			RawDataRoomItems.Clear();
			RoomItems.Clear();
			RoomItemsList.Clear();

			LoadItemData();
			SetFixedSpawnPoints();
		}

		public static void OnLoadSpawnPoints(int seed) {

			// Check this and create the file here so the default game spawn points are loaded.
			if (!FileManager.FileExists(SavedSpawnPointPath)) {
				CreateDefaultSpawnPointsFile();
				SetFixedSpawnPoints();
			}

			UnityEngine.Random.InitState(seed);

			var pointsCount = SpawnPoints.Count;

			if (pointsCount == 0) return;

			var rooms = Rooms.CustomRooms;
			var roomCount = rooms.Count;

			for (var i = 0; i < roomCount; i++) {
				var customRoom = rooms[i];
				var roomTransform = customRoom.transform;
				var roomName = customRoom.FixedName;

				if (RoomGroupedSpawnPoints.TryGetValue(roomName, out var spawnList) &&
					RawDataRoomItems.TryGetValue(roomName, out var itemRoom)) {

					// Transform points to room direction
					var fixedSpawnList = new List<ItemSpawnPoint>();
					var spawnCount = spawnList.Count;

					var list = new List<IItemObtainable>(itemRoom.Items.Count + itemRoom.QueuedLists.Count +
														 itemRoom.ItemLists.Count);

					// Shuffle the lists before adding them
					itemRoom.Items.UnityShuffle();
					itemRoom.QueuedLists.UnityShuffle();
					itemRoom.ItemLists.UnityShuffle();

					list.AddRange(itemRoom.Items);
					list.AddRange(itemRoom.QueuedLists);
					list.AddRange(itemRoom.ItemLists);

					var ric = new RoomItemComponent(
							customRoom,
							fixedSpawnList,
							list
						);

					for (int j = 0; j < spawnCount; j++) {
						var spawn = spawnList[j];

						fixedSpawnList.Add(new ItemSpawnPoint(ric,
							roomTransform.TransformPoint(spawn.Position) + ItemSpawnOffset,
							roomTransform.TransformDirection(spawn.Rotation)
						));
					}

					if (fixedSpawnList.Count > 0 && list.Count > 0) {
						ric.MaxItemsAllowed = Mathf.Clamp(itemRoom.MaxItemsAllowed, 0, fixedSpawnList.Count);
						RoomItemsList.Add(ric);
						RoomItems.Add(customRoom.Id, ric);
					}
				}
			}

			RoomItemsList.UnityShuffle();
			var listSize = QueuedListsList.Count;
			for (int i = 0; i < listSize; i++) {
				QueuedListsList[i].Reset();
			}
		}

		#region Creating default text files

		private static void CreateDefaultSpawnPointsFile() {
			var ris = RandomItemSpawner.singleton;

			// Save Position data
			var positionData = ris.posIds;
			var positionDataLength = positionData.Length;

			SpawnPoints.Capacity = positionDataLength;

			for (int i = 0; i < positionDataLength; i++) {
				var dat = positionData[i];
				var itemTransform = dat.position;

				var room = itemTransform.GetComponentInParent<CustomRoom>();

				if (room == null)
				{
					Log.Error($"Could not find Custom Room for {dat.posID}");
					continue;
				}

				var roomTransform = room.gameObject.transform;

				var localItemPosition = roomTransform.position - itemTransform.position;
				var localItemRotation = roomTransform.eulerAngles - itemTransform.eulerAngles;



				SpawnPoints.Add(new SpawnPoint(room.FixedName, room.Room.Zone, localItemPosition,
					localItemRotation));
			}

			PointIO.Save(SpawnPoints, SavedSpawnPointPath);
		}


		private static readonly string ItemSpawnTypesDescription =
	"# How to use this.\n" +
	"\n" +
	"# When the server spawns items, it will...\n" +
	"# First: Spawn all items assigned to spawn in a room.\n" +
	"# Example: This will force the HID room to spawn the MicroHID.\n" +
	"# HCZ_Hid:1:16\n" +
	"\n" +
	"# Second: Spawn the \"Queued Lists\" randomly across random rooms until the queue is empty.\n" +
	"# Example, this will make sure at least 2 checkpoint keycard will spawn somewhere in LCZ.\n" +
	"# Queued Lists:\n" +
	"# SpawnLCZ:3,3\n" +
	"# Rooms:\n" +
	"# LCZ_Toilets:SpawnLCZ\n" +
	"# LCZ_372:SpawnLCZ\n" +
	"# LCZ_Cafe:SpawnLCZ\n" +
	"# LCZ_Plants:SpawnLCZ\n" +
	"# LCZ_173:SpawnLCZ\n" +
	"\n" +
	"# Third: Any \"Item Lists\" you attached to the rooms will spawn a random item from that list.\n" +
	"# Example: You can use this for rarities.\n" +
	"# Item Lists:\n" +
	"# HighRarity:21,25\n" +
	"# LowRarity:12,14,15\n" +
	"# Rooms:\n" +
	"# LCZ_Armory:LowRarity,LowRarity,LowRarity,HighRarity,HighRarity\n" +
	"# (This will spawn 3 random items from the LowRarity list and 2 items from the HighRarity list in Light Containment Armory.\n" +
	"\n" +
	"# -Again, the difference between a Queued List and Item List is: A Queued list will spawn all the items inside it, across all the rooms it is attached to. Where an Item List will only spawn 1 random item inside the list.\n" +
	"# -You can add an Item List to a Queued List, but you can't add a Queued List to an Item List, or an Item List to an Item List.\n\n";

		private static void CreateItemRoomFile() {
			using (var writer = new StreamWriter(File.Create(SavedItemFilePath))) {

				// Description
				writer.Write(ItemSpawnTypesDescription);

				// Display Items
				writer.WriteLine("# Items:\n");
				writer.WriteLine("# *=Random Item");
				var names = Enum.GetNames(typeof(ItemType));
				var nameLength = names.Length;

				for (int i = 0; i < nameLength; i++) {
					writer.WriteLine($"# {i}={names[i]}");
				}

				writer.WriteLine();

				// Display Lists

				writer.WriteLine("[Item Lists]");
				writer.WriteLine();
				writer.WriteLine("Garbage:0,1,15,19,22,26,28,29,35");
				writer.WriteLine("Common:2,3,4,12,14,23,25,33,34");
				writer.WriteLine("Uncommon:5,6,17,18,24,30,31,32");
				writer.WriteLine("Rare:7,8,9,13,20,21");
				writer.WriteLine("VeryRare:10,11,16");
				writer.WriteLine();

				// Display Queued Lists

				writer.WriteLine("[Queued Lists]");
				writer.WriteLine();
				writer.WriteLine("LCZQueue:3,2,3,13,Garbage,Garbage,Uncommon,Common");
				writer.WriteLine("EntranceQueue:*,*,*,VeryRare,Common,Uncommon,Uncommon,Garbage,Garbage,Garbage");
				writer.WriteLine();


				// Display Rooms
				writer.WriteLine("[Rooms]");
				writer.WriteLine("# RoomName : Max items allowed at a time : Item Data (1,6,Rare,Uncommon)\n");

				// Lazy, used an escape because unique rooms aren't guaranteed.
				writer.Write(
					"#Light Containment\nLCZ_ClassDSpawn:\nLCZ_Curve:\nLCZ_Toilets:3:LCZQueue\nLCZ_ChkpB:\nLCZ_ChkpA:\nLCZ_TCross:\nLCZ_Crossing:\nLCZ_372:1:LCZQueue\nLCZ_Straight:\nLCZ_Cafe:4:LCZQueue\nLCZ_Plants:1:LCZQueue\nLCZ_Armory:5:LCZQueue,Common,Common,Rare,Uncommon,*\nLCZ_Airlock:\nLCZ_173:1:LCZQueue\nLCZ_914:\nLCZ_012:2:LCZQueue\n\n#Heavy Containment\nHCZ_106:\nHCZ_Hid:1:16\nHCZ_EZ_Checkpoint:\nHCZ_Crossing:\nHCZ_457:2:VeryRare,Common,*\nHCZ_Tesla:\nHCZ_049:Uncommon:Rare,*,*\nHCZ_Room3ar:5:Common:Common:Uncommon:Rare,*\nHCZ_ChkpA:\nHCZ_Curve:\nHCZ_Room3:\nHCZ_Nuke:1:Uncommon\nHCZ_Testroom:\nHCZ_Servers:1:Common,*\nHCZ_079:\nHCZ_ChkpB:\nHCZ_Tesla:\n\n#Entrance\nEZ_Endoof:\nEZ_Crossing:\nEZ_GateB:\nEZ_Smallrooms2:2:EntranceQueue\nEZ_Cafeteria:2:EntranceQueue\nEZ_PCs_small:2:EntranceQueue\nEZ_Curve:\nEZ_Intercom:\nEZ_Straight:\nEZ_upstairs:\nEZ_PCs:2:EntranceQueue\nEZ_GateA:\nEZ_Shelter:\n\n#Surface\nRoot_*&*Outside Cams:\n");
			}
		}

		#endregion

		#region Loading the ItemRoomFile

		private static void SetFixedSpawnPoints() {
			var pointsCount = SpawnPoints.Count;

			if (!FileManager.FileExists(SavedSpawnPointPath)) return;

			if (pointsCount == 0) {
				Log.Error("No Spawn Points Set.");
				return;
			}

			var rooms = new List<string>(RawDataRoomItems.Keys);
			var roomCount = rooms.Count;

			for (int i = 0; i < roomCount; i++) {
				var roomName = rooms[i];
				var pointList = new List<SpawnPoint>();

				for (int j = 0; j < pointsCount; j++) {
					var point = SpawnPoints[j];
					var spawnRoomName = point.RoomType;

					if (roomName.Equals(spawnRoomName, StringComparison.InvariantCultureIgnoreCase)) {
						pointList.Add(point);
					}
				}

				if (pointList.Count > 0) {
					RoomGroupedSpawnPoints.Add(roomName, pointList);
				}
			}
		}

		internal enum Section {
			None,
			ItemList,
			QueuedList,
			Rooms
		}

		private static readonly Dictionary<string, Section> Sections = new Dictionary<string, Section> {
			{"None", Section.None},
			{"Rooms", Section.Rooms},
			{"Item Lists", Section.ItemList},
			{"Queued Lists", Section.QueuedList},
		};

		private static readonly List<string> LoadedItemData = new List<string>();
		private static Section _lastFoundSection;

		private static readonly Regex SectionHeader = new Regex(@"\[(?<Name>[a-zA-Z\s]*)\]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

		private static void LoadItemData() {
			try {
				if (!FileManager.FileExists(SavedItemFilePath)) {
					CreateItemRoomFile();
				}

				using (var reader = File.OpenText(SavedItemFilePath)) {

					// Create SavedItemType instances for dictionary
					for (int i = 0; i < SavedItemType.ItemTypeLength; i++) {
						SavedItemTypes.Add(new SavedItemType((ItemType)i));
					}
					SavedItemTypes.Add(new SavedItemType());

					_lastFoundSection = Section.None;

					while (!reader.EndOfStream) {
						var line = reader.ReadLine();

						if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

						LoadedItemData.Add(line); // Save for later parsing

						if (CheckForSection(line)) continue;

						var sData = line.Split(':');
						var sDataLength = sData.Length;

						if (sDataLength != 2) continue;

						switch (_lastFoundSection) {
							case Section.ItemList:
								ItemLists.Add(sData[0], new ItemList());
								break;

							case Section.QueuedList:
								var qList = new QueuedList();
								QueuedListsList.Add(qList);
								QueuedLists.Add(sData[0], qList);
								break;
						}
					}
				}
			} catch (Exception e) {
				Log.Error("Failed to load Item Data, try deleting the file and reloading to reset to default.");
				Log.Error(e.Message);
			}

			SecondPass();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CheckForSection(string line) {
			var match = SectionHeader.Match(line);
			if (match.Success && Sections.TryGetValue(match.Groups["Name"].Value, out var section)) {
				_lastFoundSection = section;
				return true;
			}

			return false;
		}

		private static void CantLoadData(string data) => Log.Error($"Section \"{_lastFoundSection}\", Key \"{data}\": Could not load data.");
		private static void TooManySplitters(string key) => Log.Error($"Section \"{_lastFoundSection}\", Key \"{key}\": too many ':' splitters.");

		private static void SecondPass() {
			_lastFoundSection = Section.None;
			var textFileLength = LoadedItemData.Count;

			for (int i = 0; i < textFileLength; i++) {
				var line = LoadedItemData[i];

				if (CheckForSection(line)) continue;

				var sData = line.Split(':');
				var sDataLength = sData.Length;

				var data1 = sData[0];

				switch (_lastFoundSection) {
					case Section.ItemList:

						if (sDataLength != 2) {
							if (sDataLength > 2) TooManySplitters(data1);
							continue;
						}

						var data = sData[1].Split(',');
						var dataLength = data.Length;

						if (ItemLists.TryGetValue(data1, out var itemList)) {
							if (dataLength > 0) {
								var theList = new List<SavedItemType>(dataLength);

								for (int k = 0; k < dataLength; k++) {
									var item = data[k].Trim();

									var instance = GetInstance(item);

									if (instance == null) continue;

									if (instance.GetType() != typeof(SavedItemType)) {
										Log.Error(
											$"Section \"{_lastFoundSection}\", Key \"{data1}\": Failed to add {item}. You can only add Items to Item Lists");
									} else {
										theList.Add((SavedItemType)instance);
									}
								}

								if (theList.Count == 0) {
									CantLoadData(data1);
									continue;
								}

								itemList.Items = theList;
							} else {
								CantLoadData(data1);
								ItemLists.Remove(data1);
							}
						}

						break;

					case Section.QueuedList:

						if (sDataLength != 2) {
							if (sDataLength > 2) TooManySplitters(data1);
							continue;
						}

						data = sData[1].Split(',');
						dataLength = data.Length;

						if (QueuedLists.TryGetValue(data1, out var queuedList)) {
							if (dataLength > 0) {

								var theList = new List<IItemObtainable>(dataLength);

								for (int k = 0; k < dataLength; k++) {
									var item = data[k].Trim();

									var instance = GetInstance(item);

									if (instance == null) continue;

									theList.Add(instance);
								}

								queuedList.Items = theList;
							} else {
								CantLoadData(data1);
								QueuedListsList.Remove(queuedList);
								QueuedLists.Remove(data1);
							}
						}

						break;

					case Section.Rooms:

						if (sDataLength != 3) {
							if (sDataLength > 3) TooManySplitters(data1);

							if (sDataLength == 2 && !string.IsNullOrWhiteSpace(sData[1]) && !int.TryParse(sData[1], out var foo))
								Log.Error($"Section \"{_lastFoundSection}\", Key \"{data1}\": Invalid data: \"{sData[1]}\"");
							continue;
						}

						var room = new RoomItems();
						bool dataAttached = false;

						if (int.TryParse(sData[1], out var result)) {
							room.MaxItemsAllowed = result;
						} else {
							Log.Error($"Section \"{_lastFoundSection}\", Key \"{data1}\": Invalid MaxRoomType value for {data1}");
							continue;
						}

						var itemData = sData[2].Split(',');
						var itemDataLength = itemData.Length;
						if (itemDataLength == 0) continue;

						for (int j = 0; j < itemDataLength; j++) {
							var key = itemData[j].Trim();
							if (string.IsNullOrWhiteSpace(key)) continue;
							var instance = GetInstance(key);

							if (instance == null) continue;

							dataAttached = true;

							if (instance.GetType() == typeof(ItemList)) {
								room.ItemLists.Add(instance);
							} else if (instance.GetType() == typeof(QueuedList)) {
								room.QueuedLists.Add(instance);
							} else room.Items.Add(instance);
						}

						if (dataAttached)
							RawDataRoomItems.Add(data1, room);
						else {
							CantLoadData(data1);
						}

						break;
				}
			}
		}

		private static IItemObtainable GetInstance(string key) {
			if (key.Equals("*")) return SavedItemTypes[SavedItemTypes.Count - 1];

			if (int.TryParse(key, out var result) && result < SavedItemTypes.Count && result >= 0) {
				return SavedItemTypes[result];
			}

			if (ItemLists.TryGetValue(key, out var il)) {
				return il;
			}

			if (QueuedLists.TryGetValue(key, out var ql)) {
				return ql;
			}

			Log.Error($"Section \"{_lastFoundSection}\": Could not create list for {key}.");
			return null;
		}

		#endregion
	}
}
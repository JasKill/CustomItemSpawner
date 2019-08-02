using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ArithFeather.ArithsToolKit.SpawnPointTools;
using Smod2.API;

namespace ArithFeather.RandomItemSpawner
{
    public static class RoomDataIO
    {
        public static void LoadItemRoomData(ItemSpawning data, string filePath)
        {
			data.Reset();

            if (!File.Exists(filePath))
            {
                using (var writer = new StreamWriter(File.Create(filePath)))
                {
                    var uniqueRooms = new List<CustomRoom>();
					var rooms = CustomRoomManager.Instance.Rooms;
                    var roomCount = rooms.Count;
                    for (var i = 0; i < roomCount; i++)
                    {
                        var room = rooms[i];
                        var foundRoom = false;

                        for (int j = 0; j < uniqueRooms.Count; j++)
                        {
                            if (room.Name == uniqueRooms[j].Name)
                            {
                                foundRoom = true;
                                break;
                            }
                        }

                        if (!foundRoom)
                        {
                            uniqueRooms.Add(room);
                        }
                    }

                    string s = "#ItemRarities:";
                    var itemRarities = Enum.GetNames(typeof(ItemRarity));
                    var itemCount = itemRarities.Length;
                    for (int i = 0; i < itemCount; i++)
                    {
                        var item = itemRarities[i];
                        s += "|" + item + "=" + i;
                    }
                    writer.WriteLine(s + "|");
                    writer.WriteLine("BaseItemSpawnQueue:4,9,4,6,3,4,2,7,4,6,5,1,4,7,2,4,6,9,4,5,2,4,5,4,1,7,2,3,4,6,7,9,8");
                    writer.WriteLine("SpawnItemsSpawnQueue:0,4,9");
                    writer.WriteLine("NumberItemsOnDeath:5");
                    writer.WriteLine("NumberItemsOnStart:20");
                    writer.WriteLine();
                    writer.WriteLine("#RoomName:NumberOfItemsMax");
                    foreach (CustomRoom room in uniqueRooms)
                    {
                        writer.WriteLine(room.Name + ":2");
                    }
                }
            }
            else
            {
                using (StreamReader reader = File.OpenText(filePath))
                {
                    while (!reader.EndOfStream)
                    {
                        var item = reader.ReadLine();

                        if (string.IsNullOrWhiteSpace(item))
                        {
                            continue;
                        }

                        if (item[0] == '#')
                        {
                            continue;
                        }

                        string[] sData = item.Split(':');

                        if (sData.Length == 0)
                        {
                            continue;
                        }

                        switch (sData[0])
                        {
	                        case "BaseItemSpawnQueue":
	                        {
		                        var items = sData[1].Split(',');
		                        var itemCount = items.Length;
		                        int[] intItems = new int[itemCount];
		                        for (int i = 0; i < itemCount; i++)
		                        {
			                        intItems[i] = int.Parse(items[i]);
		                        }
		                        data.BaseItemSpawnQueue = intItems;
		                        break;
	                        }

	                        case "SpawnItemsSpawnQueue":
	                        {
		                        var items = sData[1].Split(',');
		                        var itemCount = items.Length;
		                        int[] intItems = new int[itemCount];
		                        for (int i = 0; i < itemCount; i++)
		                        {
			                        intItems[i] = int.Parse(items[i]);
		                        }
		                        data.SafeItemsSpawnQueue = intItems;
		                        break;
	                        }

	                        case "NumberItemsOnDeath":
		                        data.NumberItemsOnDeath = int.Parse(sData[1]);
		                        break;
	                        case "NumberItemsOnStart":
		                        data.NumberItemsOnStart = int.Parse(sData[1]);
		                        break;
	                        default:
		                        data.AddRoomData(sData[0], int.Parse(sData[1]));
		                        break;
                        }
                    }
                }
            }
        }
    }
}
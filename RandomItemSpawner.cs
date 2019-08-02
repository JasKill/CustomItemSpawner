using Smod2;
using Smod2.Attributes;
using Smod2.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArithFeather.RandomItemSpawner
{
	[PluginDetails(
		author = "Arith",
		name = "Scattered Survival",
		description = "",
		id = "ArithFeather.ScatteredSurvival",
		configPrefix = "afss",
		version = "1.0",
		SmodMajor = 3,
		SmodMinor = 4,
		SmodRevision = 0
		)]
	public class RandomItemSpawner : Plugin
	{
		[ConfigOption] public bool DisablePlugin = false;

		/// <summary>
		/// Set this to false if you want to program in your own logic for spawning.
		/// </summary>
		public bool UseDefaultEvents = true;

		private static RandomItemSpawner instance;
		public static RandomItemSpawner Instance
		{
			get
			{
				if (instance == null)
				{
					throw new System.Exception("Player Lives needs to be registered first.");
				}
				else return instance;
			}
		}

		public override void OnDisable() => Info("RandomItemSpawner Disabled");
		public override void OnEnable() => Info("RandomItemSpawner Enabled");

		private EventHandler eventHandler;

		public override void Register()
		{
			instance = this;
			AddEventHandlers(eventHandler = new EventHandler(this));
		}

		public ItemSpawning ItemSpawning => eventHandler.ItemRoomData;
	}
}

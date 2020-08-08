using Exiled.API.Interfaces;

namespace ArithFeather.CustomItemSpawner {
	public class Config : IConfig {
		public bool IsEnabled { get; set; } = true;

		public bool EnableItemTracking { get; set; } = false;
	}
}

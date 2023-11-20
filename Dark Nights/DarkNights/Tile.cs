using System;

namespace DarkNights
{
	public class TileData
	{
		public int X => Coordinates.X;
		public int Y => Coordinates.Y;

		public Coordinates Coordinates { get; set; }
		public TileContainer Container { get; set; }
	}
}

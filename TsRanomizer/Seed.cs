﻿using System.Globalization;

namespace TsRandomizer
{
	struct Seed
	{
		readonly uint value;

		public Seed(uint seed)
		{value = seed;
			

			ExceptionLogger.SetSeedContext(seed);
		}

		public Seed(int seed) : this((uint)seed)
		{
		}

		public static bool TrySetFromHexString(string text, out Seed seed)
		{
			if (!uint.TryParse(text, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out uint parsedValue))
			{
				seed = default(Seed);
				return false;
			}

			seed = new Seed(parsedValue);
			return true;
		}

		public override string ToString()
		{
			return value.ToString("X8");
		}

		public static implicit operator int(Seed seed)
		{
			return (int)seed.value;
		}

		public static implicit operator uint(Seed seed)
		{
			return seed.value;
		}

		public static implicit operator Seed(uint seed)
		{
			return new Seed(seed);
		}

		public static implicit operator Seed(int seed)
		{
			return new Seed(seed);
		}
	}
}

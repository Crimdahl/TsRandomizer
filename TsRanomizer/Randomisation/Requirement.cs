﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace TsRandomizer.Randomisation
{
	struct Requirement : IEquatable<Requirement>
	{
		public static Requirement TeleportationGates { get; }

		static readonly Dictionary<Requirement, string> Flags;

		public static readonly Requirement None = 0UL;
		public static readonly Requirement TimeStop = 1UL << 0;
		public static readonly Requirement ForwardDash = 1UL << 1;
		public static readonly Requirement AntiWeed = 1UL << 3;
		public static readonly Requirement CardA = 1UL << 4;
		public static readonly Requirement CardB = 1UL << 5;
		public static readonly Requirement CardC = 1UL << 6;
		public static readonly Requirement CardD = 1UL << 7;
		public static readonly Requirement CardE = 1UL << 8;
		public static readonly Requirement CardV = 1UL << 9;
		public static readonly Requirement TimespinnerSpindle = 1UL << 10;
		public static readonly Requirement Swimming = 1UL << 11;
		public static readonly Requirement UpwardDash = 1UL << 12;
		public static readonly Requirement DoubleJump = 1UL << 13;
		public static readonly Requirement GassMask = 1UL << 14;
		public static readonly Requirement Teleport = 1UL << 15;
		public static readonly Requirement TimespinnerPiece1 = 1UL << 21;
		public static readonly Requirement TimespinnerPiece2 = 1UL << 22;
		public static readonly Requirement TimespinnerPiece3 = 1UL << 23;
		public static readonly Requirement PinkOrb = 1UL << 25;
		public static readonly Requirement TimespinnerWheel = 1UL << 26;

		public static readonly Requirement GateKittyBoss = 1UL << 50;
		public static readonly Requirement GateLeftLibrary = 1UL << 51;
		public static readonly Requirement GateSealedSirensCave = 1UL << 52;
		public static readonly Requirement GateLakeSirineLeft = 1UL << 53;
		public static readonly Requirement GateLakeSirineRight = 1UL << 54;
		public static readonly Requirement GateAccessToPast = 1UL << 55;

		readonly ulong flags;

		static Requirement()
		{
			Flags = typeof(Requirement)
				.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
				.Where(f => f.Name != nameof(None))
				.ToDictionary(f => (Requirement)f.GetValue(null), f => f.Name);

			TeleportationGates = Flags
				.Where(f => f.Value.StartsWith("Gate"))
				.Select(f => f.Key)
				.Aggregate((a, b) => a | b);
		}

		Requirement(ulong flags)
		{
			this.flags = flags;
		}

		[Pure]
		public bool Contains(Requirement other)
		{
			return (flags & other.flags) > 0;
		}

		[Pure]
		public bool IsSingleRequirement()
		{
			return (flags & (flags - 1)) == 0;
		}

		[Pure]
		public Requirement[] Split()
		{
			if (IsSingleRequirement())
				return new[] {this};

			List<Requirement> flaggedRequirements = new List<Requirement>();

			foreach (var flag in Flags)
			{
				if (((ulong)flag.Key & flags) == (ulong)flag.Key)
					flaggedRequirements.Add(flag.Key);
			}

			return flaggedRequirements.ToArray();
		}

		[Pure]
		public override bool Equals(object obj)
		{
			if (obj is null) return false;
			return obj is Requirement item && Equals(item);
		}

		[Pure]
		public bool Equals(Requirement other)
		{
			return flags == other.flags;
		}

		[Pure]
		public override int GetHashCode()
		{
			return flags.GetHashCode();
		}

		public static implicit operator Requirement(ulong value)
		{
			return new Requirement(value);
		}

		public static implicit operator ulong(Requirement value)
		{
			return value.flags;
		}

		public static Requirement operator |(Requirement a, Requirement b)
		{
			return a.flags | b.flags;
		}

		public static Gate operator &(Requirement a, Requirement b)
		{
			return (Gate)a & b;
		}

		public static bool operator ==(Requirement a, Requirement b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(Requirement a, Requirement b)
		{
			return !(a == b);
		}

		public override string ToString()
		{
			return GetFlagNames(flags);
		}

		static string GetFlagNames(ulong flags)
		{
			if (flags == None) return "";

			var flagNames = Flags
				.Where(f => ((ulong)f.Key & flags) > 0)
				.Select(f => ToShortName(f.Value));

			return string.Join("|", flagNames);
		}

		static string ToShortName(string name)
		{
			switch (name)
			{
				case "TimeStop": return "TS";
				case "ForwardDash": return "FD";
				case "Lightwall": return "LW";
				case "AntiWeed": return "AW";
				case "CardA": return "cA";
				case "CardB": return "cB";
				case "CardC": return "cC";
				case "CardD": return "cD";
				case "CardE": return "cE";
				case "CardV": return "cV";
				case "TimespinnerSpindle": return "Spindle";
				case "Swimming": return "Sw";
				case "GassMask": return "Gas";
				case "Teleport": return "TP";
				case "PinkOrb": return "PO";
				default:
					return name;
			}
		}
	}
}
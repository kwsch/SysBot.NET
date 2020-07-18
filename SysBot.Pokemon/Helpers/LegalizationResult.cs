using PKHeX.Core;

namespace SysBot.Pokemon
{
	public class LegalizationResult
	{
		public PKM Pokemon { get; }
		
		public string Result { get; }

		public LegalizationResult(PKM pokemon, string result)
		{
			Pokemon = pokemon;
			Result = result;
		}
	}
}

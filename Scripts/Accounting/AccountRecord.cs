using System.Collections.Generic;

namespace Server.Accounting
{
	public class AccountRecord
	{
		public string Username { get; set; }
		public List<CharacterRecord> Characters  { get; set; }
	}
}
using System.Net;
using Server.Accounting;

namespace Server.Network
{
	public class RemoteServerPlayRequest
	{
		public ClientVersion ClientVersion { get; set; }
		public uint Seed { get; set; }
		public uint AuthId { get; set; }
		public IAccount Account { get; set; }
	}
}
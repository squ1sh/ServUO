using System.Collections.Generic;
using System.Linq;

namespace Server.Misc
{
	public class AdditionalServers
	{
		public ServerInfo[] Servers { get; set; }

		public List<ServerInfo> ServerList
		{
			get { return Servers == null ? new List<ServerInfo>() : Servers.ToList(); }
			set { Servers = value?.ToArray(); }
		}
	}

	public class ServerInfo
	{
		public string ServerName { get; set; }
		public string Address { get; set; }
		public int Port { get; set; }
	}
}
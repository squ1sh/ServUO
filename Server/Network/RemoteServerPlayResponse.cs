namespace Server.Network
{
	public class RemoteServerPlayResponse
	{
		public NetState SendingState { get; set; }
		public NetState RespondingState { get; set; }
		public PlayServerAck PlayServerAck { get; set; }
	}
}
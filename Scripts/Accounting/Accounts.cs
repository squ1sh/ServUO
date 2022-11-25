using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Server.Accounting
{
    public class Accounts
    {
        private static Dictionary<string, IAccount> m_Accounts = new Dictionary<string, IAccount>();
		private static readonly string AccountDirectory = Config.Get("Server.AccountPath", "Saves/Accounts");


		public static void Configure()
        {
            EventSink.WorldLoad += Load;
            EventSink.WorldSave += Save;
			EventSink.MergeAccountFile += MergeAccounts;
			EventSink.ChangeCharacterShard += ChangeShardName;
		}

        static Accounts()
        {
        }

        public static int Count => m_Accounts.Count;

        public static ICollection<IAccount> GetAccounts()
        {
            return m_Accounts.Values;
        }

        public static IAccount GetAccount(string username)
        {
            IAccount a;

            m_Accounts.TryGetValue(username, out a);

            return a;
        }

        public static void Add(IAccount a)
        {
            m_Accounts[a.Username] = a;
        }

        public static void Remove(string username)
        {
            m_Accounts.Remove(username);
        }

        public static void Load()
        {
            m_Accounts = new Dictionary<string, IAccount>(32, StringComparer.OrdinalIgnoreCase);

			var accounts = GetAccountNode();

			if (accounts != null)
			{
				foreach (XmlElement account in accounts)
				{
					try
					{
						Account acct = new Account(account);
					}
					catch (Exception e)
					{
						Console.WriteLine("Warning: Account instance load failed");
						Diagnostics.ExceptionLogging.LogException(e);
					}
				}
			}			
        }

		public static List<AccountRecord> LoadAccountRecords()
		{
			var accountRecords = new List<AccountRecord>();

			var accounts = GetAccountNode();

			if (accounts != null)
			{
				foreach (XmlElement account in accounts)
				{
					try
					{
						var username = Utility.GetText(account["username"], "empty");
						var characters = Account.LoadCharacterRecords(account);

						if (!string.IsNullOrWhiteSpace(username))
						{
							accountRecords.Add(new AccountRecord { Username = username, Characters = characters });
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("Warning: Account instance load failed");
						Diagnostics.ExceptionLogging.LogException(e);
					}
				}				
			}

			return accountRecords;
		}

		private static XmlNodeList GetAccountNode(string accountFileLocation = null)
		{
			string filePath = accountFileLocation ?? Path.Combine(AccountDirectory, "accounts.xml");

			if (!File.Exists(filePath))
				return null;

			XmlDocument doc = new XmlDocument();
			doc.Load(filePath);

			XmlElement root = doc["accounts"];

			return root.GetElementsByTagName("account");
		}

		public static void Save(WorldSaveEventArgs e)
        {
			try
			{
				var accountRecords = LoadAccountRecords();

				if (!Directory.Exists(AccountDirectory))
					Directory.CreateDirectory(AccountDirectory);

				string filePath = Path.Combine(AccountDirectory, "accounts.xml");

				using (StreamWriter op = new StreamWriter(filePath))
				{
					XmlTextWriter xml = new XmlTextWriter(op)
					{
						Formatting = Formatting.Indented,
						IndentChar = '\t',
						Indentation = 1
					};

					xml.WriteStartDocument(true);

					xml.WriteStartElement("accounts");

					xml.WriteAttributeString("count", m_Accounts.Count.ToString());

					foreach (Account a in GetAccounts())
					{
						var characters = accountRecords.FirstOrDefault(account => a.Username.Equals(account.Username, StringComparison.InvariantCultureIgnoreCase))
							?.Characters ?? new List<CharacterRecord>();

						a.Save(xml, characters);
					}

					xml.WriteEndElement();

					xml.Close();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Warning: Account file failed to save.");
				Diagnostics.ExceptionLogging.LogException(ex);
			}			
        }

		public static void ChangeShardName(ChangeCharacterShardEventArgs e)
		{
			try
			{
				e.Success = true;

				var currentAccounts = GetAccountNode();

				var accounts = new List<XmlElement> ();

				if (currentAccounts != null)
				{
					foreach (XmlElement accountXml in currentAccounts)
					{
						foreach(XmlElement characterElement in accountXml["chars"].GetElementsByTagName("char"))
						{
							var serverName = Utility.GetAttribute(characterElement, "server", "");

							if (serverName.Equals(e.OldShardName, StringComparison.OrdinalIgnoreCase))
							{
								characterElement.SetAttribute("server", Core.ServerName);
							}
						}

						accounts.Add(accountXml);
					}
				}

				if (!Directory.Exists(AccountDirectory))
					Directory.CreateDirectory(AccountDirectory);

				string filePath = Path.Combine(AccountDirectory, "accounts.xml");

				using (StreamWriter op = new StreamWriter(filePath))
				{
					XmlTextWriter xml = new XmlTextWriter(op)
					{
						Formatting = Formatting.Indented,
						IndentChar = '\t',
						Indentation = 1
					};

					xml.WriteStartDocument(true);

					xml.WriteStartElement("accounts");

					xml.WriteAttributeString("count", accounts.Count.ToString());

					accounts.ForEach(a => a.WriteTo(xml));

					xml.WriteEndElement();

					xml.Close();
				}
			}
			catch (Exception ex)
			{
				e.Success = false;
				Console.WriteLine("Warning: Account file failed to save.");
				Diagnostics.ExceptionLogging.LogException(ex);
			}
		}

		public static void MergeAccounts(MergeAccountsEventArgs e)
		{
			try
			{
				e.Success = true;

				var currentAccounts = GetAccountNode();
				var otherAccounts = GetAccountNode(e.AccountFileLocation);

				var mergedAccounts = new List<XmlElement>();

				if (currentAccounts != null)
				{
					foreach (XmlElement accountXml in currentAccounts)
					{
						var username = Utility.GetText(accountXml["username"], "empty");

						XmlElement charsNode = accountXml["chars"];

						if(otherAccounts != null)
						{
							foreach (XmlElement otherAccountXml in otherAccounts)
							{
								var otherUsername = Utility.GetText(otherAccountXml["username"], "empty");

								if(username.Equals(otherUsername, StringComparison.OrdinalIgnoreCase))
								{
									foreach (XmlElement otherChar in otherAccountXml["chars"].GetElementsByTagName("char"))
									{
										var newElement = charsNode.OwnerDocument.ImportNode(otherChar, true);

										charsNode.AppendChild(newElement);
									}

									break;
								}
							}
						}

						mergedAccounts.Add(accountXml);
					}

					if (otherAccounts != null)
					{
						foreach(XmlElement otherAccountXml in otherAccounts)
						{
							var otherUsername = Utility.GetText(otherAccountXml["username"], "empty");

							bool found = false;

							foreach(XmlElement accountXml in currentAccounts)
							{
								var username = Utility.GetText(accountXml["username"], "empty");

								if(username.Equals(otherUsername, StringComparison.OrdinalIgnoreCase))
								{
									found = true;
									break;
								}
							}

							if (!found)
							{
								mergedAccounts.Add(otherAccountXml);
							}
						}
					}
				}
				else if(otherAccounts != null)
				{
					currentAccounts = otherAccounts;
				}

				if (!Directory.Exists(AccountDirectory))
					Directory.CreateDirectory(AccountDirectory);

				string filePath = Path.Combine(AccountDirectory, "accounts.xml");

				using (StreamWriter op = new StreamWriter(filePath))
				{
					XmlTextWriter xml = new XmlTextWriter(op)
					{
						Formatting = Formatting.Indented,
						IndentChar = '\t',
						Indentation = 1
					};

					xml.WriteStartDocument(true);

					xml.WriteStartElement("accounts");

					xml.WriteAttributeString("count", mergedAccounts.Count.ToString());

					mergedAccounts.ForEach(a => a.WriteTo(xml));

					xml.WriteEndElement();

					xml.Close();
				}
			}
			catch (Exception ex)
			{
				e.Success = false;
				Console.WriteLine("Warning: Account file failed to save.");
				Diagnostics.ExceptionLogging.LogException(ex);
			}
		}
    }
}

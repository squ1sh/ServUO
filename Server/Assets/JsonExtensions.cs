using System;
using System.Web.Script.Serialization;

namespace Server.Assets
{
	public static class JsonExtensions
	{
		public static bool TryParseAsJson<T>(this string value ,out T obj)
		{
			obj = default(T);

			try
			{
				obj = new JavaScriptSerializer().Deserialize<T>(value);
				
				if(obj != null)
				{
					return true;
				}
			}
			catch(Exception ex)
			{
				Diagnostics.ExceptionLogging.LogException(ex);
			}

			return false;
		}
	}
}

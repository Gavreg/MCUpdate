using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace MCUlib
{
	public struct Params
	{
		public int port;
	}

	public class ParamsLoad
	{
		static void createEmptyParams()
		{
			XDocument userFile =
				new XDocument(new XElement("params", new XAttribute("port", "1214")));
			userFile.Save("params.xml");
		}

		public static Params loadParams()
		{
			Params r = new Params();

			try
			{
				if (System.IO.File.Exists("params.xml") == false)
					createEmptyParams();

				XDocument xdoc = XDocument.Load("params.xml");
				XElement e = xdoc.Descendants("params").ElementAt<XElement>(0);
				r.port = (int)e.Attribute("port");
			}

			catch
			{
				Console.WriteLine("Error loading params");
				Console.ReadKey();
				Environment.Exit(0);
			}


			return r;

		}


	}
}


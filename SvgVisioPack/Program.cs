using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace SvgVisioPack
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Упаковка SVG-файла.");

            //if (args.Length != 1)
            //{
            //    Console.WriteLine("Необходимо указать исходный файл, например:");
            //    Console.WriteLine();
            //    Console.WriteLine("\tSvgVisioPack.exe test.svg");
            //}
            //else
			{
                string sourceFile = @"C:\Проекты\DrgSCADA\WebTest01\Images\Page1-3.svg";
                //string sourceFile = args[0];
				string newFile = Path.ChangeExtension(sourceFile, ".packed" + Path.GetExtension(sourceFile));

				StringBuilder sb = new StringBuilder();
				using (StreamReader sr = new StreamReader(sourceFile))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						if (!(line.StartsWith("<!DOCTYPE") || line.StartsWith("<!--")))
							sb.AppendLine(line);
					}
				}

				XmlDocument doc = new XmlDocument();
				using (var stringReader = new StringReader(sb.ToString()))
				{
					doc.Load(stringReader);
				}

				// менеджер пространств имен для префикса "v"
				XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("v", "http://schemas.microsoft.com/visio/2003/SVGExtensions/");

				ClearNode(doc, doc.DocumentElement, nsmgr);

				Encoding encoding = Encoding.UTF8;
				if (doc.FirstChild.NodeType == XmlNodeType.XmlDeclaration)
					encoding = Encoding.GetEncoding(((XmlDeclaration)doc.FirstChild).Encoding);

				using (XmlTextWriter xw = new XmlTextWriter(newFile, encoding))
				{
					xw.Formatting = Formatting.Indented;
					xw.IndentChar = '\t';

					doc.Save(xw);
				}

                Console.WriteLine("Результат упаковки:\n" + newFile);
			}

			Console.WriteLine();
			Console.Write("Press Enter to exit...");
			Console.ReadLine();
		}

        /// <summary>
        /// Рекурсивно удаляет узлы "title", а также узлы и атрибуты с префиксом "v"
        /// Кроме того, переносит в узлы "desc" содержимое пользовательских комментариев (v:custProps/v:cp[v:lbl="desc"]/v:val)
        /// </summary>
		/// <param name="node"></param>
		/// <param name="nsmgr"></param>
		static void ClearNode(XmlDocument doc, XmlNode node, XmlNamespaceManager nsmgr)
		{
            bool descMade = false;
			if (node.ChildNodes != null)
			{
                XmlNode nodeDescNode = null;
                List<XmlNode> nodesToRemove = new List<XmlNode>();

                for (int i = 0; i < node.ChildNodes.Count; i++)
				{
					XmlNode childNode = node.ChildNodes[i];
					if (childNode.Name == "desc")
                        nodeDescNode = childNode;
                    else if (childNode.Name == "title" || childNode.Prefix == "v")
                    {
                        // переместить метку в <desc>
                        if (!descMade && childNode.Name == "v:custProps")
                        {
                            XmlNode descNode = childNode.SelectSingleNode("v:cp[@v:lbl='desc']", nsmgr);
                            if (descNode != null)
                            {
                                string descFullText = descNode.Attributes["v:val"].Value;
                                int openBracketIndex = descFullText.IndexOf('(');
                                int closeBracketIndex = descFullText.IndexOf(')');
                                if (openBracketIndex >= 0 && closeBracketIndex > openBracketIndex)
                                {
                                    string desc = descFullText.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1);
									if (nodeDescNode != null)
										nodeDescNode.InnerText = desc;
									else
									{
										// TODO: добавить в node узел desc
                                        XmlElement elem = doc.CreateElement("desc");
                                        elem.InnerText = desc;
                                        node.AppendChild(elem);
									}
                                    descMade = true;
                                }
                            }
                        }

                        nodesToRemove.Add(childNode);
                    }
                    else
                        ClearNode(doc, childNode, nsmgr);
				}

                foreach (var nodeToRemove in nodesToRemove)
                {
                    node.RemoveChild(nodeToRemove);
                }
			}

			if (node is XmlElement && node.Attributes != null)
			{
				for (int i = node.Attributes.Count - 1; i >= 0; i--)
				{
					XmlAttribute attr = node.Attributes[i];
					// если пространство имен или префикс Visio, удаляем атрибут
					if (attr.Name == "xmlns:v" || attr.Prefix == "v")
						(node as XmlElement).RemoveAttributeNode(attr);
				}
			}

		}
	}
}

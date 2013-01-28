using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using NDesk.Options;

namespace AssemblyTester
{
	class Program
	{
		private const string ExtFileFlag = "#!EXTFILE!#=>";

		static void Main(string[] args)
		{
			string typeName = null;
			string methodName = null;
			string file = null;

			var optionSet = new OptionSet() {
					{ "file=", "", s => file = s },
   				{ "type=", "", v => typeName = v },
					{ "method=", "", v => methodName = v}
   				//{ "d|debug",  v => { debug = true; } },
   				//{ "h|?|help",   v => help = true }
				};
			var extra = optionSet.Parse(args);
			var parameters = ParseGroupArray(extra, "p");
			var types = ParseGroupArray(extra, "t");

			// todo: a command to "poke" around and see what methods are available.

			var result = Test(file, typeName, methodName, parameters, types);

			if (result != null)
			{
				Console.WriteLine(XmlSerializeToString(result));
			}

		}

		public static object Test(string fileName, string typeName, string methodName, string[] parametersArray, string[] typesArray)
		{
			Assembly assembly = Assembly.LoadFrom(fileName);
			Type type = assembly.GetType(typeName);
			object result = null;
			if (type != null)
			{
				var methodSplitArray = methodName.Split(new char[] { '[' }, 2);
				var methodNameOnly = methodSplitArray[0];
				var genericTypes = new List<Type>();
				// contains generics
				if (methodSplitArray.Length > 1)
				{
					var genericStrings = methodSplitArray[1].Replace("[", "").Split(']');
					foreach (var genericStr in genericStrings)
					{
						if (!string.IsNullOrEmpty(genericStr.Trim()))
							genericTypes.Add(TypeHelpers.GetType(genericStr, false, true));
					}
				}

				MethodInfo methodInfo = type.GetMethod(methodNameOnly);
				if (methodInfo != null)
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					object classInstance = Activator.CreateInstance(type, null);
					if (parameters.Length == 0)
					{
						if (methodInfo.IsGenericMethod)
							methodInfo = methodInfo.MakeGenericMethod(genericTypes.ToArray());
						result = methodInfo.Invoke(classInstance, null);
					}
					else
					{
						Type[] types = new Type[0];
						object[] paramArray = null;
						if (typesArray.Length == 0)
						{
							paramArray = GetMethodParameters(methodInfo, parametersArray);
						}
						else
						{
							paramArray = MapParamsToTypes(parametersArray, typesArray, assembly, out types);
						}
						if (methodInfo.IsGenericMethod)
							methodInfo = methodInfo.MakeGenericMethod(genericTypes.ToArray());
						result = methodInfo.Invoke(classInstance, paramArray);
					}
				}
			}
			return result;
		}

		public static object ConvertToType(string value, Type type)
		{
			object returnObj;
			if (type.IsValueType && !value.StartsWith(ExtFileFlag))
			{
				returnObj = Convert.ChangeType(value, type);
			}
			else
			{
				returnObj = XmlDeserializeFromString(value.Replace(ExtFileFlag, ""), type);
			}
			return returnObj;
		}

		public static object[] MapParamsToTypes(string[] param, string[] types, Assembly assembly, out Type[] typeArray)
		{
			var typeList = new List<Type>();
			var returnArray = new object[param.Length];
			for (var i = 0; i < param.Length; i++)
			{
				var type = TypeHelpers.GetType(types[i], false, true);
				typeList.Add(type);
				returnArray[i] = ConvertToType(param[i], type);
			}
			typeArray = typeList.ToArray();
			return returnArray;
		}

		public static object[] GetMethodParameters(MethodInfo method, params string[] parametersArray)
		{
			var parameters = method.GetParameters();
			if (parameters.Length != parametersArray.Length)
			{
				throw new ArgumentOutOfRangeException("The number of parameters does not match the method signature.");
			}
			var returnArray = new object[parametersArray.Length];
			for (var i = 0; i < parametersArray.Length; i++)
			{
				returnArray[i] = ConvertToType(parametersArray[i], parameters[i].ParameterType);
			}
			return returnArray;

		}


		public static string[] ParseGroupArray(IEnumerable<string> param, string prefix)
		{
			var paramArray = param.ToArray();
			var newArray = new string[paramArray.Length];
			var regex = new Regex(string.Format(@"({0}|{0}!)\[([\d]+)\]", prefix), RegexOptions.Compiled);
			for (var i = 0; i < paramArray.Length; i++)
			{
				var p = paramArray[i];
				var match = regex.Match(p);
				if (match.Success)
				{
					var type = match.Groups[1].Value;
					var value = p.Split(new [] { '=' }, 2)[1];
					var index = int.Parse(match.Groups[2].Value);

					if (type == string.Format("{0}!", prefix))
					{
						value = ExtFileFlag + File.ReadAllText(Path.Combine(Environment.CurrentDirectory, value));
					}
					
					newArray[index] = value;
				}
			}
			return newArray.Where(x => !string.IsNullOrEmpty(x)).ToArray();
		}

		public static string XmlSerializeToString(object objectInstance)
		{
			using (var writer = new StringWriter())
			{
				using (XmlWriter xmlWriter = new NoNamespaceXmlWriter(writer))
				{
					var ser = new DataContractSerializer(objectInstance.GetType());
					ser.WriteObject(xmlWriter, objectInstance);
					return writer.ToString();
				}
			}
		}

		public static object XmlDeserializeFromString(string objectData, Type type)
		{
			if (type == typeof(string))
				return objectData;
			object result;
			var ser = new DataContractSerializer(type);
			using (var ms = new MemoryStream())
			{ // clone it via DCS
				ser.WriteObject(ms, objectData);
				ms.Position = 0;
				result = ser.ReadObject(ms);
			}
			return result;
		}
	}

	public class NoNamespaceXmlWriter : XmlTextWriter
	{
		//Provide as many contructors as you need
		public NoNamespaceXmlWriter(System.IO.TextWriter output)
			: base(output) { Formatting = System.Xml.Formatting.Indented; }

		public override void WriteStartDocument() { }

		public override void WriteStartElement(string prefix, string localName, string ns)
		{
			base.WriteStartElement("", localName, "");
		}
	}
}

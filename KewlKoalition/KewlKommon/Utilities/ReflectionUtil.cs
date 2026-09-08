using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KewlKommon.Utilities
{
	public static class ReflectionUtil
	{
		public static IEnumerable<Type> GetImplementingTypes(Type implemented)
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(x => x.GetTypes())
				.Where(x => x.IsAssignableTo(implemented))
				.Where(x => !x.IsAbstract)
				.Where(x => !x.ContainsGenericParameters)
				.Distinct();
		}
		public static void InvokeStaticOverrides(Type interfaceType, string methodName)
		{
			var interfaceMethod = interfaceType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (var type in GetImplementingTypes(interfaceType))
				interfaceMethod?.MakeGenericMethod(type).Invoke(null, null);
		}
	}
}
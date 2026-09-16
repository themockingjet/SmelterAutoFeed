using System;
using HarmonyLib;

namespace SmeltAndFuel;

internal static class NativeMethodDelegate
{
    internal static TDelegate CreateParameterless<TDelegate>(Type type, string methodName)
        where TDelegate : Delegate
    {
        return Create<TDelegate>(type, methodName, Type.EmptyTypes);
    }

    internal static TDelegate Create<TDelegate>(Type type, string methodName, Type[] parameterTypes)
        where TDelegate : Delegate
    {
        System.Reflection.MethodInfo? method = AccessTools.Method(type, methodName, parameterTypes);
        if (method is null)
        {
            throw new MissingMethodException(type.FullName, methodName);
        }

        return AccessTools.MethodDelegate<TDelegate>(method);
    }
}

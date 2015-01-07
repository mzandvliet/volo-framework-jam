using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

public static class ReflectionUtils {
    public static IList<MethodInfo> GetMethodsWithAttribute(Type type, Type attributeType, BindingFlags flags) {
        var methods = type.GetMethods(flags);
        var stateMethods = new List<MethodInfo>();
        foreach (var m in methods) {
            var attributes = m.GetCustomAttributes(attributeType, false);
            if (attributes.Length > 0) {
                stateMethods.Add(m);
            }
        }
        return stateMethods;
    }

    /// <summary>
    /// Builds a Delegate instance from the supplied MethodInfo object and a target to invoke against.
    /// </summary>
    public static Delegate ToDelegate(MethodInfo methodInfo, object target) {
        if (methodInfo == null) throw new ArgumentNullException("methodInfo");

        Type delegateType;

        var typeArgs = methodInfo.GetParameters()
            .Select(p => p.ParameterType)
            .ToList();

        // builds a delegate type
        if (methodInfo.ReturnType == typeof(void)) {
            delegateType = Expression.GetActionType(typeArgs.ToArray());
        } else {
            typeArgs.Add(methodInfo.ReturnType);
            delegateType = Expression.GetFuncType(typeArgs.ToArray());
        }

        // creates a binded delegate if target is supplied
        var result = (target == null)
            ? Delegate.CreateDelegate(delegateType, methodInfo)
            : Delegate.CreateDelegate(delegateType, target, methodInfo);

        return result;
    }
}
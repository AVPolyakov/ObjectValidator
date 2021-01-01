using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ObjectValidator
{
    public static class ReflectionUtil
    {
        private static readonly ConcurrentDictionary<MethodInfo, PropertyInfo> _propertyDictionary =
            new();

        private static readonly ConcurrentDictionary<Type, Dictionary<MethodBase, PropertyInfo>> _propertyDictionaryByGetMethod =
            new();

        private static readonly ConcurrentDictionary<MethodInfo, FieldInfo> _fieldDictionary =
            new();

        /// <summary>
        /// Получает <see cref="PropertyInfo"/> для свойства, которое используется в теле метода
        /// <paramref name="func"/>. Например, <c><![CDATA[GetProperyInfo<Message, string>(_ => _.Subject)]]></c>,
        /// возвращает <see cref="PropertyInfo"/> для свойства <c>Subject</c>.
        /// </summary>
        public static PropertyInfo GetProperyInfo<T, TPropery>(Func<T, TPropery> func)
        {
            var methodInfo = func.Method;
            PropertyInfo value;
            if (_propertyDictionary.TryGetValue(methodInfo, out value)) return value;
            var tuples = IlReader.Read(methodInfo).ToList();
            if (!tuples.Select(_ => _.Item1).SequenceEqual(new[] {OpCodes.Ldarg_1, OpCodes.Callvirt, OpCodes.Ret}))
                throw new ArgumentException($"The {nameof(func)} must encapsulate a method with a body that " +
                    "consists of a sequence of intermediate language instructions " +
                    $"{nameof(OpCodes.Ldarg_1)}, {nameof(OpCodes.Callvirt)}, {nameof(OpCodes.Ret)}.", nameof(func));
            return ResolveProperty(methodInfo, tuples[1].Item2.Value);
        }

        /// <summary>
        /// Получает <see cref="MemberInfo"/> для свойства или поля, которое используется 
        /// в теле метода <paramref name="func"/>. Например,
        /// <c><![CDATA[GetMemberInfo<string>(() => MyResources.Subject)]]></c>,
        /// возвращает <see cref="MemberInfo"/> для свойства <c>Subject</c>.
        /// </summary>
        public static MemberInfo GetMemberInfo<TPropery>(Func<TPropery> func)
        {
            var methodInfo = func.Method;
            PropertyInfo value;
            if (_propertyDictionary.TryGetValue(methodInfo, out value)) return value;
            var tuples = IlReader.Read(methodInfo).ToList();
            var codes = tuples.Select(_ => _.Item1).ToList();
            if (codes.SequenceEqual(new[] {OpCodes.Call, OpCodes.Ret}))
                return ResolveProperty(methodInfo, tuples[0].Item2.Value);
            else if (codes.SequenceEqual(new[] {OpCodes.Ldarg_0, OpCodes.Ldfld, OpCodes.Ret}))
                return ResolveField(methodInfo, tuples[1].Item2.Value);
            else
                throw new ArgumentException($"The {nameof(func)} must encapsulate a method with a body that " +
                    "consists of a sequence of intermediate language instructions " +
                    $"{nameof(OpCodes.Call)}, {nameof(OpCodes.Ret)} or " +
                    $"{nameof(OpCodes.Ldarg_0)}, {nameof(OpCodes.Ldfld)}, {nameof(OpCodes.Ret)}.", nameof(func));
        }

        private static PropertyInfo ResolveProperty(MethodInfo methodInfo, int metadataToken)
        {
            var methodBase = methodInfo.Module.ResolveMethod(metadataToken,
                methodInfo.DeclaringType.GetGenericArguments(), null);
            Dictionary<MethodBase, PropertyInfo> infos;
            if (!_propertyDictionaryByGetMethod.TryGetValue(methodBase.DeclaringType, out infos))
            {
                infos = methodBase.DeclaringType.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .ToDictionary(_ => {
                        MethodBase method = _.GetGetMethod(true);
                        return method;
                    });
                _propertyDictionaryByGetMethod.TryAdd(methodBase.DeclaringType, infos);
            }
            var propertyInfo = infos[methodBase];
            _propertyDictionary.TryAdd(methodInfo, propertyInfo);
            return propertyInfo;
        }

        private static MemberInfo ResolveField(MethodInfo methodInfo, int metadataToken)
        {
            var fieldInfo = methodInfo.Module.ResolveField(metadataToken,
                methodInfo.DeclaringType.GetGenericArguments(), null);
            _fieldDictionary.TryAdd(methodInfo, fieldInfo);
            return fieldInfo;
        }
    }
}
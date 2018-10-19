//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reflection;
//using System.Reflection.Emit;
//using System.Text;
//using Common.Log;
//using Microsoft.Extensions.DependencyInjection;

//namespace Lykke.Job.BlockchainCashinDetector.IntegrationTests.Utils
//{
//    public static class TestHelper
//    {
//        public static Type GenerateStartupProxyType(Type typeToWrapUp)
//        {
//            Type baseType = typeToWrapUp;
//            AssemblyName asmName = new AssemblyName(
//                string.Format("{0}_{1}", "tmpAsm", Guid.NewGuid().ToString("N"))
//            );

//            // create in memory assembly only
//            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

//            ModuleBuilder moduleBuilder =
//                asmBuilder.DefineDynamicModule("core");

//            string proxyTypeName = string.Format("{0}_{1}", baseType.Name, Guid.NewGuid().ToString("N"));

//            TypeBuilder typeBuilder =
//                moduleBuilder.DefineType(proxyTypeName, TypeAttributes.Public | TypeAttributes.Sealed);

//            var baseMethod = baseType.GetMethod("RegisterContainer", BindingFlags.Public | BindingFlags.Instance);
//            MethodBuilder mbIM = typeBuilder.DefineMethod("RegisterContainer",
//                MethodAttributes.Public |
//                MethodAttributes.HideBySig |
//                MethodAttributes.Virtual,
//                typeof(ValueTuple<IContainer, ILog>),
//                new Type[] { typeof(IServiceCollection) });

//            var ilGen = mbIM.GetILGenerator();
//            var methodToCall = wrapperType.GetMethod("RegisterContainer", BindingFlags.Instance | BindingFlags.Public);

//            ilGen.Emit(OpCodes.Newobj, wrapperType.GetConstructor(Type.EmptyTypes));
//            ilGen.Emit(OpCodes.Ldarg_1);
//            ilGen.Emit(OpCodes.Call, methodToCall);
//            ilGen.Emit(OpCodes.Ret);

//            typeBuilder.SetParent(baseType);
//            typeBuilder.CreatePassThroughConstructors(baseType);
//            typeBuilder.DefineMethodOverride(mbIM, baseMethod);

//            Type proxy = typeBuilder.CreateType();

//            return proxy;
//        }

//        public static void CreatePassThroughConstructors(this TypeBuilder builder, Type baseType)
//        {
//            foreach (var constructor in baseType.GetConstructors())
//            {
//                var parameters = constructor.GetParameters();
//                if (parameters.Length > 0 && parameters.Last().IsDefined(typeof(ParamArrayAttribute), false))
//                {
//                    //throw new InvalidOperationException("Variadic constructors are not supported");
//                    continue;
//                }

//                var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
//                var requiredCustomModifiers = parameters.Select(p => p.GetRequiredCustomModifiers()).ToArray();
//                var optionalCustomModifiers = parameters.Select(p => p.GetOptionalCustomModifiers()).ToArray();

//                var ctor = builder.DefineConstructor(MethodAttributes.Public, constructor.CallingConvention, parameterTypes, requiredCustomModifiers, optionalCustomModifiers);
//                for (var i = 0; i < parameters.Length; ++i)
//                {
//                    var parameter = parameters[i];
//                    var parameterBuilder = ctor.DefineParameter(i + 1, parameter.Attributes, parameter.Name);
//                    if (((int)parameter.Attributes & (int)ParameterAttributes.HasDefault) != 0)
//                    {
//                        parameterBuilder.SetConstant(parameter.RawDefaultValue);
//                    }

//                    foreach (var attribute in BuildCustomAttributes(parameter.GetCustomAttributesData()))
//                    {
//                        parameterBuilder.SetCustomAttribute(attribute);
//                    }
//                }

//                foreach (var attribute in BuildCustomAttributes(constructor.GetCustomAttributesData()))
//                {
//                    ctor.SetCustomAttribute(attribute);
//                }

//                var emitter = ctor.GetILGenerator();
//                emitter.Emit(OpCodes.Nop);

//                // Load `this` and call base constructor with arguments
//                emitter.Emit(OpCodes.Ldarg_0);
//                for (var i = 1; i <= parameters.Length; ++i)
//                {
//                    emitter.Emit(OpCodes.Ldarg, i);
//                }
//                emitter.Emit(OpCodes.Call, constructor);

//                emitter.Emit(OpCodes.Ret);
//            }
//        }


//        private static CustomAttributeBuilder[] BuildCustomAttributes(IEnumerable<CustomAttributeData> customAttributes)
//        {
//            return customAttributes.Select(attribute =>
//            {
//                var attributeArgs = attribute.ConstructorArguments.Select(a => a.Value).ToArray();
//                var namedPropertyInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<PropertyInfo>().ToArray();
//                var namedPropertyValues = attribute.NamedArguments.Where(a => a.MemberInfo is PropertyInfo).Select(a => a.TypedValue.Value).ToArray();
//                var namedFieldInfos = attribute.NamedArguments.Select(a => a.MemberInfo).OfType<FieldInfo>().ToArray();
//                var namedFieldValues = attribute.NamedArguments.Where(a => a.MemberInfo is FieldInfo).Select(a => a.TypedValue.Value).ToArray();
//                return new CustomAttributeBuilder(attribute.Constructor, attributeArgs, namedPropertyInfos, namedPropertyValues, namedFieldInfos, namedFieldValues);
//            }).ToArray();
//        }
//    }
//}

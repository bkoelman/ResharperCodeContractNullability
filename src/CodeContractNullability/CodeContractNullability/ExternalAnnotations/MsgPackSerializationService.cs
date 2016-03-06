using System;
using System.IO;
using System.Reflection;
using CodeContractNullability.Utilities;
using JetBrains.Annotations;
using MsgPack.Serialization;

namespace CodeContractNullability.ExternalAnnotations
{
    public sealed class MsgPackSerializationService
    {
        static MsgPackSerializationService()
        {
            MsgPackAssemblyLoader.EnsureInitialized();
        }

        [NotNull]
        public T ReadObject<T>([NotNull] Stream source)
        {
            Guard.NotNull(source, nameof(source));

            MessagePackSerializer<T> serializer = CreateSerializer<T>();
            return serializer.Unpack(source);
        }

        public void WriteObject<T>([NotNull] T instance, [NotNull] Stream target)
        {
            Guard.NotNull(instance, nameof(instance));
            Guard.NotNull(target, nameof(target));

            MessagePackSerializer<T> serializer = CreateSerializer<T>();
            serializer.Pack(target, instance);
        }

        [NotNull]
        private static MessagePackSerializer<T> CreateSerializer<T>()
        {
            return SerializationContext.Default.GetSerializer<T>();
        }

        private static class MsgPackAssemblyLoader
        {
            static MsgPackAssemblyLoader()
            {
                Assembly assembly = GetAssemblyFromResource();
                RegisterResolverFor(assembly);
            }

            public static void EnsureInitialized()
            {
                // When getting here, the static constructor must have executed.
            }

            [NotNull]
            private static Assembly GetAssemblyFromResource()
            {
                string thisAssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                string resourceName = thisAssemblyName + ".MsgPack.dll";

                using (Stream imageStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    var assemblyData = new byte[imageStream.Length];
                    imageStream.Read(assemblyData, 0, assemblyData.Length);

                    return Assembly.Load(assemblyData);
                }
            }

            private static void RegisterResolverFor([NotNull] Assembly assembly)
            {
                AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
                {
                    var assemblyName = new AssemblyName(args.Name);
                    return assemblyName.ToString() == assembly.GetName().ToString() ? assembly : null;
                };
            }
        }
    }
}
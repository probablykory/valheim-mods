using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Common
{
    // Much of this code was borrowed and modified from ServerSync: https://github.com/blaxxun-boop/ServerSync
    // Blaxxun et al deserves all the credit for this.
    public class ParsedEntries : Dictionary<string, PackageEntry> { }

    public class PackageEntry
    {
        public string key = null!;
        public Type type = null!;
        public object? value;
    }

    public class InvalidDeserializationTypeException : Exception
    {
        public string expected = null!;
        public string received = null!;
        public string field = "";
    }

    public static class PackageUtils
    {
        public static string GetPackageTypeString(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        public static void AddValue(this ZPackage package, object? value)
        {
            Type? type = value?.GetType();
            if (value is Enum)
            {
                value = ((IConvertible)value).ToType(Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture);
            }
            else if (value is ICollection collection)
            {
                package.Write(collection.Count);
                foreach (object item in collection)
                {
                    AddValue(package, item);
                }
                return;
            }
            else if (type is { IsValueType: true, IsPrimitive: false })
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                package.Write(fields.Length);
                foreach (FieldInfo field in fields)
                {
                    package.Write(GetPackageTypeString(field.FieldType));
                    AddValue(package, field.GetValue(value));
                }
                return;
            }

            ZRpc.Serialize(new[] { value }, ref package);
        }

        public static void AddEntry(this ZPackage package, PackageEntry entry)
        {
            package.Write(entry.key);
            package.Write(entry.value == null ? "" : GetPackageTypeString(entry.type!));
            AddValue(package, entry.value);
        }

        public static ZPackage ToPackage(this PackageEntry packageEntry)
        {
            return ToPackage(new List<PackageEntry>() { packageEntry });
        }

        public static ZPackage ToPackage(this IEnumerable<PackageEntry> packageEntries)
        {
            ZPackage package = new ZPackage();

            package.Write(packageEntries?.Count() ?? 0);
            foreach (PackageEntry packageEntry in packageEntries ?? Array.Empty<PackageEntry>())
            {
                AddEntry(package, packageEntry!);
            }

            return package;
        }

        public static object ReadValueWithType(this ZPackage package, Type type)
        {
            if (type is { IsValueType: true, IsPrimitive: false, IsEnum: false })
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                int fieldCount = package.ReadInt();
                if (fieldCount != fields.Length)
                {
                    throw new InvalidDeserializationTypeException { received = $"(field count: {fieldCount})", expected = $"(field count: {fields.Length})" };
                }

                object value = FormatterServices.GetUninitializedObject(type);
                foreach (FieldInfo field in fields)
                {
                    string typeName = package.ReadString();
                    if (typeName != GetPackageTypeString(field.FieldType))
                    {
                        throw new InvalidDeserializationTypeException { received = typeName, expected = GetPackageTypeString(field.FieldType), field = field.Name };
                    }
                    field.SetValue(value, ReadValueWithType(package, field.FieldType));
                }
                return value;
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                int entriesCount = package.ReadInt();
                IDictionary dict = (IDictionary)Activator.CreateInstance(type);
                Type kvType = typeof(KeyValuePair<,>).MakeGenericType(type.GenericTypeArguments);
                FieldInfo keyField = kvType.GetField("key", BindingFlags.NonPublic | BindingFlags.Instance)!;
                FieldInfo valueField = kvType.GetField("value", BindingFlags.NonPublic | BindingFlags.Instance)!;
                for (int i = 0; i < entriesCount; ++i)
                {
                    object kv = ReadValueWithType(package, kvType);
                    dict.Add(keyField.GetValue(kv), valueField.GetValue(kv));
                }
                return dict;
            }
            if (type != typeof(List<string>) && type.IsGenericType && typeof(ICollection<>).MakeGenericType(type.GenericTypeArguments[0]) is { } collectionType && collectionType.IsAssignableFrom(type))
            {
                int entriesCount = package.ReadInt();
                object list = Activator.CreateInstance(type);
                MethodInfo adder = collectionType.GetMethod("Add")!;
                for (int i = 0; i < entriesCount; ++i)
                {
                    adder.Invoke(list, new[] { ReadValueWithType(package, type.GenericTypeArguments[0]) });
                }
                return list;
            }

            ParameterInfo param = (ParameterInfo)FormatterServices.GetUninitializedObject(typeof(ParameterInfo));
            AccessTools.DeclaredField(typeof(ParameterInfo), "ClassImpl").SetValue(param, type);
            List<object> data = new List<object>();
            ZRpc.Deserialize(new[] { null, param }, package, ref data);
            return data.FirstOrDefault();
        }

        public static ParsedEntries ReadEntries(this ZPackage package)
        {
            ParsedEntries results = new ParsedEntries();

            int valueCount = package.ReadInt();
            for (int i = 0; i < valueCount; ++i)
            {
                string keyName = package.ReadString();
                string typeName = package.ReadString();

                Type? type = Type.GetType(typeName);
                if (typeName == "" || type != null)
                {
                    object? value;
                    try
                    {
                        value = typeName == "" ? null : ReadValueWithType(package, type!);
                    }
                    catch (InvalidDeserializationTypeException e)
                    {
                        Get.Plugin.LogWarning($"Got unexpected struct internal type {e.received} for field {e.field} struct {typeName}");
                        continue;
                    }
                    if (value != null)
                    {
                        results[keyName] = new PackageEntry() { key = keyName, type = type, value = value };
                    }
                }
                else
                {
                    Get.Plugin.LogWarning($"Got invalid type {typeName}, abort reading of received configs");
                    return new ParsedEntries();
                }
            }

            return results;
        }

        public static void Test()
        {
            //List<PackageEntry> entries = new List<PackageEntry>()
            //{
            //    new PackageEntry() {key = "SomeInt", type = typeof(int), value = 123},
            //    new PackageEntry() {key = "SomeBool", type = typeof(bool), value = true},
            //    new PackageEntry() {key = "SomeString", type = typeof(string), value = "YourMommaWearsCombatBoots"},
            //    new PackageEntry() {key = "ListOhStrings", type = typeof(List<string>), value = new List<string>() { "CloseYourEyes", "PinchYoNose", "ShutYoFace" }},
            //};

            //var package = entries.ToPackage();
            //this.LogInfo($" package written {package.Size()}");

            //package.SetPos(0);

            //var results = package.ReadEntries();

            //Get.Plugin.LogInfo($" {results["SomeInt"]}");
            //this.LogInfo($" {results["SomeBool"]}");
            //this.LogInfo($" {results["SomeString"]}");
            //this.LogInfo($" {results["ListOhStrings"]}");
        }
    }
}
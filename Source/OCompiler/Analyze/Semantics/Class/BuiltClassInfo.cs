﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OCompiler.Analyze.Semantics.Class;

internal class BuiltClassInfo : ClassInfo
{
    public override Type Class { get; }
    public List<MethodInfo> Methods { get; }
    public List<FieldInfo> Fields { get; }
    public List<ConstructorInfo> Constructors { get; }
    public static Dictionary<string, ClassInfo> StandardClasses { get; private set; } = new();

    private BuiltClassInfo(Type builtClassType)
    {
        Class = builtClassType;
        Name = builtClassType.Name;
        if (builtClassType.BaseType != null && builtClassType.BaseType != typeof(object))
        {
            BaseClass = GetByType(builtClassType.BaseType);
        }

        Methods = builtClassType.GetRuntimeMethods().ToList();
        Fields = builtClassType.GetRuntimeFields().ToList();
        Constructors = builtClassType.GetConstructors().ToList();
    }

    public static BuiltClassInfo GetByType(Type type)
    {
        if (
            StandardClasses.Values.FirstOrDefault(c => ((BuiltClassInfo)c).Class == type)
            is not BuiltClassInfo existingClassInfo
        )
        {
            return new BuiltClassInfo(type);
        }
        return existingClassInfo;
    }

    static BuiltClassInfo()
    {
        StandardClasses = LoadStandardClasses();
    }

    private static Dictionary<string, ClassInfo> LoadStandardClasses(string @namespace = "OCompiler.Builtins")
    {
        var asm = Assembly.GetExecutingAssembly();
        return new Dictionary<string, ClassInfo>(
            asm.GetTypes().Where(
                type => (type.IsClass || type.IsValueType) &&
                type.Namespace != null &&
                type.Namespace.StartsWith(@namespace)
            ).Select(type => new KeyValuePair<string, ClassInfo>(type.Name, new BuiltClassInfo(type)))
        );
    }

    public override string? GetMethodReturnType(string name, List<string> argumentTypes)
    {
        var method = Methods.FirstOrDefault(
            m => m.Name == name && 
                 m.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(argumentTypes));

        return method?.ReturnType.Name;
    }

    public override ConstructorInfo? GetConstructor(List<string> argumentTypes)
    {
        var constructor = Constructors.FirstOrDefault(
            c => c.GetParameters().Select(
                p => p.ParameterType.Name).SequenceEqual(argumentTypes));

        return constructor;
    }

    public override string? GetFieldType(string name)
    {
        var field = Fields.FirstOrDefault(f => f.Name == name);
        return field?.FieldType.Name;
    }

    public override bool HasField(string name)
    {
        return GetFieldType(name) != null;
    }

    public override bool HasConstructor(List<string> argumentTypes)
    {
        return GetConstructor(argumentTypes) != null;
    }

    public override string ToString(bool includeBase = true)
    {
        StringBuilder @string = new();
        @string.Append("Standard library class ");
        @string.Append(Name);
        if (includeBase && BaseClass != null && BaseClass.Class as Type != typeof(object))
        {
            @string.Append(" extends ");
            @string.Append(BaseClass.Name);
        }
        return @string.ToString();
    }
}

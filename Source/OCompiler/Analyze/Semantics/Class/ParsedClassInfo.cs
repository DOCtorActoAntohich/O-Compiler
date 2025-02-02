﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OCompiler.Analyze.Semantics.Callable;
using OCompiler.Analyze.Syntax.Declaration.Class.Member;
using OCompiler.Analyze.Syntax.Declaration.Class.Member.Method;
using OCompiler.Exceptions.Semantic;

namespace OCompiler.Analyze.Semantics.Class;

internal class ParsedClassInfo : ClassInfo
{
    public override Syntax.Declaration.Class.Class? Class { get; }
    public List<ParsedMethodInfo> Methods { get; } = new();
    public List<ParsedFieldInfo> Fields { get; } = new();
    public List<ParsedConstructorInfo> Constructors { get; } = new();
    public Context Context { get; }

    private static readonly Dictionary<string, ParsedClassInfo> ParsedClasses = new();

    private ParsedClassInfo(Syntax.Declaration.Class.Class parsedClass)
    {
        Context = new Context(this);
        AddMethods(parsedClass.Methods);
        AddFields(parsedClass.Fields);
        AddConstructors(parsedClass.Constructors);
        AddDefaultConstructor();

        Name = parsedClass.Name.Name.Literal;
        Class = parsedClass;
        BaseClass = parsedClass.Extends == null ? GetByName("Class") : GetByName(parsedClass.Extends.Name.Literal);
    }

    protected ParsedClassInfo()
    {
        Context = new Context(this);
    }

    private void AddMethods(List<Method> methods)
    {
        foreach (var method in methods)
        {
            var methodInfo = new ParsedMethodInfo(method, Context);
            var methodName = methodInfo.Name;
            var parameterTypes = methodInfo.GetParameterTypes();
            if (HasMethod(methodName, parameterTypes))
            {
                var argsStr = string.Join(", ", parameterTypes);
                var @return = methodInfo.ReturnType == "Void" ? "" : $"-> {methodInfo.ReturnType}"; 
                throw new NameCollisionError(method.Name.Position, $"Method {methodName}({argsStr}) {@return} defined more than once in class {Name}");
            }
            Methods.Add(methodInfo);
        }
    }

    private void AddFields(List<Field> fields)
    {
        foreach (var field in fields)
        {
            var fieldInfo = new ParsedFieldInfo(field, Context);
            if (Fields.Any(f => f.Name == fieldInfo.Name))
            {
                throw new NameCollisionError(field.Identifier.Position, $"Field {fieldInfo.Name} defined more than once in class {Name}");
            }
            Fields.Add(fieldInfo);
        }
    }

    private void AddConstructors(List<Constructor> constructors)
    {
        foreach (var constructor in constructors)
        {
            var constructorInfo = new ParsedConstructorInfo(constructor, Context);
            var parameterTypes = constructorInfo.GetParameterTypes();
            if (HasConstructor(parameterTypes))
            {
                var argsStr = string.Join(", ", parameterTypes);
                throw new NameCollisionError(constructor.Position, $"Constructor {Name}({argsStr}) defined more than once in class {Name}");
            }
            Constructors.Add(constructorInfo);
        }
    }

    private void AddDefaultConstructor()
    {
        if (HasConstructor(new()))
        {
            return;
        }
        Constructors.Add(new ParsedConstructorInfo(Constructor.EmptyConstructor, Context));
    }

    public static ParsedClassInfo GetByClass(Syntax.Declaration.Class.Class parsedClass)
    {
        var name = parsedClass.Name.Name.Literal;
        if (ParsedClasses.TryGetValue(name, out var classInfo) && classInfo is not EmptyParsedClassInfo)
        {
            return classInfo;
        }

        var newInfo = new ParsedClassInfo(parsedClass);
        foreach (var derivedClass in ParsedClasses.Values.Where(
            c => c.BaseClass != null && c.BaseClass.Name == name
        ))
        {
            derivedClass.BaseClass = newInfo;
        }
        ParsedClasses[name] = newInfo;
        return newInfo;
    }

    public static ClassInfo GetByName(string name)
    {
        if (ParsedClasses.ContainsKey(name))
        {
            return ParsedClasses[name];
        }
        if (BuiltClassInfo.StandardClasses.ContainsKey(name))
        {
            return BuiltClassInfo.StandardClasses[name];
        }

        var newClassInfo = new EmptyParsedClassInfo(name);
        ParsedClasses.Add(name, newClassInfo);
        return newClassInfo;
    }

    public override string? GetMethodReturnType(string name, List<string> argumentTypes)
    {
        var type = Methods.FirstOrDefault(
            m => m.Name == name &&
                 m.Parameters.Select(p => p.Type).SequenceEqual(argumentTypes))?.ReturnType;

        if (type == null && BaseClass != null)
        {
            type = BaseClass.GetMethodReturnType(name, argumentTypes);
        }

        return type;
    }

    public override ParsedConstructorInfo? GetConstructor(List<string> argumentTypes)
    {
        var constructor = Constructors.FirstOrDefault(
            c => c.Parameters.Select(
                p => p.Type).SequenceEqual(argumentTypes));

        return constructor;
    }

    public bool HasMethod(string name, List<string> argumentTypes)
    {
        return GetMethodReturnType(name, argumentTypes) != null;
    }


    public ParsedFieldInfo? GetFieldInfo(string name)
    {
        var field = Fields.FirstOrDefault(f => f.Name == name);
        if (field == null && BaseClass is ParsedClassInfo parsedBaseClass) {
            field = parsedBaseClass.GetFieldInfo(name);
        }
        return field;
    }

    public override string? GetFieldType(string name)
    {
        var type = GetFieldInfo(name)?.Type;
        if (type == null && BaseClass != null)
        {
            type = BaseClass.GetFieldType(name);
        }
        return type;
    }

    public override bool HasField(string name)
    {
        return GetFieldType(name) != null;
    }

    public void AddFieldType(string name, string type)
    {
        var field = Fields.FirstOrDefault(f => f.Name == name);
        if (field != null && field.Type == null)
        {
            field.Expression.ValidateExpression();
        }
    }

    public override bool HasConstructor(List<string> argumentTypes)
    {
        return GetConstructor(argumentTypes) != null;
    }

    public override string ToString(bool includeBase = true)
    {
        StringBuilder @string = new();
        @string.Append("Parsed class ");
        @string.Append(Name);
        if (includeBase && BaseClass != null)
        {
            @string.Append(" extends ");
            @string.Append(BaseClass.Name);
        }
        return @string.ToString();
    }
}

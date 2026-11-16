using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Acha os campos que o Inspector mostra e que não têm <see cref="TooltipAttribute"/> (regra 8 do CLAUDE.md).
    /// Só vale para o que o projeto escreve: um tipo <c>[Serializable]</c> que não é do assembly de quem o declara
    /// nem de um assembly <c>ProjetoVN.*</c> (<c>Vector3</c>, <c>UnityEvent</c>) não é percorrido. O campo herdado
    /// de um componente ou <c>ScriptableObject</c> é cobrado do tipo que o declara; já a base de uma classe
    /// <c>[Serializable]</c> aninhada é percorrida junto com ela, enquanto for do projeto.
    /// </summary>
    public static class ExposedFields
    {
        private const string ProjectAssemblyPrefix = "ProjetoVN.";

        private const BindingFlags DeclaredInstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static bool IsProjectAssembly(Assembly assembly) =>
            assembly.GetName().Name.StartsWith(ProjectAssemblyPrefix, StringComparison.Ordinal);

        public static IReadOnlyList<string> WithoutTooltip(Type type)
        {
            var withoutTooltip = new List<string>();
            Collect(type, new HashSet<Type>(), withoutTooltip);
            return withoutTooltip;
        }

        private static void Collect(Type type, HashSet<Type> visited, List<string> withoutTooltip)
        {
            if (!visited.Add(type)) return;

            foreach (FieldInfo field in type.GetFields(DeclaredInstanceFields).Where(IsShownByInspector))
            {
                if (!field.IsDefined(typeof(TooltipAttribute)))
                    withoutTooltip.Add($"{type.Name}.{field.Name}");

                Type nested = SerializableTypeDeclaredNextTo(field);
                if (nested != null) CollectWithProjectBases(nested, visited, withoutTooltip);
            }
        }

        private static void CollectWithProjectBases(Type nested, HashSet<Type> visited, List<string> withoutTooltip)
        {
            for (Type type = nested; type != null && IsWrittenByProject(type, nested); type = type.BaseType)
                Collect(type, visited, withoutTooltip);
        }

        private static bool IsShownByInspector(FieldInfo field)
        {
            if (field.IsInitOnly) return false;
            if (field.IsDefined(typeof(NonSerializedAttribute))) return false;
            if (field.IsDefined(typeof(HideInInspector))) return false;

            return field.IsPublic || field.IsDefined(typeof(SerializeField));
        }

        private static Type SerializableTypeDeclaredNextTo(FieldInfo field)
        {
            Type type = ElementTypeOf(field.FieldType);

            if (!IsWrittenByProject(type, field.DeclaringType)) return null;
            if (type.IsEnum || !type.IsSerializable) return null;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return null;

            return type;
        }

        private static bool IsWrittenByProject(Type type, Type nextTo) =>
            type.Assembly == nextTo.Assembly || IsProjectAssembly(type.Assembly);

        private static Type ElementTypeOf(Type fieldType)
        {
            if (fieldType.IsArray) return fieldType.GetElementType();

            bool isList = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);
            return isList ? fieldType.GetGenericArguments()[0] : fieldType;
        }
    }
}

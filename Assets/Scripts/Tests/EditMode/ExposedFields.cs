using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Acha os campos que o Inspector mostra e que não têm <see cref="TooltipAttribute"/> (regra 8 do CLAUDE.md).
    /// Só vale para o que o projeto escreve: um tipo <c>[Serializable]</c> de outro assembly (<c>Vector3</c>,
    /// <c>UnityEvent</c>) não é percorrido, e um campo herdado é cobrado do tipo que o declara.
    /// </summary>
    public static class ExposedFields
    {
        private const BindingFlags DeclaredInstanceFields =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

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
                if (nested != null) Collect(nested, visited, withoutTooltip);
            }
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

            if (type.Assembly != field.DeclaringType.Assembly) return null;
            if (type.IsEnum || !type.IsSerializable) return null;
            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return null;

            return type;
        }

        private static Type ElementTypeOf(Type fieldType)
        {
            if (fieldType.IsArray) return fieldType.GetElementType();

            bool isList = fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>);
            return isList ? fieldType.GetGenericArguments()[0] : fieldType;
        }
    }
}

using System;
using UnityEngine;

namespace ProjetoVN.GameFlow.DevTools
{
    public enum PlaceholderRole
    {
        LockedDoor,
        Exit,
        Item,
        Conversation,
        Object
    }

    /// <summary>
    /// A regra do rótulo provisório: que tipo de objeto é, como o tipo se chama e como se pinta, e o texto do rótulo.
    /// Sem estado nem componente: o <see cref="PlaceholderLabel"/> só lê os componentes do objeto e aplica o
    /// resultado (D-03). O verde não é de nenhum tipo: é o do <see cref="PlaceholderTint"/>, que quer dizer
    /// "porta aberta".
    /// </summary>
    public static class PlaceholderRoles
    {
        public const string MissingNameMarker = "(sem nome)";

        public static PlaceholderRole Pick(bool hasLockedAction, bool hasExit, bool isCollectable, bool startsConversation)
        {
            if (hasLockedAction) return PlaceholderRole.LockedDoor;
            if (hasExit) return PlaceholderRole.Exit;
            if (isCollectable) return PlaceholderRole.Item;
            if (startsConversation) return PlaceholderRole.Conversation;

            return PlaceholderRole.Object;
        }

        public static string TitleOf(PlaceholderRole role) => role switch
        {
            PlaceholderRole.LockedDoor => "PORTA TRANCADA",
            PlaceholderRole.Exit => "SAÍDA",
            PlaceholderRole.Item => "ITEM",
            PlaceholderRole.Conversation => "CONVERSA",
            PlaceholderRole.Object => "OBJETO",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

        public static Color ColorOf(PlaceholderRole role) => role switch
        {
            PlaceholderRole.LockedDoor => new Color32(0xE0, 0x4A, 0x4A, 0xFF),
            PlaceholderRole.Exit => new Color32(0x9B, 0x51, 0xE0, 0xFF),
            PlaceholderRole.Item => new Color32(0xF2, 0xC2, 0x30, 0xFF),
            PlaceholderRole.Conversation => new Color32(0x2F, 0x80, 0xED, 0xFF),
            PlaceholderRole.Object => new Color32(0x9E, 0x9E, 0x9E, 0xFF),
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

        public static bool IsNameMissing(string name) => string.IsNullOrWhiteSpace(name);

        public static string LabelText(PlaceholderRole role, string name)
        {
            string visibleName = IsNameMissing(name) ? MissingNameMarker : name.Trim();

            return $"<size=75%>{TitleOf(role)}</size>\n{visibleName}";
        }
    }
}

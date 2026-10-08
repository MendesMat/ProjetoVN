using System.Collections.Generic;
using System.Linq;
using ProjetoVN.Dialogue.Logic;
using Yarn.Compiler;
using Yarn.Markup;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>Uma linha do roteiro: onde está, quem fala (<c>null</c> em narração e opção) e as etiquetas de expressão.</summary>
    public readonly struct ScriptSpeakerLine
    {
        public string NodeName { get; }
        public int LineNumber { get; }
        public string Character { get; }
        public string[] Expressions { get; }

        public ScriptSpeakerLine(string nodeName, int lineNumber, string character, string[] expressions)
        {
            NodeName = nodeName;
            LineNumber = lineNumber;
            Character = character;
            Expressions = expressions;
        }

        public override string ToString() => $"{NodeName}, linha {LineNumber}";
    }

    /// <summary>
    /// Quem fala em cada linha de um roteiro compilado. Lê o personagem com o mesmo <c>LineParser</c> do jogo,
    /// então o escape <c>\:</c> e o nome vindo de expressão (<c>{0}</c>) se comportam como em Play Mode.
    /// </summary>
    public sealed class ScriptSpeakers
    {
        private const string CharacterAttribute = "character";
        private const string CharacterNameProperty = "name";
        private const string Language = "pt-BR";

        public IReadOnlyList<ScriptSpeakerLine> Lines { get; }

        /// <summary>As linhas com personagem.</summary>
        public IEnumerable<ScriptSpeakerLine> Spoken => Lines.Where(line => line.Character != null);

        /// <summary>Etiquetas de expressão em narração ou opção: não há retrato para trocar.</summary>
        public IEnumerable<ScriptSpeakerLine> TaggedWithoutSpeaker =>
            Lines.Where(line => line.Character == null && line.Expressions.Length > 0);

        private ScriptSpeakers(IReadOnlyList<ScriptSpeakerLine> lines) => Lines = lines;

        public static ScriptSpeakers In(IEnumerable<StringInfo> stringInfos)
        {
            var parser = new LineParser();
            return new ScriptSpeakers(stringInfos.Select(info => ReadLine(parser, info)).ToList());
        }

        private static ScriptSpeakerLine ReadLine(LineParser parser, StringInfo info) =>
            new(info.nodeName, info.lineNumber, CharacterIn(parser, info.text), ExpressionTag.AllIn(info.metadata).ToArray());

        private static string CharacterIn(LineParser parser, string text)
        {
            MarkupParseResult parsed = parser.ParseString(text, Language);
            if (!parsed.TryGetAttributeWithName(CharacterAttribute, out MarkupAttribute attribute)) return null;

            return attribute.Properties.TryGetValue(CharacterNameProperty, out MarkupValue name) ? name.StringValue : null;
        }
    }
}

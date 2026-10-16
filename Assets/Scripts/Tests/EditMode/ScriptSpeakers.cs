using System.Collections.Generic;
using System.Linq;
using ProjetoVN.Dialogue.Logic;
using Yarn;
using Yarn.Compiler;
using Yarn.Markup;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Uma linha do roteiro: onde está, quem fala (<c>null</c> em narração e opção), as etiquetas de expressão e
    /// se é uma opção e uma opção de ação.
    /// </summary>
    public readonly struct ScriptSpeakerLine
    {
        public string NodeName { get; }
        public int LineNumber { get; }
        public string Character { get; }
        public string[] Expressions { get; }
        public bool IsOption { get; }
        public bool IsAction { get; }

        public ScriptSpeakerLine(string nodeName, int lineNumber, string character, string[] expressions, bool isOption, bool isAction)
        {
            NodeName = nodeName;
            LineNumber = lineNumber;
            Character = character;
            Expressions = expressions;
            IsOption = isOption;
            IsAction = isAction;
        }

        public bool IsThought => ThoughtLine.Is(Character);

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

        /// <summary>As linhas com personagem. O pensamento (<c>Pensamento:</c>) não é personagem.</summary>
        public IEnumerable<ScriptSpeakerLine> Spoken => Lines.Where(line => line.Character != null && !line.IsThought);

        /// <summary>Etiquetas de expressão em narração, pensamento ou opção: não há retrato para trocar.</summary>
        public IEnumerable<ScriptSpeakerLine> TaggedWithoutSpeaker =>
            Lines.Where(line => (line.Character == null || line.IsThought) && line.Expressions.Length > 0);

        /// <summary>A etiqueta <c>#acao</c> em fala, narração ou pensamento: ela só vale em opção.</summary>
        public IEnumerable<ScriptSpeakerLine> ActionOutsideOptions => Lines.Where(line => line.IsAction && !line.IsOption);

        private ScriptSpeakers(IReadOnlyList<ScriptSpeakerLine> lines) => Lines = lines;

        public static ScriptSpeakers In(IDictionary<string, StringInfo> stringTable, Program program)
        {
            var parser = new LineParser();
            HashSet<string> optionIds = OptionIdsIn(program);
            return new ScriptSpeakers(stringTable
                .Select(entry => ReadLine(parser, entry.Value, optionIds.Contains(entry.Key)))
                .ToList());
        }

        private static HashSet<string> OptionIdsIn(Program program) =>
            program.Nodes.Values
                .SelectMany(node => node.Instructions)
                .Where(instruction => instruction.InstructionTypeCase == Instruction.InstructionTypeOneofCase.AddOption)
                .Select(instruction => instruction.AddOption.LineID)
                .ToHashSet();

        private static ScriptSpeakerLine ReadLine(LineParser parser, StringInfo info, bool isOption) =>
            new(info.nodeName, info.lineNumber, CharacterIn(parser, info.text), ExpressionTag.AllIn(info.metadata).ToArray(),
                isOption, ActionOption.IsMarkedIn(info.metadata));

        private static string CharacterIn(LineParser parser, string text)
        {
            MarkupParseResult parsed = parser.ParseString(text, Language);
            if (!parsed.TryGetAttributeWithName(CharacterAttribute, out MarkupAttribute attribute)) return null;

            return attribute.Properties.TryGetValue(CharacterNameProperty, out MarkupValue name) ? name.StringValue : null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjetoVN.Dialogue.Logic;
using Yarn;
using Yarn.Compiler;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Compila um roteiro (um texto ou os arquivos de <c>Assets/Roteiro/</c>) e o executa em um
    /// <c>Yarn.Dialogue</c> ligado ao <see cref="StoryStateVariables"/>, guardando o que o jogador veria.
    /// Escrito <c>Yarn.Dialogue</c> por extenso: dentro de <c>ProjetoVN.*</c>, <c>Dialogue</c> é o namespace.
    /// </summary>
    public sealed class ScriptRun
    {
        private const string ScriptFolder = "Assets/Roteiro";

        private readonly Yarn.Dialogue _dialogue;
        private readonly CompilationResult _compilation;
        private bool _shouldContinue;
        private OptionSet? _pendingOptions;

        public StoryStateVariables Variables { get; } = new();
        public List<string> Lines { get; } = new();
        public List<string> Commands { get; } = new();
        public List<string> OptionTexts { get; } = new();

        /// <summary>Os ids que <c>tem_item</c> considera no inventário do jogador.</summary>
        public HashSet<string> OwnedItems { get; } = new();
        public bool Completed { get; private set; }

        public IEnumerable<Diagnostic> ErrorsAndWarnings => _compilation.Diagnostics.Where(d =>
            d.Severity == Diagnostic.DiagnosticSeverity.Error || d.Severity == Diagnostic.DiagnosticSeverity.Warning);

        /// <summary><c>null</c> quando o roteiro tem erro de compilação.</summary>
        public Program Program => _compilation.Program;

        public IEnumerable<string> NodeNames => _compilation.Program == null
            ? Enumerable.Empty<string>()
            : _compilation.Program.Nodes.Keys;

        private ScriptRun(Func<Library, CompilationJob> createJob)
        {
            _dialogue = new Yarn.Dialogue(Variables);
            _dialogue.Library.RegisterFunction("tem_item", (string itemId) => OwnedItems.Contains(itemId));
            _compilation = Compiler.Compile(createJob(_dialogue.Library));
            if (_compilation.Program == null) return;

            _dialogue.SetProgram(_compilation.Program);
            _dialogue.LineHandler = DeliverLine;
            _dialogue.CommandHandler = command => { Commands.Add(command.Text); _shouldContinue = true; };
            _dialogue.OptionsHandler = ShowOptions;
            _dialogue.DialogueCompleteHandler = () => Completed = true;
        }

        public static ScriptRun FromText(string source) =>
            new(library => CompilationJob.CreateFromString("teste.yarn", source, library));

        public static ScriptRun FromProjectFiles() =>
            new(library => CompilationJob.CreateFromFiles(
                Directory.GetFiles(ScriptFolder, "*.yarn", SearchOption.AllDirectories), library));

        public string[] TagsOfLine(string lineText)
        {
            foreach (KeyValuePair<string, StringInfo> entry in _compilation.StringTable)
            {
                if (_compilation.GetStringForKey(entry.Key) == lineText) return entry.Value.metadata;
            }

            return Array.Empty<string>();
        }

        public void Start(string nodeName)
        {
            _dialogue.SetNode(nodeName);
            RunUntilWaiting();
        }

        public void Choose(int index)
        {
            _dialogue.SetSelectedOption(_pendingOptions.Value.Options[index].ID);
            _pendingOptions = null;
            OptionTexts.Clear();
            RunUntilWaiting();
        }

        private void DeliverLine(Line line)
        {
            Lines.Add(WithSubstitutions(_compilation.GetStringForKey(line.ID), line.Substitutions));
            _shouldContinue = true;
        }

        private static string WithSubstitutions(string text, string[] substitutions)
        {
            for (int i = 0; i < substitutions.Length; i++)
                text = text.Replace("{" + i + "}", substitutions[i]);

            return text;
        }

        private void ShowOptions(OptionSet options)
        {
            _pendingOptions = options;
            OptionTexts.AddRange(options.Options.Select(option => _compilation.GetStringForKey(option.Line.ID)));
        }

        private void RunUntilWaiting()
        {
            do
            {
                _shouldContinue = false;
                _dialogue.Continue();
            }
            while (_shouldContinue && !Completed);
        }
    }
}

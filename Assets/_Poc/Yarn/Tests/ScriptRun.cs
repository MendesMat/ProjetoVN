using System.Collections.Generic;
using System.Linq;
using Yarn;
using Yarn.Compiler;

namespace ProjetoVN.PocYarn.Tests
{
    public sealed class ScriptRun
    {
        public const string OwnedItemId = "item-teste-01";

        private readonly Dialogue _dialogue;
        private readonly CompilationResult _compilation;
        private bool _shouldContinue;
        private OptionSet? _pendingOptions;

        public StoryVariables Variables { get; } = new();
        public HashSet<string> OwnedItems { get; } = new();
        public List<string> Lines { get; } = new();
        public List<string> Commands { get; } = new();
        public List<string> OptionTexts { get; } = new();
        public List<bool> OptionAvailability { get; } = new();
        public List<string[]> LineSubstitutions { get; } = new();
        public bool Completed { get; private set; }

        public Program Program => _compilation.Program;
        public Dialogue Dialogue => _dialogue;
        public IEnumerable<Diagnostic> Errors
            => _compilation.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

        public ScriptRun(string source)
        {
            _dialogue = new Dialogue(Variables);
            _dialogue.Library.RegisterFunction("tem_item", (string itemId) => OwnedItems.Contains(itemId));

            CompilationJob job = CompilationJob.CreateFromString("teste.yarn", source, _dialogue.Library);
            _compilation = Compiler.Compile(job);
            if (_compilation.Program == null) return;

            _dialogue.SetProgram(_compilation.Program);
            _dialogue.LineHandler = line => Deliver(Lines, line);
            _dialogue.CommandHandler = command => { Commands.Add(command.Text); _shouldContinue = true; };
            _dialogue.OptionsHandler = options => ShowOptions(options);
            _dialogue.DialogueCompleteHandler = () => Completed = true;
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
            OptionAvailability.Clear();
            RunUntilWaiting();
        }

        private void Deliver(List<string> destination, Line line)
        {
            destination.Add(_compilation.GetStringForKey(line.ID));
            LineSubstitutions.Add(line.Substitutions);
            _shouldContinue = true;
        }

        private void ShowOptions(OptionSet options)
        {
            _pendingOptions = options;
            OptionTexts.AddRange(options.Options.Select(option => _compilation.GetStringForKey(option.Line.ID)));
            OptionAvailability.AddRange(options.Options.Select(option => option.IsAvailable));
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

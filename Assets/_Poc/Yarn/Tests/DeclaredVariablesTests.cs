using System.Linq;
using Google.Protobuf;
using NUnit.Framework;
using ProjetoVN.PocYarn.Editor;
using UnityEditor;
using UnityEngine;
using Yarn.Compiler;
using Yarn.Unity;

namespace ProjetoVN.PocYarn.Tests
{
    public sealed class DeclaredVariablesTests
    {
        private const string ProjectPath = "Assets/_Poc/Yarn/Roteiro/Poc.yarnproject";

        [Test]
        public void NamesOf_WithoutAProject_ReturnsAnEmptyList()
        {
            Assert.IsEmpty(DeclaredVariables.NamesOf(null));
        }

        [Test]
        public void NamesOf_AfterTheProjectIsReimportedWithARenamedVariable_ListsTheNewName()
        {
            var project = ScriptableObject.CreateInstance<YarnProject>();
            project.compiledYarnProgram = CompiledWithVariable("$antes");
            Assert.AreEqual(new[] { "$antes" }, DeclaredVariables.NamesOf(project).ToArray());

            project.compiledYarnProgram = CompiledWithVariable("$depois");

            CollectionAssert.AreEqual(new[] { "$depois" }, DeclaredVariables.NamesOf(project),
                "o YarnProject guarda cache de Program e InitialValues; a lista precisa seguir o que a importação gravou");
            Object.DestroyImmediate(project);
        }

        [Test]
        public void NamesOf_WithTheImportedPocProject_ListsEveryDeclaredVariableSorted()
        {
            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(ProjectPath);

            CollectionAssert.AreEqual(
                new[] { "$afinidade_gotica", "$conheceu_gotica" },
                DeclaredVariables.NamesOf(project));
        }

        private static byte[] CompiledWithVariable(string variableName)
        {
            string source = $"title: Teste\n---\n<<declare {variableName} = 0>>\nOi.\n===\n";
            CompilationResult result = Compiler.Compile(CompilationJob.CreateFromString("teste.yarn", source));
            return result.Program.ToByteArray();
        }
    }
}

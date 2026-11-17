using NUnit.Framework;
using ProjetoVN.Core.State;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>A Cena 2 do prólogo (o pátio e a loja do posto), nos arquivos reais de <c>Assets/Roteiro/</c>.</summary>
    public sealed class PrologueEpisode2ScriptTests
    {
        private const string TalkedToTheDriver = "$falou_com_motorista";
        private const string Attendant = "loja_atendente";
        private const string Bathroom = "loja_banheiro";

        private const string BuySnackOption = "Comprar um salgadinho";
        private const int BuySnackIndex = 0;
        private const int BuyNothingIndex = 3;

        private const string AttendantGreeting = "Atendente: Boa noite, quer o quê?";
        private const string AttendantAccidentQuestion = "Atendente: Vem cá, o que tá acontecendo lá fora?";

        [SetUp]
        public void SetUp() => StoryState.ClearAll();

        [TearDown]
        public void TearDown() => StoryState.ClearAll();

        [Test]
        public void LojaBanheiro_EachTry_ShowsTheNextLine_AndRepeatsTheLast()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            string[][] expectedPerTry =
            {
                new[] { "???: Tem gente...", "Protagonista: Ah, desculpa." },
                new[] { "???: Pô, cara, já falei que tem gente!" },
                new[] { "???: Quando eu acabar aqui, vou te pegar!" },
                new[] { "???: Coé, cara... Faz favor aí, me deixa em paz..." },
                new[] { "???: ..." },
                new[] { "???: ..." },
            };

            foreach (string[] expectedLines in expectedPerTry)
            {
                run.Lines.Clear();
                run.Start(Bathroom);

                CollectionAssert.AreEqual(expectedLines, run.Lines);
            }
        }

        [Test]
        public void LojaBanheiro_EveryTry_PlaysTheLockedDoor()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            for (int attempt = 0; attempt < 3; attempt++)
            {
                run.Commands.Clear();
                run.Start(Bathroom);

                Assert.AreEqual("tocar_efeito porta_trancada", run.Commands[0]);
            }
        }

        [Test]
        public void PostoMotorista_TurnsOnTheWalkToTown()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            run.Start("posto_motorista");

            Assert.IsTrue(StoryState.IsTrue(TalkedToTheDriver));
            Assert.IsTrue(run.Completed);
        }

        [Test]
        public void LojaAtendente_BeforeTheDriver_TellsItWasAnAccident()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start(Attendant);

            run.Choose(BuyNothingIndex);

            CollectionAssert.AreEqual(new[]
            {
                AttendantGreeting,
                AttendantAccidentQuestion,
                "Protagonista: Um acidente que teve. Ninguém se machucou.",
                "Atendente: Menos mal.",
            }, run.Lines);
        }

        [Test]
        public void LojaAtendente_AfterTheDriver_TellsAboutTheTapir()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start("posto_motorista");
            run.Lines.Clear();
            run.Start(Attendant);

            run.Choose(BuyNothingIndex);

            CollectionAssert.AreEqual(new[]
            {
                AttendantGreeting,
                AttendantAccidentQuestion,
                "Protagonista: O cara atropelou uma anta.",
                "Atendente: Caraca... Que papelão, hein...",
            }, run.Lines);
        }

        [Test]
        public void LojaAtendente_OffersTheThreeItemsAndLeaving()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();

            run.Start(Attendant);

            CollectionAssert.AreEqual(
                new[] { BuySnackOption, "Comprar um refrigerante", "Comprar um chocolate", "Não comprar nada" },
                run.OptionTexts);
        }

        [Test]
        public void LojaAtendente_BuyingAnItem_GivesIt()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start(Attendant);

            run.Choose(BuySnackIndex);

            CollectionAssert.Contains(run.Commands, "dar_item salgadinho");
        }

        [Test]
        public void LojaAtendente_OwnedItem_LocksItsOption()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.OwnedItems.Add("salgadinho");

            run.Start(Attendant);

            CollectionAssert.AreEqual(new[] { BuySnackOption }, run.UnavailableOptionTexts);
        }

        [Test]
        public void LojaAtendente_SecondTalk_SkipsTheAccidentQuestion()
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start(Attendant);
            run.Choose(BuyNothingIndex);
            run.Lines.Clear();

            run.Start(Attendant);
            run.Choose(BuyNothingIndex);

            CollectionAssert.AreEqual(new[] { AttendantGreeting }, run.Lines);
            Assert.IsTrue(run.Completed);
        }

        [TestCase("posto_chegada")]
        [TestCase("loja_chegada")]
        public void ArrivalNodes_SecondArrival_ShowsNothing(string arrivalNode)
        {
            ScriptRun run = ScriptRun.FromProjectFiles();
            run.Start(arrivalNode);
            run.Lines.Clear();
            run.Commands.Clear();

            run.Start(arrivalNode);

            CollectionAssert.IsEmpty(run.Lines);
            CollectionAssert.IsEmpty(run.Commands);
            Assert.IsTrue(run.Completed);
        }
    }
}

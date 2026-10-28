using System.Linq;
using NUnit.Framework;
using ProjetoVN.UI.Components;
using UnityEngine.UI;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ButtonSoundsTests
    {
        [Test]
        public void EveryButton_HasButtonSounds()
        {
            var readings = ProjectComponents.ReadAll<Button, bool>(button => button.GetComponent<ButtonSounds>() != null);

            Assert.IsNotEmpty(readings, "o teste não achou nenhum Button em Assets/");
            Assert.IsEmpty(readings.Where(reading => !reading.Value).Select(reading => reading.Location),
                "todo Button do jogo leva o componente ButtonSounds (som de passar o mouse e de clique)");
        }
    }
}

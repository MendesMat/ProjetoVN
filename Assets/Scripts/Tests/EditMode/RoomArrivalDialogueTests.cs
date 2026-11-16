using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.GameFlow.Rooms;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class RoomArrivalDialogueTests
    {
        [Test]
        public void Scenes_HaveAtMostOneRoomArrivalDialogue()
        {
            IEnumerable<string> crowded = ProjectComponents
                .ReadAll<RoomArrivalDialogue, string>(arrival => arrival.gameObject.scene.path)
                .Where(reading => !string.IsNullOrEmpty(reading.Value))
                .GroupBy(reading => reading.Value)
                .Where(scene => scene.Count() > 1)
                .Select(scene => $"{scene.Key}: {string.Join("; ", scene.Select(reading => reading.Location))}");

            CollectionAssert.IsEmpty(crowded,
                "cenas com mais de um RoomArrivalDialogue: a sala abre uma conversa de chegada só");
        }
    }
}

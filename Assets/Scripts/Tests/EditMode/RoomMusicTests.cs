using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.Audio;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class RoomMusicTests
    {
        [Test]
        public void Scenes_HaveAtMostOneRoomMusic()
        {
            IEnumerable<string> crowded = ProjectComponents
                .ReadAll<RoomMusic, string>(roomMusic => roomMusic.gameObject.scene.path)
                .Where(reading => !string.IsNullOrEmpty(reading.Value))
                .GroupBy(reading => reading.Value)
                .Where(scene => scene.Count() > 1)
                .Select(scene => $"{scene.Key}: {string.Join("; ", scene.Select(reading => reading.Location))}");

            CollectionAssert.IsEmpty(crowded, "cenas com mais de um RoomMusic: a sala declara uma música só");
        }
    }
}

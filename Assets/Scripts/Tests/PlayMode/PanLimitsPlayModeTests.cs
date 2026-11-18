using System.Collections;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.PointNClick;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjetoVN.Tests.PlayMode
{
    public class PanLimitsPlayModeTests
    {
        private const string Room = "[Teste] Mecanicas";
        private const string BackgroundName = "Backgrounds";
        private const float FarAway = 1000f;
        private const float Tolerance = 0.01f;
        private const float TimeoutSeconds = 10f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled);

            yield return SceneManager.LoadSceneAsync(Room);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return WaitUntil(() => PlayerInputGate.IsEnabled);
        }

        [UnityTest]
        public IEnumerator CenterOnFarToTheRight_StopsWhereTheImageEdgeMeetsTheScreenEdge()
        {
            ScreenPanController pan = FindPan();

            pan.CenterOn(FarAway);

            Assert.That(BackgroundBounds(pan).max.x, Is.EqualTo(RightEdgeOfScreen()).Within(Tolerance));
            yield break;
        }

        [UnityTest]
        public IEnumerator CenterOnFarToTheLeft_StopsWhereTheImageEdgeMeetsTheScreenEdge()
        {
            ScreenPanController pan = FindPan();

            pan.CenterOn(-FarAway);

            Assert.That(BackgroundBounds(pan).min.x, Is.EqualTo(LeftEdgeOfScreen()).Within(Tolerance));
            yield break;
        }

        [UnityTest]
        public IEnumerator ContainerPushedPastTheLimit_ReturnsInsideOnTheNextFrame()
        {
            ScreenPanController pan = FindPan();
            pan.transform.position = new Vector3(FarAway, 0f, 0f);

            yield return null;

            Assert.That(BackgroundBounds(pan).min.x, Is.LessThanOrEqualTo(LeftEdgeOfScreen() + Tolerance));
        }

        private static ScreenPanController FindPan()
        {
            ScreenPanController pan = Object.FindFirstObjectByType<ScreenPanController>();
            Assert.IsNotNull(pan, $"A sala '{Room}' não tem ScreenPanController.");
            return pan;
        }

        private static Bounds BackgroundBounds(ScreenPanController pan)
        {
            Transform background = pan.transform.Find(BackgroundName);
            Assert.IsNotNull(background, $"O ScreenPanController da sala '{Room}' não tem o filho '{BackgroundName}'.");

            SpriteRenderer[] panels = background.GetComponentsInChildren<SpriteRenderer>();
            Bounds union = panels[0].bounds;
            foreach (SpriteRenderer panel in panels.Skip(1))
                union.Encapsulate(panel.bounds);

            return union;
        }

        private static float LeftEdgeOfScreen()
        {
            Camera camera = Camera.main;
            return camera.transform.position.x - HalfViewWidth(camera);
        }

        private static float RightEdgeOfScreen()
        {
            Camera camera = Camera.main;
            return camera.transform.position.x + HalfViewWidth(camera);
        }

        private static float HalfViewWidth(Camera camera) => camera.orthographicSize * camera.aspect;

        private static IEnumerator WaitUntil(System.Func<bool> condition)
        {
            float end = Time.realtimeSinceStartup + TimeoutSeconds;
            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, end, "Estourou o tempo esperando o fim de uma troca de sala pendente.");
                yield return null;
            }
        }
    }
}

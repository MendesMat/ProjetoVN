using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjetoVN.GameFlow.DevTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ProjetoVN.Tests.EditMode
{
    /// <summary>
    /// Todo objeto interativo de uma sala do build que ainda usa o quadrado de placeholder tem o rótulo provisório,
    /// com um nome que a fonte sabe desenhar. "Objeto interativo" é o que tem collider, porque é por ele que o
    /// seletor do mouse o acha: o contorno (<c>Highlight</c>) e um fundo de sala usam o mesmo quadrado sem collider.
    /// Quando a arte definitiva troca o sprite, o teste deixa de cobrar o objeto.
    /// </summary>
    public sealed class PlaceholderLabelsTests
    {
        private const string SquareSpriteGuid = "311925a002f4447b3a28927169b83ea6";
        private const string FontFilePath = "Assets/Fonts/AtkinsonHyperlegibleNext/AtkinsonHyperlegibleNext-Regular.ttf";
        private const string LabelField = "label";
        private const string DisplayNameField = "displayName";

        private readonly struct InteractableReading
        {
            public bool UsesPlaceholderSquare { get; }
            public bool HasLabelComponent { get; }
            public bool HasLabelText { get; }
            public string DisplayName { get; }

            public InteractableReading(bool usesPlaceholderSquare, bool hasLabelComponent, bool hasLabelText, string displayName)
            {
                UsesPlaceholderSquare = usesPlaceholderSquare;
                HasLabelComponent = hasLabelComponent;
                HasLabelText = hasLabelText;
                DisplayName = displayName;
            }
        }

        private List<ComponentReading<InteractableReading>> _interactables;

        [OneTimeSetUp]
        public void ReadTheInteractablesOfTheBuildRooms()
        {
            string[] roomPrefixes = BuildRooms.OfTheBuildSettings().ScenePathByRoom.Values
                .Select(scenePath => scenePath + ":")
                .ToArray();

            _interactables = ProjectComponents
                .ReadAll<Collider2D, InteractableReading>(Read)
                .Where(reading => roomPrefixes.Any(prefix => reading.Location.StartsWith(prefix)))
                .Where(reading => reading.Value.UsesPlaceholderSquare)
                .ToList();
        }

        [Test]
        public void BuildRooms_HaveInteractablesWithThePlaceholderSquare()
        {
            Assert.IsNotEmpty(_interactables,
                "nenhum objeto interativo com o quadrado de placeholder foi achado nas salas do build; o teste não cobre nada");
        }

        [Test]
        public void BuildRooms_PlaceholderInteractablesHaveTheLabel()
        {
            IEnumerable<string> unlabeled = _interactables
                .Where(reading => !reading.Value.HasLabelComponent)
                .Select(reading => $"{reading.Location}: sem o componente PlaceholderLabel");

            IEnumerable<string> withoutText = _interactables
                .Where(reading => reading.Value.HasLabelComponent && !reading.Value.HasLabelText)
                .Select(reading => $"{reading.Location}: PlaceholderLabel sem o filho Label");

            CollectionAssert.IsEmpty(unlabeled.Concat(withoutText),
                "objetos interativos com o quadrado de placeholder precisam do rótulo (prefab Interactable); " +
                "se a arte definitiva chegou, troque o sprite");
        }

        [Test]
        public void BuildRooms_PlaceholderLabelsHaveADisplayName()
        {
            IEnumerable<string> unnamed = _interactables
                .Where(reading => reading.Value.HasLabelComponent)
                .Where(reading => PlaceholderRoles.IsNameMissing(reading.Value.DisplayName))
                .Select(reading => $"{reading.Location}: Display Name vazio");

            CollectionAssert.IsEmpty(unnamed, "o nome do rótulo é escrito à mão em cada objeto");
        }

        [Test]
        public void BuildRooms_PlaceholderLabelNamesUseOnlyGlyphsTheFontHas()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontFilePath);
            Assert.IsNotNull(font, $"a fonte '{FontFilePath}' não existe mais; atualize este teste");

            FontEngine.InitializeFontEngine();
            Assert.AreEqual(FontEngineError.Success, FontEngine.LoadFontFace(font));

            IEnumerable<string> unsupported = _interactables
                .Where(reading => reading.Value.HasLabelComponent && !PlaceholderRoles.IsNameMissing(reading.Value.DisplayName))
                .SelectMany(reading => reading.Value.DisplayName.Trim()
                    .Where(character => !HasGlyph(character))
                    .Distinct()
                    .Select(character => $"{reading.Location}: a fonte não tem '{character}'"));

            CollectionAssert.IsEmpty(unsupported,
                "um glifo ausente faria o TextMeshPro criar um filho de reserva em tempo de execução, " +
                "que o HiddenUntilStoryFlag não esconde");
        }

        private static InteractableReading Read(Collider2D collider)
        {
            PlaceholderLabel label = collider.GetComponent<PlaceholderLabel>();

            return new InteractableReading(
                UsesPlaceholderSquare(collider.GetComponent<SpriteRenderer>()),
                hasLabelComponent: label != null,
                hasLabelText: label != null && HasObjectInField(label, LabelField),
                displayName: label != null ? SerializedFields.StringOf(label, DisplayNameField) : null);
        }

        private static bool UsesPlaceholderSquare(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null) return false;

            string spritePath = AssetDatabase.GetAssetPath(renderer.sprite);
            return AssetDatabase.AssetPathToGUID(spritePath) == SquareSpriteGuid;
        }

        private static bool HasObjectInField(Object target, string fieldName)
        {
            SerializedProperty field = new SerializedObject(target).FindProperty(fieldName);
            if (field == null)
                Assert.Fail($"O campo '{fieldName}' do {target.GetType().Name} foi renomeado; atualize este teste.");

            return field.objectReferenceValue != null;
        }

        private static bool HasGlyph(char character) => FontEngine.TryGetGlyphIndex(character, out _);
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjetoVN.GameFlow.Persistence;
using UnityEngine;
using UnityEngine.Events;

namespace ProjetoVN.Tests.EditMode
{
    public sealed class ExposedFieldsTests
    {
        private sealed class WithSerializeField
        {
            [SerializeField] private int field;
        }

        private sealed class WithTooltip
        {
            [Tooltip("explica o campo")]
            [SerializeField] private int field;
        }

        private sealed class WithPublicFields
        {
            public int number;
            public UnityEvent onSomething;
        }

        private sealed class WithFieldsTheInspectorDoesNotShow
        {
            private int notSerialized;
            private static int staticField;
            private const int Constant = 1;
            private readonly int readOnlyField = 1;
            [NonSerialized] public int nonSerializedPublic;
            [HideInInspector] public int hidden;
            [SerializeField, HideInInspector] private int hiddenPrivate;

            public int Use() => notSerialized + staticField + Constant + readOnlyField;
        }

        [Serializable]
        private sealed class Leaf
        {
            [SerializeField] private int untooltipped;
        }

        [Serializable]
        private sealed class SelfReferencing
        {
            [SerializeField] private SelfReferencing self;
            [SerializeField] private List<SelfReferencing> children;
        }

        private sealed class WithNestedList
        {
            [Tooltip("os filhos")]
            [SerializeField] private List<Leaf> list;
        }

        private sealed class WithNestedArray
        {
            [Tooltip("os filhos")]
            [SerializeField] private Leaf[] array;
        }

        private sealed class WithNestedField
        {
            [Tooltip("o filho")]
            [SerializeField] private Leaf single;
        }

        private class Base
        {
            [SerializeField] private int fromBase;
        }

        private sealed class Derived : Base
        {
            [Tooltip("explica o campo")]
            [SerializeField] private int own;
        }

        private sealed class WithUnityTypes
        {
            [Tooltip("a posição")]
            [SerializeField] private Vector3 position;

            [Tooltip("o evento")]
            [SerializeField] private UnityEvent onSomething;
        }

        private sealed class WithTypeFromAnotherProjectAssembly
        {
            [Tooltip("o save")]
            [SerializeField] private GameState state;
        }

        [Serializable]
        private class SerializableBase
        {
            [SerializeField] private int fromSerializableBase;
        }

        [Serializable]
        private sealed class SerializableDerived : SerializableBase
        {
            [Tooltip("explica o campo")]
            [SerializeField] private int own;
        }

        private sealed class WithSerializableDerived
        {
            [Tooltip("o filho")]
            [SerializeField] private SerializableDerived child;
        }

        [Serializable]
        private sealed class IntEvent : UnityEvent<int>
        {
        }

        private sealed class WithEventSubclass
        {
            [Tooltip("o evento")]
            [SerializeField] private IntEvent onNumber;
        }

        [Test]
        public void WithoutTooltip_SerializeFieldWithoutTooltip_IsListed()
        {
            CollectionAssert.AreEqual(new[] { "WithSerializeField.field" },
                ExposedFields.WithoutTooltip(typeof(WithSerializeField)));
        }

        [Test]
        public void WithoutTooltip_FieldWithTooltip_IsNotListed()
        {
            CollectionAssert.IsEmpty(ExposedFields.WithoutTooltip(typeof(WithTooltip)));
        }

        [Test]
        public void WithoutTooltip_PublicField_IsListed()
        {
            CollectionAssert.AreEquivalent(
                new[] { "WithPublicFields.number", "WithPublicFields.onSomething" },
                ExposedFields.WithoutTooltip(typeof(WithPublicFields)));
        }

        [Test]
        public void WithoutTooltip_FieldsTheInspectorDoesNotShow_AreIgnored()
        {
            CollectionAssert.IsEmpty(ExposedFields.WithoutTooltip(typeof(WithFieldsTheInspectorDoesNotShow)));
        }

        [Test]
        public void WithoutTooltip_SerializableElementTypeInAList_IsInspected()
        {
            CollectionAssert.AreEqual(new[] { "Leaf.untooltipped" },
                ExposedFields.WithoutTooltip(typeof(WithNestedList)));
        }

        [Test]
        public void WithoutTooltip_SerializableElementTypeInAnArray_IsInspected()
        {
            CollectionAssert.AreEqual(new[] { "Leaf.untooltipped" },
                ExposedFields.WithoutTooltip(typeof(WithNestedArray)));
        }

        [Test]
        public void WithoutTooltip_SerializableFieldType_IsInspected()
        {
            CollectionAssert.AreEqual(new[] { "Leaf.untooltipped" },
                ExposedFields.WithoutTooltip(typeof(WithNestedField)));
        }

        [Test]
        public void WithoutTooltip_TypeThatReferencesItself_ListsEachFieldOnce()
        {
            CollectionAssert.AreEquivalent(
                new[] { "SelfReferencing.self", "SelfReferencing.children" },
                ExposedFields.WithoutTooltip(typeof(SelfReferencing)));
        }

        [Test]
        public void WithoutTooltip_InheritedField_IsListedOnlyByItsDeclaringType()
        {
            CollectionAssert.IsEmpty(ExposedFields.WithoutTooltip(typeof(Derived)));
            CollectionAssert.AreEqual(new[] { "Base.fromBase" }, ExposedFields.WithoutTooltip(typeof(Base)));
        }

        [Test]
        public void WithoutTooltip_UnityTypes_AreNotInspected()
        {
            CollectionAssert.IsEmpty(ExposedFields.WithoutTooltip(typeof(WithUnityTypes)));
        }

        [Test]
        public void WithoutTooltip_SerializableTypeFromAnotherProjectAssembly_IsInspected()
        {
            Assert.That(ExposedFields.WithoutTooltip(typeof(WithTypeFromAnotherProjectAssembly)),
                Has.Some.StartsWith("GameState."));
        }

        [Test]
        public void WithoutTooltip_BaseOfASerializableFieldType_IsInspected()
        {
            CollectionAssert.AreEqual(new[] { "SerializableBase.fromSerializableBase" },
                ExposedFields.WithoutTooltip(typeof(WithSerializableDerived)));
        }

        [Test]
        public void WithoutTooltip_BaseOutsideTheProject_IsNotInspected()
        {
            CollectionAssert.IsEmpty(ExposedFields.WithoutTooltip(typeof(WithEventSubclass)));
        }
    }
}

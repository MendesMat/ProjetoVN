using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjetoVN.Dialogue.Characters
{
    /// <summary>
    /// A lista de personagens do jogo. O roteiro cita o personagem pelo nome exibido (<c>Luna: ...</c>);
    /// o <c>Id</c> é o que o código usa. Um asset, referenciado por quem precisa dele (D-04).
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterRegistry", menuName = "Characters/Character Registry")]
    public sealed class CharacterRegistry : ScriptableObject
    {
        [Tooltip("Todos os personagens que o roteiro pode citar. Um personagem fora da lista aparece no jogo só com o nome, sem cor e sem sprite.")]
        [SerializeField] private List<CharacterSO> characters = new();

        [Tooltip("O protagonista: quem entra na tela quando as opções de resposta abrem. Precisa estar na lista acima. " +
                 "Vazio: as opções não trazem ninguém para a tela e o console avisa.")]
        [SerializeField] private CharacterSO protagonist;

        public IReadOnlyList<CharacterSO> Characters => characters;
        public CharacterSO Protagonist => protagonist;

        public bool TryGetByScriptName(string scriptName, out CharacterSO character)
        {
            character = characters.Find(candidate =>
                candidate != null && string.Equals(candidate.DisplayName, scriptName, StringComparison.Ordinal));
            return character != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            WarnAboutProtagonist();

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (CharacterSO character in characters)
            {
                if (character == null) continue;

                if (!ids.Add(character.Id))
                    Debug.LogError($"[CharacterRegistry] Id duplicado: '{character.Id}' em {character.name}.", character);

                if (!names.Add(character.DisplayName))
                    Debug.LogError($"[CharacterRegistry] Nome exibido duplicado: '{character.DisplayName}' em {character.name}.", character);
            }
        }

        private void WarnAboutProtagonist()
        {
            if (protagonist == null)
                Debug.LogWarning($"[CharacterRegistry] {name}: o 'Protagonist' está vazio: as opções de resposta não trazem ninguém para a tela.", this);
            else if (!characters.Contains(protagonist))
                Debug.LogWarning($"[CharacterRegistry] {name}: o 'Protagonist' ({protagonist.name}) não está na lista 'Characters'.", this);
        }
#endif
    }
}

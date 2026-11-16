using System;
using System.Collections.Generic;
using ProjetoVN.Dialogue.Logic;
using UnityEngine;

namespace ProjetoVN.GameFlow.ScreenEffects
{
    /// <summary>
    /// As imagens de tela cheia que o roteiro pode pedir, cada uma por um id. Somente leitura em runtime (D-02): o que
    /// está na tela mora no <see cref="ScriptScreen"/>. Uma imagem nova de roteiro é uma linha aqui.
    /// </summary>
    [CreateAssetMenu(fileName = "PictureRegistry", menuName = "Game Flow/Picture Registry")]
    public sealed class PictureRegistry : ScriptableObject
    {
        [Serializable]
        public sealed class PictureEntry
        {
            [Tooltip("O nome que o roteiro digita (<<mostrar_imagem lembranca_pais>>): minúsculas sem acento, dígitos e _. " +
                     "Único no registro inteiro. Vazio ou fora do formato = a imagem não pode ser pedida.")]
            [SerializeField] private string id;

            [Tooltip("A imagem de tela cheia (16:9, importada com Sprite Mode = Single). Vazio = o roteiro pede o id e nada aparece.")]
            [SerializeField] private Sprite sprite;

            public string Id => id;

            public Sprite Sprite => sprite;
        }

        [Tooltip("As imagens do <<mostrar_imagem id>>. Uma linha por imagem; a tabela de imagens de docs/autoria/roteiro.md ganha a mesma linha.")]
        [SerializeField] private List<PictureEntry> pictures = new();

        public IReadOnlyList<PictureEntry> Pictures => pictures;

        public bool TryGet(string id, out Sprite sprite)
        {
            PictureEntry entry = pictures.Find(candidate => candidate != null && candidate.Id == id);
            sprite = entry?.Sprite;
            return sprite != null;
        }

        public string DescribeIds() => string.Join(", ", pictures.ConvertAll(entry => entry?.Id));

#if UNITY_EDITOR
        private void OnValidate()
        {
            var seen = new HashSet<string>();
            foreach (PictureEntry entry in pictures)
            {
                if (entry == null) continue;

                if (!ScriptNodeName.FollowsConvention(entry.Id))
                    Debug.LogWarning($"[PictureRegistry] O id '{entry.Id}' está vazio ou fora do formato (minúsculas sem acento, dígitos e _).", this);
                else if (!seen.Add(entry.Id))
                    Debug.LogWarning($"[PictureRegistry] Id repetido: '{entry.Id}'. Cada id vale uma vez no registro.", this);

                if (entry.Sprite == null)
                    Debug.LogWarning($"[PictureRegistry] A imagem '{entry.Id}' está sem Sprite: o roteiro a pede e nada aparece.", this);
            }
        }
#endif
    }
}

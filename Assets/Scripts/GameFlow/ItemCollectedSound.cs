using ProjetoVN.Audio;
using ProjetoVN.Core.Messaging;
using ProjetoVN.Inventory.Messages;
using UnityEngine;

namespace ProjetoVN.GameFlow
{
    /// <summary>
    /// Toca o som de coleta a cada <c>ItemCollectedMessage</c>: item pego na sala ou entregue pelo roteiro. É um ouvinte
    /// a mais da notificação, então nenhum coletável precisa ser ligado. Vive no <c>Managers.prefab</c>.
    /// </summary>
    public sealed class ItemCollectedSound : MonoBehaviour
    {
        private void OnEnable() => MessageBroker.Subscribe<ItemCollectedMessage>(OnItemCollected);

        private void OnDisable() => MessageBroker.Unsubscribe<ItemCollectedMessage>(OnItemCollected);

        private void OnItemCollected(ItemCollectedMessage message)
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[ItemCollectedSound] Não há AudioManager (o prefab Managers não foi criado). A coleta ficou sem som.", this);
                return;
            }

            AudioManager.Instance.PlayInterfaceSound(InterfaceSound.ItemCollected);
        }
    }
}

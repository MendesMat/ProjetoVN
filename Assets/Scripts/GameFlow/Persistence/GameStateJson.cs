using System;
using UnityEngine;

namespace ProjetoVN.GameFlow.Persistence
{
    /// <summary>
    /// Converte o texto de um save em <see cref="GameState"/>. Mora separada do <c>GameSaveManager</c> porque
    /// ele é um MonoBehaviour que lê arquivo, e a conversão é C# puro testável. O <c>JsonUtility</c> lança
    /// <see cref="ArgumentException"/> em JSON inválido, e um save corrompido não pode derrubar o jogo.
    /// </summary>
    public static class GameStateJson
    {
        public static bool TryRead(string json, out GameState state)
        {
            state = null;
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                state = JsonUtility.FromJson<GameState>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return state != null;
        }
    }
}

using UnityEngine;

namespace ProjetoVN.GameFlow.ScreenEffects
{
    /// <summary>
    /// O alfa de uma camada de tela indo de um valor a outro em um tempo. C# puro: o <see cref="ScriptScreen"/>
    /// só soma o tempo de cada quadro e aplica o alfa devolvido. Duração zero ou negativa vale na hora.
    /// </summary>
    public sealed class AlphaTransition
    {
        private readonly float _from;
        private readonly float _to;
        private readonly float _seconds;
        private float _elapsed;

        public AlphaTransition(float from, float to, float seconds)
        {
            _from = from;
            _to = to;
            _seconds = seconds;
        }

        public bool IsDone => _elapsed >= _seconds;

        public float Alpha => IsDone ? _to : Mathf.Lerp(_from, _to, _elapsed / _seconds);

        public float Advance(float deltaTime)
        {
            _elapsed += deltaTime;
            return Alpha;
        }
    }
}

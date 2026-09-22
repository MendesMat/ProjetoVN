using UnityEngine;

namespace ProjetoVN.Dialogue.Data
{
    /// <summary>
    /// Base de um efeito colateral de diálogo: dar um item, ligar uma flag, etc.
    /// <para>
    /// Mora em Dialogue porque <see cref="DialogueNode"/> e <see cref="DialogueChoice"/> declaram o campo,
    /// mas as implementações concretas moram em GameFlow, que é quem enxerga Inventory e Core.
    /// A direção da dependência é protegida por <i>onde a subclasse vive</i>, não por uma interface:
    /// por isso <see cref="Execute"/> não recebe contexto algum (ver D-04, sem service locator).
    /// </para>
    /// <para>
    /// Efeitos são assets <b>compartilhados</b> entre nós. Nunca guarde estado de runtime em um
    /// <c>[SerializeField]</c> aqui (ver D-02): isso sujaria o .asset e vazaria entre sessões de Play.
    /// </para>
    /// </summary>
    public abstract class DialogueEffectSO : ScriptableObject
    {
        public abstract void Execute();
    }
}

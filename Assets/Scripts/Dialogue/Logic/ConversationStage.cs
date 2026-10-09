using System;
using ProjetoVN.Dialogue.Characters;

namespace ProjetoVN.Dialogue.Logic
{
    public enum StageSide
    {
        Left,
        Right,
    }

    public readonly struct StageOccupant
    {
        public StageOccupant(CharacterSO character, string expression, bool isHighlighted)
        {
            Character = character;
            Expression = expression;
            IsHighlighted = isHighlighted;
        }

        public CharacterSO Character { get; }
        public string Expression { get; }
        public bool IsHighlighted { get; }
        public bool IsEmpty => Character == null;
    }

    /// <summary>
    /// Quem está na tela de personagens da conversa em curso: dois lugares, quem está em destaque e a última
    /// expressão de cada um. Estado de apresentação (D-02): vale só para a conversa em curso e não entra no save.
    /// Usa o <see cref="CharacterSO"/> só como chave; nunca lê os campos dele.
    /// </summary>
    public sealed class ConversationStage
    {
        private const int SideCount = 2;

        private readonly StageOccupant[] _occupants = new StageOccupant[SideCount];
        private readonly int[] _lastTurn = new int[SideCount];
        private int _turn;

        public StageOccupant Occupant(StageSide side) => _occupants[(int)side];

        public void Speak(CharacterSO character, string expression)
        {
            int seat = SeatFor(character);
            DimEveryone();
            _occupants[seat] = new StageOccupant(character, expression, isHighlighted: true);
            _lastTurn[seat] = ++_turn;
        }

        public void DimEveryone()
        {
            for (int seat = 0; seat < SideCount; seat++)
                _occupants[seat] = new StageOccupant(_occupants[seat].Character, _occupants[seat].Expression, isHighlighted: false);
        }

        public void GiveTurnToPlayer(CharacterSO protagonist, string defaultExpression)
        {
            int seat = SeatOf(protagonist);
            string expression = seat >= 0 ? _occupants[seat].Expression : defaultExpression;
            Speak(protagonist, expression);
        }

        public void Clear()
        {
            Array.Clear(_occupants, 0, SideCount);
            Array.Clear(_lastTurn, 0, SideCount);
        }

        private int SeatFor(CharacterSO character)
        {
            int seat = SeatOf(character);
            if (seat >= 0) return seat;

            int emptySeat = Array.FindIndex(_occupants, occupant => occupant.IsEmpty);
            return emptySeat >= 0 ? emptySeat : SeatOfWhoSpokeLongestAgo();
        }

        private int SeatOf(CharacterSO character) =>
            Array.FindIndex(_occupants, occupant => occupant.Character == character);

        private int SeatOfWhoSpokeLongestAgo() => _lastTurn[(int)StageSide.Left] <= _lastTurn[(int)StageSide.Right]
            ? (int)StageSide.Left
            : (int)StageSide.Right;
    }
}

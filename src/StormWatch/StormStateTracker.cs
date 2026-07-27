using System;
using System.Collections.Generic;
using Wotc.Mtgo.Gre.External.Messaging;

namespace StormWatch
{
    internal sealed class StormStateTracker
    {
        private readonly HashSet<uint> _processedAnnotationIds = new HashSet<uint>();
        private string _matchId = string.Empty;
        private uint _gameNumber;
        private uint _turn;
        private int _count;
        private bool _inGame;

        internal StormUpdate Process(GREToClientMessage message)
        {
            if (message.Type != GREMessageType.GameStateMessage ||
                message.GameStateMessage == null)
            {
                return StormUpdate.None;
            }

            var gameState = message.GameStateMessage;
            var changed = false;
            var reset = false;
            var incremented = false;

            if (gameState.GameInfo != null)
            {
                var info = gameState.GameInfo;
                var incomingMatchId = info.MatchID ?? string.Empty;
                var hasIdentity = incomingMatchId.Length > 0 && info.GameNumber > 0;
                var newGame = hasIdentity &&
                    (!string.Equals(_matchId, incomingMatchId, StringComparison.Ordinal) ||
                     _gameNumber != info.GameNumber);

                if (newGame)
                {
                    _matchId = incomingMatchId;
                    _gameNumber = info.GameNumber;
                    _turn = 0;
                    _count = 0;
                    _processedAnnotationIds.Clear();
                    reset = true;
                    changed = true;
                }

                var wasInGame = _inGame;
                if (info.MatchState == MatchState.GameInProgress)
                    _inGame = true;
                else if (info.MatchState == MatchState.GameComplete ||
                         info.MatchState == MatchState.MatchComplete)
                    _inGame = false;

                changed |= wasInGame != _inGame;
            }

            if (gameState.TurnInfo != null && gameState.TurnInfo.TurnNumber > 0)
            {
                var incomingTurn = gameState.TurnInfo.TurnNumber;
                if (incomingTurn != _turn)
                {
                    _turn = incomingTurn;
                    _count = 0;
                    reset = true;
                    changed = true;
                }
            }

            foreach (var annotation in gameState.Annotations)
            {
                if (annotation == null) continue;
                if (annotation.Id != 0 && !_processedAnnotationIds.Add(annotation.Id))
                    continue;
                if (!annotation.Type.Contains(AnnotationType.ZoneTransfer))
                    continue;
                if (!HasCategory(annotation, "CastSpell"))
                    continue;

                _count++;
                incremented = true;
                changed = true;
            }

            return changed
                ? new StormUpdate(_count, _turn, _inGame, incremented, reset)
                : StormUpdate.None;
        }

        private static bool HasCategory(AnnotationInfo annotation, string expected)
        {
            foreach (var detail in annotation.Details)
            {
                if (!string.Equals(detail.Key, "category", StringComparison.Ordinal))
                    continue;

                foreach (var value in detail.ValueString)
                {
                    if (string.Equals(value, expected, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }
    }

    internal readonly struct StormUpdate
    {
        internal static readonly StormUpdate None =
            new StormUpdate(0, 0, false, false, false, false);

        internal StormUpdate(int count, uint turn, bool inGame, bool incremented, bool reset)
            : this(count, turn, inGame, incremented, reset, true)
        {
        }

        private StormUpdate(
            int count,
            uint turn,
            bool inGame,
            bool incremented,
            bool reset,
            bool changed)
        {
            Count = count;
            Turn = turn;
            InGame = inGame;
            Incremented = incremented;
            Reset = reset;
            Changed = changed;
        }

        internal int Count { get; }
        internal uint Turn { get; }
        internal bool InGame { get; }
        internal bool Incremented { get; }
        internal bool Reset { get; }
        internal bool Changed { get; }
    }
}

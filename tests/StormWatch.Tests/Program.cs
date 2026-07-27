using System;
using StormWatch;
using Wotc.Mtgo.Gre.External.Messaging;

var tests = new (string Name, System.Action Run)[]
{
    ("starts a game at zero", StartsGameAtZero),
    ("counts only CastSpell zone transfers", CountsOnlySpells),
    ("deduplicates annotation IDs", DeduplicatesAnnotations),
    ("resets on each turn", ResetsOnTurn),
    ("counts a spell in the first update of a turn", CountsAfterTurnReset),
    ("hides after game completion", HidesAtGameEnd),
    ("clears deduplication state for the next game", ClearsForNextGame),
    ("ignores non-game-state messages", IgnoresOtherMessages),
};

var failures = 0;
foreach (var test in tests)
{
    try
    {
        test.Run();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {test.Name}: {ex.Message}");
    }
}

Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed");
return failures == 0 ? 0 : 1;

static void StartsGameAtZero()
{
    var tracker = new StormStateTracker();
    var update = tracker.Process(State(gameInfo: Game("match-a", 1), turn: 1));

    Equal(0, update.Count);
    Equal((uint)1, update.Turn);
    True(update.InGame);
    True(update.Reset);
}

static void CountsOnlySpells()
{
    var tracker = StartedTracker();

    var land = tracker.Process(State(annotation: Transfer(1, "PlayLand")));
    False(land.Changed);

    var ability = tracker.Process(State(annotation: Annotation(2, AnnotationType.AbilityInstanceCreated)));
    False(ability.Changed);

    var spell = tracker.Process(State(annotation: Transfer(3, "CastSpell")));
    Equal(1, spell.Count);
    True(spell.Incremented);

    var copied = tracker.Process(State(annotation: Transfer(4, "Copy")));
    False(copied.Changed);
}

static void DeduplicatesAnnotations()
{
    var tracker = StartedTracker();
    Equal(1, tracker.Process(State(annotation: Transfer(42, "CastSpell"))).Count);
    False(tracker.Process(State(annotation: Transfer(42, "CastSpell"))).Changed);
}

static void ResetsOnTurn()
{
    var tracker = StartedTracker();
    tracker.Process(State(annotation: Transfer(1, "CastSpell")));
    tracker.Process(State(annotation: Transfer(2, "CastSpell")));

    var update = tracker.Process(State(turn: 2));
    Equal(0, update.Count);
    Equal((uint)2, update.Turn);
    True(update.Reset);
}

static void CountsAfterTurnReset()
{
    var tracker = StartedTracker();
    tracker.Process(State(annotation: Transfer(1, "CastSpell")));

    var update = tracker.Process(State(turn: 2, annotation: Transfer(2, "CastSpell")));
    Equal(1, update.Count);
    True(update.Reset);
    True(update.Incremented);
}

static void HidesAtGameEnd()
{
    var tracker = StartedTracker();
    var info = Game("match-a", 1);
    info.MatchState = MatchState.GameComplete;

    var update = tracker.Process(State(gameInfo: info));
    False(update.InGame);
    True(update.Changed);
}

static void ClearsForNextGame()
{
    var tracker = StartedTracker();
    Equal(1, tracker.Process(State(annotation: Transfer(9, "CastSpell"))).Count);

    tracker.Process(State(gameInfo: Game("match-a", 2), turn: 1));
    var update = tracker.Process(State(annotation: Transfer(9, "CastSpell")));
    Equal(1, update.Count);
}

static void IgnoresOtherMessages()
{
    var tracker = StartedTracker();
    var message = new GREToClientMessage { Type = GREMessageType.PromptReq };
    False(tracker.Process(message).Changed);
}

static StormStateTracker StartedTracker()
{
    var tracker = new StormStateTracker();
    tracker.Process(State(gameInfo: Game("match-a", 1), turn: 1));
    return tracker;
}

static GREToClientMessage State(
    GameInfo gameInfo = null,
    uint? turn = null,
    AnnotationInfo annotation = null)
{
    var state = new GameStateMessage
    {
        Type = GameStateType.Diff,
        GameStateId = 1,
        GameInfo = gameInfo,
    };
    if (turn.HasValue)
        state.TurnInfo = new TurnInfo { TurnNumber = turn.Value };
    if (annotation != null)
        state.Annotations.Add(annotation);

    return new GREToClientMessage
    {
        Type = GREMessageType.GameStateMessage,
        GameStateMessage = state,
    };
}

static GameInfo Game(string matchId, uint gameNumber)
{
    return new GameInfo
    {
        MatchID = matchId,
        GameNumber = gameNumber,
        Stage = GameStage.Play,
        MatchState = MatchState.GameInProgress,
    };
}

static AnnotationInfo Transfer(uint id, string category)
{
    var annotation = Annotation(id, AnnotationType.ZoneTransfer);
    var detail = new KeyValuePairInfo { Key = "category" };
    detail.ValueString.Add(category);
    annotation.Details.Add(detail);
    return annotation;
}

static AnnotationInfo Annotation(uint id, AnnotationType type)
{
    var annotation = new AnnotationInfo { Id = id };
    annotation.Type.Add(type);
    return annotation;
}

static void Equal<T>(T expected, T actual)
{
    if (!Equals(expected, actual))
        throw new InvalidOperationException($"expected {expected}, got {actual}");
}

static void True(bool value)
{
    if (!value) throw new InvalidOperationException("expected true");
}

static void False(bool value)
{
    if (value) throw new InvalidOperationException("expected false");
}

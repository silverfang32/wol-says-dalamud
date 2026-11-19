using WoLSays;
using static WoLSays.Member;
using static WoLSays.SetSlot;

(string Name, Action Run)[] tests =
[
    ("Initial visibility and values", Initial),
    ("Every member inherits both set values", Inheritance),
    ("False remains captured and can be cleared", FalseMembership),
    ("Elemental corrections reconcile both sets", ElementalCorrections),
    ("Optional captures reconcile both sets", OptionalCorrections),
    ("One or both optional members remain available", OptionalCombinations),
    ("Visibility restores according to remaining assignments", VisibilityRestoration),
    ("Independent shape mappings and toggles", Shapes),
    ("Independent floor positions and grouped Thunder", Positions),
    ("Reset restores the initial state", Reset),
];

foreach (var (name, run) in tests)
{
    try
    {
        run();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception error)
    {
        Console.Error.WriteLine($"FAIL {name}: {error.Message}");
        return 1;
    }
}

Console.WriteLine($"{tests.Length} tests passed.");
return 0;

static void Expect<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"Expected {expected?.ToString() ?? "null"}, got {actual?.ToString() ?? "null"}.");
}

static void ExpectInitial(Encounter state)
{
    foreach (var slot in new[] { First, Second })
    {
        Expect<bool?>(null, state.SetValue(slot));
        Expect(Encounter.AllMembers, state.VisibleMembers(slot));
        foreach (var member in new[] { Thunder, Water, Bomb, Gaze })
            Expect<bool?>(null, state.MemberValue(slot, member));
    }

    Expect<bool?>(null, state.Inferno);
    Expect<bool?>(null, state.Tsunami);
    Expect(Shape.Unknown, state.InfernoShape);
    Expect(Shape.Unknown, state.TsunamiShape);
    Expect(FloorPosition.Unknown, state.ThunderPosition);
    Expect(FloorPosition.Unknown, state.IcePosition);
}

static void Initial() => ExpectInitial(new Encounter());

static void Inheritance()
{
    foreach (var slot in new[] { First, Second })
    foreach (var member in new[] { Thunder, Water, Bomb, Gaze })
    {
        var state = new Encounter();
        state.ToggleMember(slot, member);
        Expect<bool?>(true, state.SetValue(slot));
        Expect<bool?>(true, state.MemberValue(slot, member));
        state.ToggleSet(slot);
        Expect<bool?>(false, state.MemberValue(slot, member));
        state.ToggleSet(slot);
        Expect<bool?>(true, state.MemberValue(slot, member));
    }
}

static void FalseMembership()
{
    var state = new Encounter();
    state.ToggleSet(First);
    state.ToggleSet(First);
    state.ToggleMember(First, Water);
    Expect<bool?>(false, state.MemberValue(First, Water));
    Expect<bool?>(null, state.MemberValue(First, Thunder));
    Expect(Water, state.VisibleMembers(First));
    state.ToggleMember(First, Water);
    Expect<bool?>(null, state.MemberValue(First, Water));
    Expect<bool?>(false, state.SetValue(First));
}

static void ElementalCorrections()
{
    var state = new Encounter();
    state.ToggleMember(First, Thunder);
    state.ToggleMember(First, Water);
    Expect<bool?>(null, state.MemberValue(First, Thunder));
    state.ToggleMember(Second, Bomb);
    state.ToggleMember(Second, Gaze);
    state.ToggleMember(Second, Thunder);
    Expect<bool?>(null, state.MemberValue(First, Water));
    Expect<bool?>(true, state.SetValue(First));
    Expect<bool?>(null, state.MemberValue(Second, Bomb));
    Expect<bool?>(null, state.MemberValue(Second, Gaze));
    Expect(Thunder, state.VisibleMembers(Second));
    Expect(Bomb | Gaze, state.VisibleMembers(First));
}

static void OptionalCorrections()
{
    var state = new Encounter();
    state.ToggleMember(First, Thunder);
    state.ToggleMember(Second, Bomb);
    state.ToggleMember(Second, Gaze);
    state.ToggleMember(First, Bomb);
    Expect<bool?>(null, state.MemberValue(First, Thunder));
    Expect<bool?>(true, state.MemberValue(First, Bomb));
    Expect<bool?>(null, state.MemberValue(First, Gaze));
    Expect<bool?>(null, state.MemberValue(Second, Bomb));
    Expect<bool?>(null, state.MemberValue(Second, Gaze));
    Expect<bool?>(true, state.SetValue(Second));
}

static void OptionalCombinations()
{
    foreach (var member in new[] { Bomb, Gaze })
    {
        var state = new Encounter();
        var other = member == Bomb ? Gaze : Bomb;
        state.ToggleMember(Second, member);
        Expect(Bomb | Gaze, state.VisibleMembers(Second));
        Expect<bool?>(null, state.MemberValue(Second, other));
        Expect<bool?>(null, state.SetValue(First));
        Expect(Thunder | Water, state.VisibleMembers(First));
        state.ToggleMember(Second, other);
        state.ToggleSet(Second);
        Expect<bool?>(false, state.MemberValue(Second, member));
        Expect<bool?>(false, state.MemberValue(Second, other));
        state.ToggleMember(Second, member);
        Expect<bool?>(false, state.MemberValue(Second, other));
    }
}

static void VisibilityRestoration()
{
    var state = new Encounter();
    state.ToggleMember(First, Thunder);
    Expect(Bomb | Gaze, state.VisibleMembers(Second));
    Expect<bool?>(null, state.SetValue(Second));
    state.ToggleMember(Second, Gaze);
    state.ToggleMember(First, Thunder);
    Expect(Thunder | Water, state.VisibleMembers(First));
    Expect(Bomb | Gaze, state.VisibleMembers(Second));
    state.ToggleMember(Second, Gaze);
    Expect(Encounter.AllMembers, state.VisibleMembers(First));
    Expect(Encounter.AllMembers, state.VisibleMembers(Second));
}

static void Shapes()
{
    var state = new Encounter();
    state.ToggleInferno();
    Expect<bool?>(true, state.Inferno);
    Expect(Shape.AoE, state.InfernoShape);
    Expect(Shape.Unknown, state.TsunamiShape);
    state.ToggleTsunami();
    Expect<bool?>(true, state.Tsunami);
    Expect(Shape.Donut, state.TsunamiShape);
    state.ToggleInferno();
    Expect(Shape.Donut, state.InfernoShape);
    state.ToggleTsunami();
    Expect(Shape.AoE, state.TsunamiShape);
    Expect<bool?>(null, state.SetValue(First));
    Expect(FloorPosition.Unknown, state.ThunderPosition);
}

static void Positions()
{
    var state = new Encounter();
    state.ToggleMember(First, Thunder);
    Expect(FloorPosition.Unknown, state.ThunderPosition);
    state.ToggleThunderPosition();
    Expect(FloorPosition.Inside, state.ThunderPosition);
    Expect(FloorPosition.Unknown, state.IcePosition);
    state.ToggleIcePosition();
    Expect(FloorPosition.Inside, state.IcePosition);
    state.ToggleThunderPosition();
    Expect(FloorPosition.Outside, state.ThunderPosition);
    Expect(FloorPosition.Inside, state.IcePosition);
    state.ToggleIcePosition();
    Expect(FloorPosition.Outside, state.IcePosition);
    state.ToggleThunderPosition();
    Expect(FloorPosition.Inside, state.ThunderPosition);
    Expect<bool?>(true, state.MemberValue(First, Thunder));
    Expect(Shape.Unknown, state.InfernoShape);
}

static void Reset()
{
    var state = new Encounter();
    state.ToggleMember(First, Thunder);
    state.ToggleMember(Second, Bomb);
    state.ToggleInferno();
    state.ToggleTsunami();
    state.ToggleThunderPosition();
    state.ToggleIcePosition();
    state.Reset();
    ExpectInitial(state);
    state.ToggleMember(First, Water);
    Expect<bool?>(true, state.MemberValue(First, Water));
}

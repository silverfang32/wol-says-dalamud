namespace WoLSays;

internal enum SetSlot { First, Second }

[Flags]
internal enum Member
{
    None = 0,
    Thunder = 1,
    Water = 2,
    Bomb = 4,
    Gaze = 8,
}

internal enum Shape { Unknown, AoE, Donut }
internal enum FloorPosition { Unknown, Inside, Outside }

internal sealed class Encounter
{
    private const Member Elemental = Member.Thunder | Member.Water;
    private const Member Optional = Member.Bomb | Member.Gaze;
    internal const Member AllMembers = Elemental | Optional;

    private record struct SetState(bool? Value, Member Members);

    private SetState first;
    private SetState second;
    public bool? Inferno { get; private set; }
    public bool? Tsunami { get; private set; }
    public Shape InfernoShape => Inferno switch { true => Shape.AoE, false => Shape.Donut, null => Shape.Unknown };
    public Shape TsunamiShape => Tsunami switch { true => Shape.Donut, false => Shape.AoE, null => Shape.Unknown };
    public FloorPosition ThunderPosition { get; private set; }
    public FloorPosition IcePosition { get; private set; }

    private ref SetState Set(SetSlot slot) => ref (slot == SetSlot.First ? ref first : ref second);

    private ref SetState Opposite(SetSlot slot) => ref (slot == SetSlot.First ? ref second : ref first);

    public bool? SetValue(SetSlot slot) => Set(slot).Value;

    public bool? MemberValue(SetSlot slot, Member member)
    {
        var set = Set(slot);
        return (set.Members & member) != 0 ? set.Value : null;
    }

    public void ToggleSet(SetSlot slot)
    {
        ref var set = ref Set(slot);
        set.Value = set.Value != true;
    }

    public void ToggleMember(SetSlot slot, Member member)
    {
        ref var set = ref Set(slot);
        if ((set.Members & member) != 0)
        {
            set.Members &= ~member;
            return;
        }

        Member family = (member & Elemental) != 0 ? Elemental : Optional;
        set.Value ??= true;
        set.Members = family == Elemental ? member : (set.Members & Optional) | member;
        Opposite(slot).Members &= ~family;
    }

    public Member VisibleMembers(SetSlot slot)
    {
        Member own = Set(slot).Members;
        Member other = Opposite(slot).Members;
        if ((own & Elemental) != 0)
            return own & Elemental;
        if ((own & Optional) != 0 || (other & Elemental) != 0)
            return Optional;
        return (other & Optional) != 0 ? Elemental : AllMembers;
    }

    public void ToggleInferno() => Inferno = Inferno != true;

    public void ToggleTsunami() => Tsunami = Tsunami != true;

    public void ToggleThunderPosition() => ThunderPosition = NextPosition(ThunderPosition);

    public void ToggleIcePosition() => IcePosition = NextPosition(IcePosition);

    private static FloorPosition NextPosition(FloorPosition current) =>
        current == FloorPosition.Inside ? FloorPosition.Outside : FloorPosition.Inside;

    public void Reset()
    {
        first = default;
        second = default;
        Inferno = Tsunami = null;
        ThunderPosition = IcePosition = FloorPosition.Unknown;
    }
}

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace WoLSays;

internal sealed class Overlay : Window
{
    private static readonly Member[] Members = [Member.Thunder, Member.Water, Member.Bomb, Member.Gaze];
    private const uint Muted = 0xff83766b;
    private const uint Faded = 0xff655c69;
    private const uint Border = 0xff534357;
    private const uint Purple = 0xffdfa8be;
    private readonly Encounter encounter = new();
    private readonly ISharedImmediateTexture flame;
    private readonly ISharedImmediateTexture wave;
    private Vector2 origin;
    private float scale;
    private Vector2? pendingPosition;
    private readonly OverlaySettings settings;

    public Overlay(ITextureProvider textures, string directory, OverlaySettings settings)
        : base("WoL Says###WoLSaysOverlay", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse |
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDocking)
    {
        this.settings = settings;
        flame = textures.GetFromFileAbsolute(Path.Combine(directory, "Assets", "flame.png"));
        wave = textures.GetFromFileAbsolute(Path.Combine(directory, "Assets", "wave.png"));
        Position = new Vector2(60, 160);
        PositionCondition = ImGuiCond.FirstUseEver;
        Size = new Vector2(240, 160);
        SizeCondition = ImGuiCond.Always;
        RespectCloseHotkey = false;
        DisableFadeInFadeOut = true;
    }

    public override void PreDraw()
    {
        scale = ImGuiHelpers.GlobalScale;
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(17 / 255f, 18 / 255f, 27 / 255f, Math.Clamp(settings.BackgroundOpacity, 20, 100) / 100f));
        ImGui.PushStyleColor(ImGuiCol.Border, Border);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8 * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4 * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, Vector2.Zero);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4) * scale);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(5);
        ImGui.PopStyleColor(2);

        if (PositionCondition == ImGuiCond.Always)
            Position = null;
    }

    public override void Draw()
    {
        origin = ImGui.GetWindowPos();
        ImGui.PushStyleColor(ImGuiCol.Button, 0x00352a44);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xff382b46);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xff4c395d);
        try
        {
            if (!settings.LockPosition)
                DrawMoveHandle();

            if (Button("Reset", 184, 6, 48, 21, "Start fresh and clear all encounter values"))
                encounter.Reset();

            DrawSet(SetSlot.First, "1st", 8);
            DrawSet(SetSlot.Second, "2nd", 124);
            DrawShape("Inferno", encounter.InfernoShape, flame, 8, encounter.ToggleInferno);
            DrawShape("Tsunami", encounter.TsunamiShape, wave, 124, encounter.ToggleTsunami);
            Frame(8, 128, 232, 152);
            DrawPosition("Thunder", encounter.ThunderPosition, Glyph.Thunder, 8, 0xff66d3ff, encounter.ToggleThunderPosition);
            DrawPosition("Ice", encounter.IcePosition, Glyph.Ice, 124, 0xffffd688, encounter.ToggleIcePosition);
        }
        finally
        {
            ImGui.PopStyleColor(3);
        }
    }

    private void DrawMoveHandle()
    {
        Button("##Move", 8, 6, 28, 21, "Drag to move the WoL Says overlay");
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
        {
            var viewport = ImGui.GetMainViewport();
            var minimum = viewport.WorkPos;
            var maximum = Vector2.Max(minimum, minimum + viewport.WorkSize - ImGui.GetWindowSize());
            // Apply after drawing so every hit target uses the same origin this frame.
            pendingPosition = Vector2.Clamp(origin + ImGui.GetIO().MouseDelta, minimum, maximum);
        }

        for (int y = 13; y <= 19; y += 3)
            ImGui.GetWindowDrawList().AddLine(Point(16, y), Point(28, y), Muted, scale);
    }

    public override void Update()
    {
        if (pendingPosition is not { } position)
            return;

        Position = position;
        PositionCondition = ImGuiCond.Always;
        pendingPosition = null;
    }

    private void DrawSet(SetSlot slot, string label, int x)
    {
        Frame(x, 30, x + 108, 100);
        ImGui.PushID(label);
        bool? value = encounter.SetValue(slot);
        if (Button(label, x + 5, 32, 98, 20, $"{label} set: {ValueName(value)}. Toggle True/False.", Tone(value)))
            encounter.ToggleSet(slot);
        DrawGlyph(Status(value), x + 77, 42, 11, Tone(value));

        Member visible = encounter.VisibleMembers(slot);
        bool single = visible is Member.Thunder or Member.Water;
        bool unresolved = visible == Encounter.AllMembers;
        int index = 0;
        foreach (Member member in Members)
        {
            if ((visible & member) == 0)
                continue;

            int width = single ? 98 : 47;
            int left = x + 5 + index % 2 * 51;
            int top = unresolved ? 53 + index / 2 * 22 : 64;
            bool? captured = encounter.MemberValue(slot, member);
            if (Button($"##{member}", left, top, width, 21,
                $"{member}: {ValueName(captured)}. Click to {(captured.HasValue ? "clear" : "capture")}."))
                encounter.ToggleMember(slot, member);

            float center = left + width / 2f;
            DrawGlyph(MemberGlyph(member), center - (captured.HasValue ? 5 : 0), top + 10.5f, 17, captured.HasValue ? Tone(captured) : Faded);
            if (captured.HasValue)
                DrawGlyph(Status(captured), center + 9, top + 10.5f, 9, Tone(captured));
            index++;
        }

        ImGui.PopID();
    }

    private void DrawShape(string name, Shape shape, ISharedImmediateTexture emote, int x, Action toggle)
    {
        if (Button($"##{name}", x, 102, 108, 25, $"{name}: {shape}. Toggle AoE/Donut."))
            toggle();

        bool unknown = shape == Shape.Unknown;
        if (emote.TryGetWrap(out var texture, out _))
            ImGui.GetWindowDrawList().AddImage(texture.Handle, Point(x + 32, 103), Point(x + 54, 125),
                Vector2.Zero, Vector2.One, unknown ? 0x99ffffff : 0xffffffff);
        else
            DrawGlyph(name == "Inferno" ? Glyph.Flame : Glyph.Wave, x + 43, 114, 20, unknown ? Muted : Purple);

        var icon = shape switch { Shape.AoE => Glyph.AoE, Shape.Donut => Glyph.Donut, _ => Glyph.Empty };
        DrawGlyph(icon, x + 66, 114, unknown ? 11 : 17, unknown ? Muted : Purple);
    }

    private void DrawPosition(string name, FloorPosition position, Glyph icon, int x, uint inside, Action toggle)
    {
        if (Button($"##{name}Position", x + 1, 129, 106, 22, $"{name} position: {position}. Toggle Inside/Outside."))
            toggle();

        DrawGlyph(icon, x + 54, 140, 17, position == FloorPosition.Inside ? inside : Faded);
    }

    private bool Button(string label, float x, float y, float width, float height, string tooltip, uint? textColor = null)
    {
        ImGui.SetCursorScreenPos(Point(x, y));
        if (textColor.HasValue)
            ImGui.PushStyleColor(ImGuiCol.Text, textColor.Value);
        bool clicked = ImGui.Button(label, new Vector2(width, height) * scale);
        if (textColor.HasValue)
            ImGui.PopStyleColor();
        bool mouseoverHint = !settings.DisableMouseoverHints && ImGui.IsItemHovered();
        bool focusHint = ImGui.GetIO().NavVisible && ImGui.IsItemFocused();
        if (mouseoverHint || focusHint)
            ImGui.SetTooltip(tooltip);
        return clicked;
    }

    private Vector2 Point(float x, float y) => origin + new Vector2(x, y) * scale;

    private void Frame(float x1, float y1, float x2, float y2) =>
        ImGui.GetWindowDrawList().AddRect(Point(x1, y1), Point(x2, y2), Border, 4 * scale);

    private static string ValueName(bool? value) => value?.ToString() ?? "Unknown";
    private static uint Tone(bool? value) => value switch { true => 0xffa1d980, false => 0xffa891ef, null => Muted };
    private static Glyph Status(bool? value) => value switch { true => Glyph.Check, false => Glyph.Cross, null => Glyph.Empty };

    private enum Glyph { Empty, Check, Cross, Thunder, Water, Bomb, Gaze, Ice, AoE, Donut, Flame, Wave }

    private static Glyph MemberGlyph(Member member) => member switch
    {
        Member.Thunder => Glyph.Thunder,
        Member.Water => Glyph.Water,
        Member.Bomb => Glyph.Bomb,
        _ => Glyph.Gaze
    };

    private void DrawGlyph(Glyph glyph, float x, float y, float size, uint color)
    {
        var draw = ImGui.GetWindowDrawList();
        float unit = size * scale / 24;
        Vector2 topLeft = Point(x - size / 2, y - size / 2);
        float stroke = MathF.Max(scale, unit * 1.8f);

        Vector2 At(float px, float py) => topLeft + new Vector2(px, py) * unit;
        void Line(float x1, float y1, float x2, float y2) =>
            draw.AddLine(At(x1, y1), At(x2, y2), color, stroke);
        void Curve(float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4) =>
            draw.AddBezierCubic(At(x1, y1), At(x2, y2), At(x3, y3), At(x4, y4), color, stroke);
        void Circle(float cx, float cy, float radius) =>
            draw.AddCircle(At(cx, cy), radius * unit, color, 0, stroke);

        switch (glyph)
        {
            case Glyph.Empty:
                draw.AddRect(At(2, 2), At(22, 22), color, 2 * scale);
                break;
            case Glyph.Check:
                Line(4, 12, 9, 17);
                Line(9, 17, 21, 5);
                break;
            case Glyph.Cross:
                Line(5, 5, 19, 19);
                Line(19, 5, 5, 19);
                break;
            case Glyph.Thunder:
                Line(13, 2, 4, 14);
                Line(4, 14, 11, 14);
                Line(11, 14, 10, 22);
                Line(10, 22, 20, 9);
                Line(20, 9, 12, 9);
                Line(12, 9, 13, 2);
                break;
            case Glyph.Water:
                Curve(12, 3, 9, 7, 5, 12, 5, 15);
                Curve(5, 15, 5, 24, 19, 24, 19, 15);
                Curve(19, 15, 19, 12, 15, 7, 12, 3);
                break;
            case Glyph.Bomb:
                Circle(10, 14, 7);
                Line(14, 8, 17, 5);
                Line(17, 5, 21, 5);
                Line(19, 1, 19, 3);
                Line(22, 2, 23, 1);
                break;
            case Glyph.Gaze:
                Curve(2, 12, 8, 3, 16, 3, 22, 12);
                Curve(22, 12, 16, 21, 8, 21, 2, 12);
                Circle(12, 12, 3);
                break;
            case Glyph.Ice:
                Line(12, 2, 12, 22);
                Line(3, 7, 21, 17);
                Line(3, 17, 21, 7);
                Line(8, 4, 12, 7);
                Line(12, 7, 16, 4);
                Line(8, 20, 12, 17);
                Line(12, 17, 16, 20);
                break;
            case Glyph.AoE:
                draw.AddCircleFilled(At(12, 12), 8 * unit, color);
                break;
            case Glyph.Donut:
                Circle(12, 12, 9);
                Circle(12, 12, 4);
                break;
            case Glyph.Flame:
                Curve(12, 2, 14, 8, 19, 10, 19, 16);
                Curve(19, 16, 19, 24, 5, 24, 5, 16);
                Curve(5, 16, 5, 12, 9, 10, 12, 2);
                break;
            case Glyph.Wave:
                Curve(2, 18, 9, 18, 4, 3, 19, 5);
                Curve(19, 5, 12, 8, 13, 18, 22, 18);
                Line(2, 21, 22, 21);
                break;
        }
    }
}

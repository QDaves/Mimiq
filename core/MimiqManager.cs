using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Xabbo;
using Xabbo.Messages;
using Xabbo.Messages.Flash;

namespace Mimiq.Core;

public class MimiqManager : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private enum State { Idle, Selecting, Active }

    private readonly GEarthExtension extension;
    private readonly Dictionary<int, int> idToIndex = new();
    private readonly Dictionary<int, int> indexToId = new();
    private readonly Dictionary<int, string> idToName = new();
    private readonly Dictionary<int, string> idToFigure = new();
    private readonly Dictionary<int, string> idToGender = new();
    private readonly Dictionary<int, string> idToMotto = new();

    private State state = State.Idle;
    private int userId = -1;
    private int targetId = -1;
    private int targetIndex = -1;
    private string? targetName;
    private string? activeEffect;

    private bool _figure = true;
    private bool _motto = true;
    private bool _action = true;
    private bool _dance = true;
    private bool _sign = true;
    private bool _effect = true;
    private bool _sit = true;
    private bool _follow;
    private bool _typing = true;
    private bool _talk = true;
    private bool _shout = true;
    private bool _whisper = true;
    private string _buttonText = "Start";
    private string? _targetAvatarUrl;

    public bool Figure { get => _figure; set => Notify(ref _figure, value); }
    public bool Motto { get => _motto; set => Notify(ref _motto, value); }
    public bool Action { get => _action; set => Notify(ref _action, value); }
    public bool Dance { get => _dance; set => Notify(ref _dance, value); }
    public bool Sign { get => _sign; set => Notify(ref _sign, value); }
    public bool Effect { get => _effect; set => Notify(ref _effect, value); }
    public bool Sit { get => _sit; set => Notify(ref _sit, value); }
    public bool Follow { get => _follow; set => Notify(ref _follow, value); }
    public bool Typing { get => _typing; set => Notify(ref _typing, value); }
    public bool Talk { get => _talk; set => Notify(ref _talk, value); }
    public bool Shout { get => _shout; set => Notify(ref _shout, value); }
    public bool Whisper { get => _whisper; set => Notify(ref _whisper, value); }
    public string ButtonText { get => _buttonText; set => Notify(ref _buttonText, value); }
    public string? TargetAvatarUrl { get => _targetAvatarUrl; set => Notify(ref _targetAvatarUrl, value); }

    public MimiqManager(GEarthExtension extension)
    {
        this.extension = extension;
        extension.Intercepted += ProcessPacket;
    }

    private void Notify<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    public void Toggle()
    {
        if (state == State.Idle)
        {
            state = State.Selecting;
            ButtonText = "Cancel";
            TargetAvatarUrl = null;
        }
        else
        {
            ResetTarget();
        }
    }

    private void ResetTarget()
    {
        state = State.Idle;
        targetId = -1;
        targetIndex = -1;
        targetName = null;
        ButtonText = "Start";
        TargetAvatarUrl = null;
    }

    private void ClearRoomData()
    {
        idToIndex.Clear();
        indexToId.Clear();
        idToName.Clear();
        idToFigure.Clear();
        idToGender.Clear();
        idToMotto.Clear();
        activeEffect = null;
        ResetTarget();
    }

    private void ProcessPacket(Intercept intercept)
    {
        try
        {
            if (intercept.Is(In.UserObject))
            {
                HandleUserObject(intercept);
                return;
            }

            if (intercept.Is(In.RoomReady))
            {
                HandleRoomReady();
                return;
            }

            if (intercept.Is(In.Users))
            {
                HandleUsers(intercept);
                return;
            }

            if (state == State.Selecting)
            {
                if (intercept.Is(Out.GetSelectedBadges))
                {
                    HandleTargetSelection(intercept);
                    return;
                }

                if (intercept.Is(Out.LookTo))
                {
                    intercept.Block();
                    return;
                }
            }

            if (state != State.Active || targetIndex == -1)
                return;

            if (intercept.Is(In.UserUpdate))
                HandleUserUpdate(intercept);
            else if (intercept.Is(In.Expression))
                HandleExpression(intercept);
            else if (intercept.Is(In.Dance))
                HandleDance(intercept);
            else if (intercept.Is(In.AvatarEffect))
                HandleAvatarEffect(intercept);
            else if (intercept.Is(In.UserTyping))
                HandleUserTyping(intercept);
            else if (intercept.Is([In.Chat, In.Shout, In.Whisper]))
                HandleChat(intercept);
            else if (intercept.Is(In.UserChange))
                HandleUserChange(intercept);
        }
        catch { }
    }

    private void HandleUserObject(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        userId = intercept.Packet.Read<int>();
    }

    private void HandleRoomReady()
    {
        ClearRoomData();

        if (userId == -1)
            extension.Send(Out.InfoRetrieve);
    }

    private void HandleUsers(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int count = intercept.Packet.Read<int>();

        for (int i = 0; i < count; i++)
        {
            int id = intercept.Packet.Read<int>();
            string name = intercept.Packet.Read<string>();
            string motto = intercept.Packet.Read<string>();
            string figure = intercept.Packet.Read<string>();
            int index = intercept.Packet.Read<int>();
            intercept.Packet.Read<int>();
            intercept.Packet.Read<int>();
            intercept.Packet.Read<string>();
            intercept.Packet.Read<int>();
            int type = intercept.Packet.Read<int>();

            idToIndex[id] = index;
            indexToId[index] = id;
            idToName[id] = name;
            idToFigure[id] = figure;
            idToMotto[id] = motto;

            if (type == 1)
            {
                string gender = intercept.Packet.Read<string>();
                idToGender[id] = gender;

                if (id == targetId && state == State.Active)
                {
                    targetIndex = index;
                    if (Figure)
                        extension.Send(Out.UpdateFigureData, gender, figure);
                    if (Motto)
                        extension.Send(Out.ChangeMotto, motto);
                }

                intercept.Packet.Read<int>();
                intercept.Packet.Read<int>();
                intercept.Packet.Read<string>();
                intercept.Packet.Read<string>();
                intercept.Packet.Read<int>();
                intercept.Packet.Read<bool>();
                intercept.Packet.Read<int>();
            }
            else if (type == 2)
            {
                intercept.Packet.Read<int>();
                intercept.Packet.Read<int>();
                intercept.Packet.Read<string>();
                intercept.Packet.Read<int>();
                intercept.Packet.Read<bool>();
                intercept.Packet.Read<bool>();
                intercept.Packet.Read<bool>();
                intercept.Packet.Read<bool>();
                intercept.Packet.Read<bool>();
                intercept.Packet.Read<bool>();
                intercept.Packet.Read<int>();
                intercept.Packet.Read<string>();
            }
            else if (type == 4)
            {
                intercept.Packet.Read<string>();
                intercept.Packet.Read<int>();
                intercept.Packet.Read<string>();
                int skills = intercept.Packet.Read<int>();
                for (int j = 0; j < skills; j++)
                    intercept.Packet.Read<short>();
            }
        }
    }

    private void HandleTargetSelection(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int clicked = intercept.Packet.Read<int>();

        if (userId == -1 || clicked == userId || !idToIndex.ContainsKey(clicked))
            return;

        targetId = clicked;
        targetIndex = idToIndex[clicked];
        targetName = idToName[clicked];
        state = State.Active;
        ButtonText = "Stop";

        string encodedName = Uri.EscapeDataString(targetName);
        string domain = extension.HotelDomain;
        TargetAvatarUrl = $"https://www.habbo.{domain}/habbo-imaging/avatarimage?user={encodedName}&direction=2&head_direction=2";

        if (Figure && idToFigure.ContainsKey(clicked) && idToGender.ContainsKey(clicked))
            extension.Send(Out.UpdateFigureData, idToGender[clicked], idToFigure[clicked]);

        if (Motto && idToMotto.ContainsKey(clicked))
            extension.Send(Out.ChangeMotto, idToMotto[clicked]);
    }

    private void HandleUserUpdate(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int count = intercept.Packet.Read<int>();

        for (int i = 0; i < count; i++)
        {
            int index = intercept.Packet.Read<int>();
            int x = intercept.Packet.Read<int>();
            int y = intercept.Packet.Read<int>();
            intercept.Packet.Read<string>();
            intercept.Packet.Read<int>();
            intercept.Packet.Read<int>();
            intercept.Packet.Read<int>();
            string actions = intercept.Packet.Read<string>();

            if (index != targetIndex)
                continue;

            if (Follow)
            {
                extension.Send(Out.LookTo, x, y);
                if (actions.Contains("mv"))
                    extension.Send(Out.MoveAvatar, x, y);
            }

            if (Sit)
            {
                if (actions.Contains("sit"))
                    extension.Send(Out.ChangePosture, 1);
                else if (actions == "/flatctrl 4//" || !actions.Contains("sit"))
                    extension.Send(Out.ChangePosture, 0);
            }

            if (Sign && actions.Contains("sign"))
            {
                int start = actions.IndexOf("sign") + 5;
                int end = actions.IndexOf("/", start);
                if (end > start)
                {
                    string value = actions.Substring(start, end - start).Trim();
                    if (int.TryParse(value, out int signId))
                        extension.Send(Out.Sign, signId);
                }
            }
        }
    }

    private void HandleExpression(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int index = intercept.Packet.Read<int>();
        int expressionId = intercept.Packet.Read<int>();

        if (Action && index == targetIndex)
            extension.Send(Out.AvatarExpression, expressionId);
    }

    private void HandleDance(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int index = intercept.Packet.Read<int>();
        int style = intercept.Packet.Read<int>();

        if (Dance && index == targetIndex)
            extension.Send(Out.Dance, style);
    }

    private void HandleAvatarEffect(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int index = intercept.Packet.Read<int>();
        int effectId = intercept.Packet.Read<int>();

        if (index != targetIndex)
            return;

        if (effectId is 140 or 196 or 136)
        {
            string command = effectId switch
            {
                140 => ":habnam",
                196 => ":YYXXABXA",
                136 => ":moonwalk",
                _ => ""
            };

            extension.Send(Out.Chat, command, 0, -1);
            activeEffect = command;
        }
        else if ((effectId == 0 || effectId == -1) && activeEffect != null)
        {
            extension.Send(Out.Chat, activeEffect, 0, -1);
            activeEffect = null;
        }

        if (Effect)
        {
            if (effectId == 0 || effectId == -1)
            {
                extension.Send(Out.AvatarEffectActivated, -1);
                extension.Send(Out.AvatarEffectSelected, -1);
            }
            else if (effectId is not (140 or 196 or 136))
            {
                extension.Send(Out.AvatarEffectActivated, effectId);
                extension.Send(Out.AvatarEffectSelected, effectId);
            }
        }
    }

    private void HandleUserTyping(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int index = intercept.Packet.Read<int>();
        int typingState = intercept.Packet.Read<int>();

        if (Typing && index == targetIndex)
        {
            if (typingState == 1)
                extension.Send(Out.StartTyping);
            else
                extension.Send(Out.CancelTyping);
        }
    }

    private void HandleChat(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int index = intercept.Packet.Read<int>();
        string message = intercept.Packet.Read<string>();
        intercept.Packet.Read<int>();
        int bubble = intercept.Packet.Read<int>();

        if (index != targetIndex)
            return;

        Identifier? outIdentifier = null;

        if (intercept.Is(In.Chat) && Talk)
            outIdentifier = Out.Chat;
        else if (intercept.Is(In.Shout) && Shout)
            outIdentifier = Out.Shout;
        else if (intercept.Is(In.Whisper) && Whisper)
            outIdentifier = Out.Whisper;

        if (outIdentifier.HasValue)
        {
            if (intercept.Is(In.Chat))
                extension.Send(outIdentifier.Value, message, bubble, -1);
            else if (intercept.Is(In.Whisper))
                extension.Send(outIdentifier.Value, $"{targetName} {message}", bubble);
            else
                extension.Send(outIdentifier.Value, message, bubble);
        }
    }

    private void HandleUserChange(Intercept intercept)
    {
        intercept.Packet.Position = 0;
        int index = intercept.Packet.Read<int>();
        string figure = intercept.Packet.Read<string>();
        string gender = intercept.Packet.Read<string>();
        string motto = intercept.Packet.Read<string>();

        if (indexToId.TryGetValue(index, out int id))
        {
            idToFigure[id] = figure;
            idToGender[id] = gender;
            idToMotto[id] = motto;
        }

        if (index == targetIndex)
        {
            if (Figure)
                extension.Send(Out.UpdateFigureData, gender, figure);
            if (Motto)
                extension.Send(Out.ChangeMotto, motto);
        }
    }
}

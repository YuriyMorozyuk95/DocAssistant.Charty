// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Models;

public record class VoicePreferences
{
    private const string s_preferredVoiceKey = "preferred-voice";
    private const string s_preferredSpeedKey = "preferred-speed";
    private const string s_ttsIsEnabledKey = "tts-is-enabled";

    private string? _voice;
    private double? _rate;
    private bool? _isEnabled;

    private readonly ILocalStorageService _storage;

    public VoicePreferences(ILocalStorageService storage) => _storage = storage;

    public string? Voice
    {
        get => _voice ??= _storage.GetItem<string>(s_preferredVoiceKey);
        set
        {
            if (_voice != value && value is not null)
            {
                _voice = value;
                _storage.SetItem<string>(s_preferredVoiceKey, value);
            }
        }
    }

    public double Rate
    {
        get => _rate ??= _storage.GetItem<double>(s_preferredSpeedKey) is double rate
            && rate > 0 ? rate : 1;
        set
        {
            if (_rate != value)
            {
                _rate = value;
                _storage.SetItem<double>(s_preferredSpeedKey, value);
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled ??= (_storage.GetItem<bool?>(s_ttsIsEnabledKey) is { } enabled
            && enabled);
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                _storage.SetItem<bool?>(s_ttsIsEnabledKey, value);
            }
        }
    }

    public void Deconstruct(out string? voice, out double rate, out bool isEnabled) => (voice, rate, isEnabled) = (Voice, Rate, IsEnabled);
}

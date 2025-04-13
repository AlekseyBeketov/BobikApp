#if IOS || MACCATALYST
using AVFoundation;
#endif

namespace BobikApp.Services;

public class TextToSpeechService
{
#if IOS || MACCATALYST
    private readonly AVSpeechSynthesizer synthesizer = new();
#endif

    public void Speak(string text)
    {
#if IOS || MACCATALYST
        var utterance = new AVSpeechUtterance(text)
        {
            Rate = AVSpeechUtterance.DefaultSpeechRate,
            Voice = AVSpeechSynthesisVoice.FromLanguage("ru-RU")
        };
        synthesizer.SpeakUtterance(utterance);
#else
        Console.WriteLine($"Голос: {text}");
#endif
    }

    public void Stop()
    {
#if IOS || MACCATALYST
        synthesizer.StopSpeaking(AVSpeechBoundary.Immediate);
#endif
    }
}
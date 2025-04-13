using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Vosk;

namespace BobikApp.Services;

public class SpeechToTextService
{
    private VoskRecognizer recognizer;
    private Model voskModel;
    private Process micProcess;
    private bool isRecording;
    private StringBuilder recordedText = new();
    private DateTime lastSpeechTime;
    private readonly object recognizerLock = new();
    private const double SilenceSeconds = 1.5;

    public event Action<string>? OnTextFinalized;
    public event Action? OnKeywordDetected;

    private bool isStarted;

    public void Start(string modelPath)
    {
        if (isStarted) return;
        isStarted = true;

        voskModel = new Model(modelPath);
        recognizer = new VoskRecognizer(voskModel, 16000.0f);
        Task.Run(ListenLoop);
    }

    private void ListenLoop()
    {
        micProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-hide_banner -loglevel quiet -f avfoundation -i :default -ar 16000 -ac 1 -f s16le -",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        micProcess.Start();

        byte[] buffer = new byte[4096];
        var stdout = micProcess.StandardOutput.BaseStream;

        while (true)
        {
            int bytesRead = stdout.Read(buffer, 0, buffer.Length);
            if (bytesRead <= 0) continue;

            lock (recognizerLock)
            {
                if (recognizer.AcceptWaveform(buffer, bytesRead))
                {
                    var resultText = JsonDocument.Parse(recognizer.Result())
                        .RootElement.GetProperty("text").GetString();

                    if (!string.IsNullOrWhiteSpace(resultText))
                    {
                        Console.WriteLine("[FULL] " + resultText);

                        if (!isRecording && resultText.Contains("бобик", StringComparison.OrdinalIgnoreCase))
                        {
                            isRecording = true;
                            recordedText.Clear();
                            lastSpeechTime = DateTime.Now;
                            OnKeywordDetected?.Invoke();
                        }

                        if (isRecording)
                        {
                            recordedText.Append(resultText + " ");
                            lastSpeechTime = DateTime.Now;
                        }
                    }
                }
                else
                {
                    var partial = JsonDocument.Parse(recognizer.PartialResult())
                        .RootElement.GetProperty("partial").GetString();

                    if (isRecording && !string.IsNullOrWhiteSpace(partial))
                    {
                        Console.WriteLine("[PARTIAL] " + partial);
                        lastSpeechTime = DateTime.Now;
                    }
                }
            }

            if (isRecording && (DateTime.Now - lastSpeechTime).TotalSeconds > SilenceSeconds)
            {
                isRecording = false;
                string final = recordedText.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(final))
                    OnTextFinalized?.Invoke(final);

                recordedText.Clear();
            }
        }
    }
}

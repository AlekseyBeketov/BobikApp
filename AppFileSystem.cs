namespace BobikApp;

public static class AppFileSystem
{
    public static string AppDataPath { get; }
    public static string StoryFilePath { get; }
    public static string VoskModelsPath { get; }
    public static string VoiceRecordsPath { get; }

    static AppFileSystem()
    {
        AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BobikApp");
        StoryFilePath = Path.Combine(AppDataPath, "story.json");
        VoskModelsPath = Path.Combine(AppDataPath, "Models", "Vosk");
        VoiceRecordsPath = Path.Combine(AppDataPath, "VoiceRecords");

        Directory.CreateDirectory(VoskModelsPath);
        Directory.CreateDirectory(VoiceRecordsPath);
    }
}
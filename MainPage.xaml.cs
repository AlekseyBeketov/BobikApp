using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Vosk;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using BobikApp.Services;
#if IOS || MACCATALYST
using AVFoundation;
using Foundation;
#endif

namespace BobikApp;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
	public ObservableCollection<Message> MessageHistory { get; set; } = new ObservableCollection<Message>();

	private VoskRecognizer recognizer;
	private Model voskModel;
	private Process micProcess;
	private bool isRecording;
	private StringBuilder recordedText = new();
	private DateTime lastSpeechTime;
	
	private readonly object recognizerLock = new();
	private readonly SpeechToTextService speechService = new();
	private readonly TextToSpeechService tts = new();
	
	//private SpeechSynthesizer synthesizer = new SpeechSynthesizer(); #для винды
	// Создаем экземпляр синтезатора речи
#if IOS || MACCATALYST
        private AVSpeechSynthesizer synthesizer = new AVSpeechSynthesizer();
#endif

	private bool _isMuted = false;
	private bool manualTrigger = false;

	private const string BasePrompt =
		"Отвечай как голосовой ассистент, дружелюбно и по делу, но строго не более 650 символов!";

	private const string Ollama_Url = "http://127.0.0.1:11434/api/chat";
	
	private static readonly string VoskModelPath = "/Users/alexbeketov/Developer/BobikApp/voiceModels/vosk-model-small-ru-0.22";


	private static readonly string StoryFilePath = AppFileSystem.StoryFilePath;

	private string _fileVoicePath { get; set; }

	public MainPage()
	{
		_fileVoicePath = Path.Combine(AppFileSystem.VoiceRecordsPath, "записьГолоса.wav");
		InitializeComponent();

		// Инициализация детектора ключевого слова
		MessageEntry.Completed += OnSendButtonClicked;
		_ = LoadMessageHistoryAsync();
		BindingContext = this;
		
		speechService.OnKeywordDetected += () =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				RecordButton.Text = "Говорите...";
			});
		};

		speechService.OnTextFinalized += (text) =>
		{
			MainThread.BeginInvokeOnMainThread(() =>
			{
				MessageEntry.Text = text;
				OnSendButtonClicked(null, null);
				RecordButton.Text = "Начать запись";
			});
		};
		speechService.Start(VoskModelPath);
	}

	private void DownScroll(object? sender, EventArgs? e)
	{
		Dispatcher.DispatchAsync(() =>
		{
			if (MessageHistoryView.ItemsSource != null && MessageHistoryView.ItemsSource.Cast<object>().Any())
			{
				var lastItem = MessageHistoryView.ItemsSource.Cast<object>().Last();
				MessageHistoryView.ScrollTo(lastItem, position: ScrollToPosition.End, animate: true);
			}
		});
	}
	
	private async void OnCopyClicked(object sender, EventArgs e)
	{
		if (sender is ImageButton btn && btn.CommandParameter is string text)
		{
			await Clipboard.Default.SetTextAsync(text);
			await DisplayAlert("Скопировано", "Сообщение скопировано в буфер обмена", "OK");
		}
	}

	private void OnRecordButtonClicked(object? sender, EventArgs? e)
	{
		if (!isRecording)
		{
			isRecording = true;
			recordedText.Clear();
			lastSpeechTime = DateTime.Now;
			manualTrigger = true;

			MainThread.BeginInvokeOnMainThread(() =>
			{
				RecordButton.Text = "Говорите...";
			});
		}
		else
		{
			isRecording = false;

			string finalText = recordedText.ToString().Trim();
			if (!string.IsNullOrWhiteSpace(finalText))
			{
				MessageEntry.Text = finalText;
				OnSendButtonClicked(null, null);
			}

			recordedText.Clear();
			MainThread.BeginInvokeOnMainThread(() =>
			{
				RecordButton.Text = "Начать запись";
			});
		}
	}

    private void SpeakText(string text)
    {
        //if (_isMuted) return;

#if IOS || MACCATALYST
        var speechUtterance = new AVSpeechUtterance(text)
        {
            Rate = AVSpeechUtterance.DefaultSpeechRate,
            Voice = AVSpeechSynthesisVoice.FromLanguage("ru-RU")
        };
        synthesizer.SpeakUtterance(speechUtterance);
#else
	    Console.WriteLine("Необходимо использовать другие библиотеки для синтеза речи.");
#endif
    }

    private void StopSpeech()
    {
#if IOS || MACCATALYST
        synthesizer?.StopSpeaking(AVSpeechBoundary.Immediate);
#endif
    }

    private void OnSendButtonClicked(object? sender, EventArgs? e)
    {
        StopSpeech();
        string text = MessageEntry.Text;
        if (!string.IsNullOrWhiteSpace(text))
        {
            Task.Run(() => SendToOllamaAsync(text));
            MessageEntry.Text = string.Empty;
        }
    }

	private async Task SendToOllamaAsync(string text)
	{
		MainThread.BeginInvokeOnMainThread(() =>
		{
			MessageHistory.Add(new Message { Role = "user", Content = text });
			DownScroll(null, null);
		});

		var messageHistory = await ReadMessageHistoryAsync();
		messageHistory.Add(new { role = "user", content = $"{BasePrompt}" + text });

		using (HttpClient client = new HttpClient())
		{
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var requestBody = new
			{
				//model = "hf.co/Vikhrmodels/Vikhr-Gemma-2B-instruct-GGUF:Q4_K",
				model = "gemma3:12b",
				messages = messageHistory,
				stream = false,
			};

			string jsonRequest = JsonSerializer.Serialize(requestBody);
			var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

			HttpResponseMessage response = await client.PostAsync(Ollama_Url, content);

			if (response.IsSuccessStatusCode)
			{
				string responseText = await response.Content.ReadAsStringAsync();

				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true // Игнорируем регистр
				};

				var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseText, options);

				if (ollamaResponse != null && ollamaResponse.Message?.Content != null)
				{
					string resultText = ollamaResponse.Message.Content.Replace("\n---\n", "");

					MainThread.BeginInvokeOnMainThread(() =>
					{
						MessageHistory.Add(new Message { Role = "assistant", Content = resultText });
						DownScroll(null, null);
					});

					messageHistory.Add(new { role = "assistant", content = resultText });
					await WriteLastMessageAsync(messageHistory);
					tts.Speak(resultText);
				}
				else
				{
					string errorText = "Произошла ошибка, повторите попытку позже";
					messageHistory.Add(new { role = "assistant", content = errorText });
					MainThread.BeginInvokeOnMainThread(() =>
					{
						MessageHistory.Add(new Message { Role = "assistant", Content = errorText });
						DownScroll(null, null);
					});
				}
			}
			else
			{
				string errorText = "Возникла ошибка: " + response.StatusCode;
				messageHistory.Add(new { role = "assistant", content = errorText });

				MainThread.BeginInvokeOnMainThread(() =>
				{
					MessageHistory.Add(new Message { Role = "assistant", Content = errorText });
					DownScroll(null, null);
				});
			}

			//InitVosk();
			speechService?.Start(VoskModelPath);
		}
	}

	private static async Task<List<object>> ReadMessageHistoryAsync()
	{
		try
		{
			if (!File.Exists(StoryFilePath))
			{
				await File.WriteAllTextAsync(StoryFilePath, "[]");
				return new List<object>();
			}

			string json = await File.ReadAllTextAsync(StoryFilePath);

			if (string.IsNullOrWhiteSpace(json))
			{
				await File.WriteAllTextAsync(StoryFilePath, "[]");
				return new List<object>();
			}

			return JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка: {ex.Message}");
			return new List<object>();
		}
	}

	private static async Task WriteLastMessageAsync(List<object> messages)
	{
		// Если сообщений больше 8, оставляем только последние 8
		if (messages.Count > 8)
		{
			messages = messages.Skip(messages.Count - 8).ToList();
		}

		string json = JsonSerializer.Serialize(messages);
		await File.WriteAllTextAsync(StoryFilePath, json);
	}
	
	// Метод для загрузки истории сообщений из файла
	private async Task LoadMessageHistoryAsync()
	{
		var messages = await ReadMessageHistoryAsync();

		foreach (var message in messages)
		{
			if (message is JsonElement jsonElement)
			{
				string role = jsonElement.GetProperty("role").GetString() ?? string.Empty;
				string content = jsonElement.GetProperty("content").GetString() ?? string.Empty;

				MainThread.BeginInvokeOnMainThread(() =>
				{
					MessageHistory.Add(new Message
						{ Role = role, Content = content.Replace(BasePrompt, string.Empty) });
				});
			}
		}
	}
}
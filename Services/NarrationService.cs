using OneSProject.Models;

namespace OneSProject.Services;

public class NarrationService
{
    private readonly ITextToSpeech _tts;
    private int _currentlyProcessingId = -1; // Theo dõi ID đang chuẩn bị phát
    private bool _isProcessing = false;
    private readonly Queue<POITranslation> _audioQueue = new();
    private CancellationTokenSource? _cts;

    public NarrationService()
    {
        _tts = TextToSpeech.Default;
    }

    public async Task AddToQueueAsync(List<POITranslation> translations)
    {
        if (translations == null || translations.Count == 0) return;

        foreach (var translation in translations)
        {
            // CHANGE: the tracker decides re-entry; narration only avoids duplicates already in-flight.
            if (translation.POIId == _currentlyProcessingId ||
                _audioQueue.Any(p => p.POIId == translation.POIId))
            {
                continue;
            }

            _audioQueue.Enqueue(translation);
        }

        if (!_isProcessing)
        {
            await ProcessQueueAsync();
        }
    }

    private async Task ProcessQueueAsync()
    {
        _isProcessing = true;

        while (_audioQueue.Count > 0)
        {
            var current = _audioQueue.Dequeue();
            _currentlyProcessingId = current.POIId;

            await SpeakAsync(current);

            _currentlyProcessingId = -1;
            await Task.Delay(500);
        }

        _isProcessing = false;
    }

    private async Task SpeakAsync(POITranslation translation)
    {
        try
        {
            var locales = await _tts.GetLocalesAsync();
            var locale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(translation.LanguageCode, StringComparison.OrdinalIgnoreCase));

            await _tts.SpeakAsync(translation.AudioScript, new SpeechOptions { Locale = locale });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Error: {ex.Message}");
        }
    }

    public async Task PlayManualAsync(POITranslation translation)
    {
        Stop();

        bool isTtsEnabled = Preferences.Default.Get("IsTtsEnabled", true);
        double volume = Preferences.Default.Get("TtsVolume", 1.0);

        if (!isTtsEnabled) return;

        _audioQueue.Clear();
        _cts = new CancellationTokenSource();

        try
        {
            var locales = await _tts.GetLocalesAsync();
            var locale = locales.FirstOrDefault(l =>
                l.Language.StartsWith(translation.LanguageCode, StringComparison.OrdinalIgnoreCase));

            await _tts.SpeakAsync(translation.DetailedDescription, new SpeechOptions
            {
                Locale = locale,
                Volume = (float)volume
            }, _cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Stop()
    {
        if (_cts != null && !_cts.IsCancellationRequested)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        _audioQueue.Clear();
        _isProcessing = false;
        _currentlyProcessingId = -1;
    }
}

using OneSProject.Models;

public class NarrationService
{
    private readonly ITextToSpeech _tts;
    private int _lastPlayedPoiId = -1;
    private int _currentlyProcessingId = -1; // Theo dõi ID dang chu?n b? phát
    private bool _isProcessing = false;
    private readonly Queue<POITranslation> _audioQueue = new();

    public NarrationService()
    {
        _tts = TextToSpeech.Default;
    }

    public async Task AddToQueueAsync(List<POITranslation> translations)
    {
        if (translations == null || translations.Count == 0) return;

        foreach (var translation in translations)
        {
            // CH?N NGAY L?P T?C: N?u ID dang phát, v?a phát xong, ho?c dã n?m trong hàng d?i
            if (translation.POIId == _lastPlayedPoiId ||
                translation.POIId == _currentlyProcessingId ||
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
            _currentlyProcessingId = current.POIId; // Ðánh d?u dang x? lý ngay khi l?y ra kh?i hàng d?i

            await SpeakAsync(current);

            _lastPlayedPoiId = current.POIId;
            _currentlyProcessingId = -1;

            // Thêm m?t kho?ng ngh? ng?n (500ms) d? Android TTS Engine k?p chuy?n tr?ng thái
            await Task.Delay(500);
        }
        _isProcessing = false;
    }

    private async Task SpeakAsync(POITranslation translation)
    {
        try
        {
            var locales = await _tts.GetLocalesAsync();
            var locale = locales.FirstOrDefault(l => l.Language.StartsWith(translation.LanguageCode, StringComparison.OrdinalIgnoreCase));

            // S? d?ng CancellationToken d? ki?m soát lu?ng n?u c?n (tùy ch?n nâng cao)
            await _tts.SpeakAsync(translation.AudioScript, new SpeechOptions { Locale = locale });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Error: {ex.Message}");
        }
    }

    public async Task PlayManualAsync(POITranslation translation)
    {
        // Kiểm tra cài đặt từ Phase 6.1
        bool isTtsEnabled = Preferences.Default.Get("IsTtsEnabled", true);
        double volume = Preferences.Default.Get("TtsVolume", 1.0);

        if (!isTtsEnabled) return;

        _audioQueue.Clear();
        _lastPlayedPoiId = -1;

        try
        {
            var locales = await _tts.GetLocalesAsync();
            var locale = locales.FirstOrDefault(l => l.Language.StartsWith(translation.LanguageCode, StringComparison.OrdinalIgnoreCase));

            await _tts.SpeakAsync(translation.DetailedDescription, new SpeechOptions
            {
                Locale = locale,
                Volume = (float)volume
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TTS Error: {ex.Message}");
        }
    }
}
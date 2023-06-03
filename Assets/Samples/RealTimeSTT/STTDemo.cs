using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Whisper.Utils;
using Whisper;
using UnityEngine.UI;

public class STTDemo : MonoBehaviour
{
    public WhisperManager whisper;
    public MicrophoneDetection microphoneDetection;
    private string _buffer;

    public Text outputText;
    public Text timeText;
    public bool printLanguage = true;

    private void Start() 
    {
        microphoneDetection.OnRecordStop += Transcribe;
    }

    private async void Transcribe(float[] data, int frequency, int channels, float length)
    {
        

        var sw = new Stopwatch();
        sw.Start();

        var res = await whisper.GetTextAsync(data, frequency, channels);

        var time = sw.ElapsedMilliseconds;
        var rate = length / (time * 0.001f);
        timeText.text = $"Time: {time} ms\nRate: {rate:F1}x";
        if (res == null)
            return;

        var text = res.Result;
        if (printLanguage)
            text += $"\n\nLanguage: {res.Language}";
        _buffer += text + "\n";
        outputText.text = _buffer;
    }
}

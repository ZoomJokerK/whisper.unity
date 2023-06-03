using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

using System;


public class MicrophoneDetection : MonoBehaviour
{
    [Sirenix.OdinInspector.ReadOnly]
    public string deviceName = "Defauilt";
    [Sirenix.OdinInspector.ReadOnly]
    public float loudness = 0; // 麦克风音量

    [Space(20)]
    public int frequency = 44100; // 麦克风采样率
    public float sensitivity = 100; // 麦克风灵敏度
    public int clipLengthLimit = 10; // 单个录制声音的最大长度
    public float soundThreshold = 0.1f; // 声音阈值
    public float checkDelay = 1; // 检测间隔时间
    public Dropdown deviceDropdown; // 麦克风设备选择UI

    
    private float timer = 0;
    private float lastSoundTime = 0; // 最后一次检测到声音的时间
    private float clipStartTime = 0; // 当前录制的声音的开始时间
    private bool isRecording = false; // 是否正在录制声音
    private AudioClip _audioClip;
    public delegate void OnRecordStopDelegate(float[] data, int frequency, int channels, float length);
    public event OnRecordStopDelegate OnRecordStop;
    void Start()
    {
        // 初始化麦克风设备选择UI
        deviceDropdown.ClearOptions();
        foreach (string device in Microphone.devices)
        {
            deviceDropdown.options.Add(new Dropdown.OptionData(device));
        }
        deviceDropdown.value = 0;
        deviceDropdown.RefreshShownValue();
        deviceDropdown.onValueChanged.AddListener
        (
            (x) =>
            {
                OnDeviceDropdownValueChanged();
            });
        // 开始录音
        Record();

    }

    private void Record()
    {
        deviceName = Microphone.devices[deviceDropdown.value];
        if (!Microphone.IsRecording(deviceName))
        {
            _audioClip = Microphone.Start(deviceName, false, clipLengthLimit, frequency);
        }
    }
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > checkDelay/2)
        {
            timer = 0;


            // 计算麦克风音量
            loudness = GetMicVolume() * sensitivity;

            // 如果音量大于阈值，则表示有人在说话
            if (loudness > soundThreshold)
            {
                Debug.Log("Someone is speaking!");

                if (!isRecording)
                {
                    isRecording = true;
                    clipStartTime = Time.time;
                }

                lastSoundTime = Time.time;


            }
            else
            {
                if (isRecording)
                {
                    if (Time.time - lastSoundTime > checkDelay)
                    {
                        //没有声音了 并且经过了一段时间了 我们就停止录制
                        //并且把这段声音存放到voiceList中
                        isRecording = false;

                        //储存
                        // 获取录制的声音数据
                        
                        float[] data = GetTrimmedData();

                        // 触发录制结束事件

                        OnRecordStop?.Invoke(data, _audioClip.frequency, _audioClip.channels, Time.time - clipStartTime);

                    }
                }
                else
                {
                    // 没有声音 并且没有录制的时候 放掉这段音频
                    // 重新开始录制
                    Record();
                }
            }
        }


    }
    float GetMicVolume()
    {
        float[] waveData = new float[1024];
        int micPosition = Microphone.GetPosition(deviceName) - (1024 + 1); // 获取最新的1024个样本
        if (micPosition < 0) return 0;

        _audioClip.GetData(waveData, micPosition);
        float levelMax = 0;
        for (int i = 0; i < 1024; i++)
        {
            float wavePeak = waveData[i] * waveData[i];
            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }
        return Mathf.Sqrt(Mathf.Sqrt(levelMax));
    }


    private float[] GetTrimmedData()
    {
        if (_audioClip == null)
        {
            return new float[0];
        }
        // get microphone samples and current position
        var pos = Microphone.GetPosition(deviceName);
        var origData = new float[_audioClip.samples * _audioClip.channels];
        _audioClip.GetData(origData, 0);

        // check if mic just reached audio buffer end
        if (pos == 0)
            return origData;

        // looks like we need to trim it by pos
        var trimData = new float[pos];
        Array.Copy(origData, trimData, pos);
        return trimData;
    }
    private bool IsVolumeEnough(float[] data, float volume = 0.1f, int checkLength = 1024)
    {
        float[] checkData = new float[checkLength];
        if (data.Length > checkLength)
        {
            Array.Copy(data, checkData, checkLength);
        }
        else
        {
            checkData = data;
        }
        float sum = 0;
        for (int i = 0; i < checkData.Length; i++)
        {
            sum += Mathf.Abs(checkData[i]);
        }
        float average = sum / checkData.Length;
        return average > volume;
    }

    public void OnDeviceDropdownValueChanged()
    {
        // 切换麦克风设备
        string deviceName = Microphone.devices[deviceDropdown.value];
        Microphone.End(deviceName);
        Record();
    }
}

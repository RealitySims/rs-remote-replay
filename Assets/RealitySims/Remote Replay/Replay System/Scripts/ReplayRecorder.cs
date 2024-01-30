using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReplayRecorder : MonoBehaviour
{
    [Tooltip("Time in seconds after which a replay will automatically stop.")]
    [SerializeField] private float _recordingDuration = 90;

    [Tooltip("The of a frame in the replay.")]
    [SerializeField] private float _frameDuration = 1;

    [Tooltip("Camera that will be recorded.")]
    [SerializeField] private Camera _camera = null;

    [Tooltip("Name that is going to be used for the replay.")]
    [SerializeField] private string _replayName = "default";

    [Tooltip("Start replay automatically upon scene load.")]
    [SerializeField] private bool _recordAutomatically = true;

    [Tooltip("Will cache replay localy and if upload failed try to uppload it at next oppertunity.")]
    [SerializeField] private bool _cacheLatestReplay = true;

    private string _currentReplayName;
    private List<ReplayFrame> _replayFrames = null;

    private Queue<string> _messageQueue = new Queue<string>();

    private float _messageShownDuration = 0;

    public bool HasSavedRemoteReplay { get; private set; } = false;
    public string RemoteReplay { get; private set; } = null;
    public bool IsRecording { get; private set; }

    private float _time;
    private Dictionary<string, Func<string>> _customStats = new Dictionary<string, Func<string>>();

    private IEnumerator Start()
    {
        yield return null;
        if (_recordAutomatically)
        {
            StartRecording(_replayName);
        }
    }

    /// <summary>
    /// Start recording the replay.
    /// </summary>
    public void StartRecording(string replayName = null)
    {
        StopRecording();
        StartCoroutine(RecordReplay(replayName ?? _replayName));
    }

    /// <summary>
    /// Stop the recording of a replay.
    /// </summary>
    public void StopRecording()
    {
        if (IsRecording)
        {
            StopAllCoroutines();
            SaveReplay(_replayFrames);
        }
    }

    /// <summary>
    /// Set the camera recorded by the replay.
    /// </summary>
    public void SetCamera(Camera camera)
    {
        _camera = camera;
    }

    /// <summary>
    /// Set the duration after which a recording will automatically stop.
    /// </summary>
    public void SetRecordingDuration(float duration)
    {
        _recordingDuration = duration;
    }

    private IEnumerator RecordReplay(string replayName, bool persistUntilSaved = false)
    {
        _currentReplayName = replayName;
        _replayFrames = new List<ReplayFrame>();
        LogMessage($"Starting Recording: {_currentReplayName}");

        IsRecording = true;

        _time = 0;
        float lastTime = int.MinValue;

        int frameIndex = 0;
        while (true)
        {
            if (_recordingDuration < _time)
            {
                LogMessage("Replay Finished");
                break;
            }

            if (_time - lastTime > _frameDuration)
            {
                RecordFrame(_time);
                frameIndex += 1;
                lastTime = _time;
            }

            yield return null;
            _time += Time.deltaTime;
        }

        IsRecording = false;
        SaveReplay(_replayFrames);
    }

    private void SaveReplay(List<ReplayFrame> replayFrames)
    {
        var replayData = new ReplayData(_currentReplayName, replayFrames, _frameDuration);
        replayData.Save(RemoteSaveComplete, _cacheLatestReplay, logHandler: LogMessage);
    }

    private void RemoteSaveComplete(string name)
    {
        Debug.Log($"<color=blue>Replay Remotely Saved</color>: {name}");
        RemoteReplay = name;
        HasSavedRemoteReplay = true;
    }

    private void RecordFrame(float time)
    {
        ReplayFrame frame = new ReplayFrame()
        {
            Time = time,
            Camera = GetCameraObject(),
            Objects = GetObjects().ToArray(),
            Stats = GenerateCustomStats(),
        };
        _replayFrames.Add(frame);
    }

    private Dictionary<string, string> GenerateCustomStats()
    {
        Dictionary<string, string> dict = new();

        foreach (var pair in _customStats)
        {
            dict[pair.Key] = pair.Value();
        }

        return dict;
    }

    private ReplayObject GetCameraObject()
    {
        Camera cam = _camera ? _camera : Camera.main;
        if (cam == null) { return new ReplayObject(); }

        return new ReplayObject()
        {
            Position = cam.transform.position,
            Rotation = cam.transform.eulerAngles,
            id = 0,
            name = cam.name,
        };
    }

    private IEnumerable<ReplayObject> GetObjects()
    {
        foreach (ReplayRecordable recordable in FindObjectsOfType<ReplayRecordable>())
        {
            yield return recordable.GetReplayObject();
        }
    }

    private void OnGUI()
    {
        if (!Application.isEditor) { return; }
        DrawRecordingUI();
        DrawMessageUI();
    }

    private void DrawMessageUI()
    {
        if (_messageQueue.Count > 0)
        {
            if (_messageShownDuration > 5)
            {
                _messageQueue.Dequeue();
                _messageShownDuration = 0;
                return;
            }
            _messageShownDuration += Time.unscaledDeltaTime;
            string message = _messageQueue.Peek();

            // Set the style for the message
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.alignment = TextAnchor.MiddleCenter;
            style.normal.textColor = Color.white;
            style.wordWrap = true;

            // Position for the message
            Rect position = new Rect(0, 50, Screen.width, 100);

            // Draw the message
            GUI.Label(position, message, style);
        }
    }

    private void DrawRecordingUI()
    {
        if (IsRecording)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.red;

            Rect position = new Rect(10, 10, 200, 30);

            GUI.Label(position, "[REC]", style);

            style.normal.textColor = Color.white;
            position.y += 30;

            // Draw the label
            GUI.Label(position, $"{ReplayViewer.FormatTimeElapsed(_time)}", style);
        }
    }

    public void LogMessage(string msg)
    {
        Debug.Log($"<color=yellow>ReplayRecorder</color>: {msg}");
        _messageQueue.Enqueue(msg);
    }

    internal void RecordCustomStat(string key, Func<string> statFetcher)
    {
        _customStats[key] = statFetcher;
    }
}
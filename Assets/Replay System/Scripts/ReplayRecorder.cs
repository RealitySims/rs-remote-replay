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

    [Tooltip("Start replay automatically upon scene load.")]
    [SerializeField] private bool _recordAutomatically = true;

    [SerializeField] private bool _cacheReplay = true;
    private List<ReplayFrame> _replayFrames = null;

    private Queue<string> _messageQueue = new Queue<string>();

    private float _messageShownDuration = 0;

    public bool HasSavedRemoteReplay { get; private set; } = false;
    public string RemoteReplay { get; private set; } = null;
    public bool IsRecording { get; private set; }

    private float _time;

    private void Start()
    {
        if (_recordAutomatically)
        {
            StartRecording();
        }
    }

    /// <summary>
    /// Start recording the replay.
    /// </summary>
    public void StartRecording()
    {
        StopRecording();
        StartCoroutine(RecordReplay());
    }

    /// <summary>
    /// Stop the recording of a replay.
    /// </summary>
    private void StopRecording()
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

    private void Log(string message)
    {
        Debug.Log($"ReplayRecorder: {message}");
    }

    private IEnumerator RecordReplay(bool persistUntilSaved = false)
    {
        _replayFrames = new List<ReplayFrame>();
        ShowUIMessage("Starting Recording");

        IsRecording = true;

        _time = 0;
        float lastTime = int.MinValue;

        int frameIndex = 0;
        while (true)
        {
            if (_recordingDuration < _time)
            {
                ShowUIMessage("Replay Finished");
                break;
            }

            if (_time - lastTime > _frameDuration)
            {
                RecordFrame(_time, frameIndex);
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
        var replayData = new ReplayData(replayFrames, _frameDuration);
        replayData.Save(RemoteSaveComplete, _cacheReplay);
    }

    private void RemoteSaveComplete(string name)
    {
        Debug.Log($"<color=blue>Replay Remotely Saved</color>: {name}");
        RemoteReplay = name;
        HasSavedRemoteReplay = true;
    }

    private void RecordFrame(float time, int index)
    {
        Log($"Recording Frame {time} {index}");

        ReplayFrame frame = new ReplayFrame()
        {
            Time = time,
            //           PlayerLevel = Player.Ins.CurrentLevel,
            //           PlayerHealth = Player.Ins.Health.Percent,
            //           LevelProgress = Player.Ins.GetCurrentExperienceProgressRatio(),
            //           Player = Player.Ins.GetReplayObject(),
            Camera = GetCameraObject(),
            Objects = GetObjects().ToArray(),
            Upgrades = new Dictionary<int, int>(),
        };
        _replayFrames.Add(frame);
    }

    private ReplayObject GetCameraObject()
    {
        Camera cam = _camera ? _camera : Camera.main;
        if (cam == null) { return new ReplayObject(); }

        return new ReplayObject()
        {
            Position = cam.transform.position,
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
            style.normal.textColor = Color.white;

            // Position for the message
            Rect position = new Rect(Screen.width / 2 - 100, 50, 200, 100);

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
            GUI.Label(position, $"{ReplayPlayer.FormatTimeElapsed(_time)}", style);
        }
    }

    public void ShowUIMessage(string msg)
    {
        _messageQueue.Enqueue(msg);
    }
}
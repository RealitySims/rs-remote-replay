using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VersionControl;
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

    [Tooltip("Will cache replay localy and if upload failed try to uppload it at next oppertunity.")]
    [SerializeField] private bool _cacheLatestReplay = true;
    private List<ReplayFrame> _replayFrames = null;

    private Queue<string> _messageQueue = new Queue<string>();

    private float _messageShownDuration = 0;
    private string _replayName = "default";

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
    public void StartRecording(string replayName = "default")
    {
        StopRecording();
        StartCoroutine(RecordReplay(replayName));
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

    private IEnumerator RecordReplay(string replayName, bool persistUntilSaved = false)
    {
        _replayName = replayName;
        _replayFrames = new List<ReplayFrame>();
        LogMessage("Starting Recording");

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
        var replayData = new ReplayData(_replayName, replayFrames, _frameDuration);
        replayData.Save(RemoteSaveComplete, _cacheLatestReplay, logHandler: LogMessage);
    }

    private void RemoteSaveComplete(string name)
    {
        Debug.Log($"<color=blue>Replay Remotely Saved</color>: {name}");
        RemoteReplay = name;
        HasSavedRemoteReplay = true;
    }

    private void RecordFrame(float time, int index)
    {

        ReplayFrame frame = new ReplayFrame()
        {
            Time = time,
            //           PlayerLevel = Player.Ins.CurrentLevel,
            //           PlayerHealth = Player.Ins.Health.Percent,
            //           LevelProgress = Player.Ins.GetCurrentExperienceProgressRatio(),
            //           Player = Player.Ins.GetReplayObject(),
            Camera = GetCameraObject(),
            Objects = GetObjects().ToArray(),
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
            GUI.Label(position, $"{ReplayPlayer.FormatTimeElapsed(_time)}", style);
        }
    }

    public void LogMessage(string msg)
    {
        Debug.Log($"<color=yellow>ReplayRecorder</color>: {msg}");
        _messageQueue.Enqueue(msg);
    }
}
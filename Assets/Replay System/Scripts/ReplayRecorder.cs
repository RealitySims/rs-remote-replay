using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ReplayRecorder : MonoBehaviour
{
    [SerializeField] private float _recordingDuration = 90;
    [SerializeField] private float _frameDuration = 1;
    [SerializeField] private float _startTime = 0;
    [SerializeField] private bool _cacheReplay = true;

    [SerializeField] private Camera _camera = null;

    private List<ReplayFrame> _replayFrames = null;
    private bool _isRecording;

    public bool HasSavedRemoteReplay { get; private set; } = false;
    public string RemoteReplay { get; private set; } = null;

    private void Start()
    {
        StartCoroutine(RecordReplay());
    }

    private void StopRecording()
    {
        if (_isRecording)
        {
            StopAllCoroutines();
            SaveReplay(_replayFrames);
        }
    }

    public void Log(string message)
    {
        Debug.Log($"ReplayRecorder: {message}");
    }

    private IEnumerator RecordReplay(bool persistUntilSaved = false)
    {
        float gameTime = Time.time;
        _replayFrames = new List<ReplayFrame>();
        Log("Starting Recording");

        _isRecording = true;

        float time = 0;
        float lastTime = int.MinValue;
        int frameIndex = 0;
        while (true)
        {
            if (gameTime < _startTime)
            {
                yield return null;
                continue;
            }

            time += Time.deltaTime;
            if (_recordingDuration < time)
            {
                Log("Replay Finished");
                break;
            }

            if (time - lastTime > _frameDuration)
            {
                RecordFrame(gameTime, frameIndex);
                frameIndex += 1;
                lastTime = time;
            }

            yield return null;
        }

        _isRecording = false;
        SaveReplay(_replayFrames);
    }

    private void SaveReplay(List<ReplayFrame> replayFrames)
    {
        var replayData = new ReplayData(replayFrames, _frameDuration);
        replayData.Save(RemoteSaveComplete, _cacheReplay);
    }

    public void RemoteSaveComplete(string name)
    {
        Debug.Log($"<color=blue>Replay Remotely Saved</color>: {name}");
        RemoteReplay = name;
        HasSavedRemoteReplay = true;
    }

    public void RecordFrame(float time, int index)
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

    public IEnumerable<ReplayObject> GetObjects()
    {
        foreach (ReplayRecordable recordable in FindObjectsOfType<ReplayRecordable>())
        {
            yield return recordable.GetReplayObject();
        }
    }
}
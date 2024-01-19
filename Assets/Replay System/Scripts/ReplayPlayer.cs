using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ReplayPlayer : MonoBehaviour
{
    [SerializeField] private bool _useRealprefabs = false;
    [SerializeField] private ReplayObjectBehaviour _objectPrefab = null;
    [SerializeField] private Camera _camera = null;

    [SerializeField] private TMPro.TMP_Text _frameText = null;
    [SerializeField] private TMPro.TMP_Text _playerLevel = null;
    [SerializeField] private TMPro.TMP_Text _timeText = null;
    [SerializeField] private Image _levelProgress = null;
    [SerializeField] private Image _healthFill = null;
    [SerializeField] private Slider _frameSlider = null;
    [SerializeField] private TMPro.TMP_InputField _remoteReplayName = null;

    [SerializeField] private GameObject _playButton = null;
    [SerializeField] private GameObject _pauseButton = null;

    private int _frame = 0;
    private ReplayData _replay;

    private Dictionary<int, ReplayObjectBehaviour> _idToBehaviour = new Dictionary<int, ReplayObjectBehaviour>();
    private HashSet<int> _usedBehaviours;
    private Coroutine _playCoroutine;
    private int _uniqueID = -1;

    public int Frame
    {
        get => _frame;
        set
        {
            _frame = Mathf.Clamp(value, 0, _replay.replayFrames.Length - 1);
            _frameText.SetText($"{_frame}/{_replay.replayFrames.Length - 1}");
            _frameSlider.SetValueWithoutNotify(value);
        }
    }

    public void Start()
    {
        _replay = ReplayData.LoadLocal();
        InitializeReplay();
    }

    public void LoadRemoteReplay()
    {
        Debug.Log($"<color=yellow>Trying to load replay</color>: {_remoteReplayName.text}");
        ReplayData.LoadRemote(_remoteReplayName.text, (ReplayData data) =>
        {
            _replay = data;

            Debug.Log($"<color=green>Replay Successfully Loaded</color>: {_remoteReplayName.text}");

            InitializeReplay();
        });
    }

    private void InitializeReplay()
    {
        Frame = 0;
        DrawFrame(0);
        SetPlayState(false);

        _frameSlider.minValue = 0;
        _frameSlider.maxValue = _replay.replayFrames.Length - 1;

        _frameSlider.onValueChanged.AddListener((float f) =>
        {
            PauseReplay();
            Frame = Mathf.RoundToInt(f);
            DrawFrame(Frame);
        });
    }

    private void Update()
    {
        if (_idToBehaviour.ContainsKey(0))
        {
            Vector3 target = _idToBehaviour[0].transform.position - transform.forward * 10;

            _camera.transform.position = Vector3.Lerp(_camera.transform.position, target, Time.deltaTime * 2);
        }
    }

    public void PlayReplay()
    {
        PauseReplay();
        _playCoroutine = StartCoroutine(PlayCoroutine());

        IEnumerator PlayCoroutine()
        {
            SetPlayState(true);
            while (true)
            {
                DrawFrame(Frame);
                yield return new WaitForSecondsRealtime(_replay.frameDuration);

                if (Frame == _replay.replayFrames.Length - 1)
                {
                    break;
                }
                Frame++;
            }
            _playCoroutine = null;
            SetPlayState(false);
        }
    }

    public void PauseReplay()
    {
        SetPlayState(false);
        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
        }
    }

    public void DrawNextFrame()
    {
        PauseReplay();
        if (Frame >= _replay.replayFrames.Length - 1)
        {
            return;
        }
        Frame += 1;

        DrawFrame(_replay.replayFrames[Frame]);
    }

    public void DrawPreviousFrame()
    {
        PauseReplay();
        if (Frame <= 0)
        {
            return;
        }
        Frame -= 1;
        DrawFrame(_replay.replayFrames[Frame]);
    }

    public void DrawFrame(int index)
    {
        index = Mathf.Clamp(index, 0, _replay.replayFrames.Length);
        DrawFrame(_replay.replayFrames[index]);
    }

    private void SetPlayState(bool value)
    {
        _playButton.SetActive(!value);
        _pauseButton.SetActive(value);
    }

    private int GetUniqueId()
    {
        return _uniqueID--;
    }

    private void DrawFrame(ReplayFrame frame)
    {
        _usedBehaviours = new HashSet<int>();

        DrawObject(frame.Player);

        foreach (var obj in frame.Objects)
        {
            if (obj.id == 0) { obj.id = GetUniqueId(); }
            DrawObject(obj);
        }

        // update UI
        _playerLevel.SetText($"{frame.PlayerLevel}");
        _levelProgress.fillAmount = frame.LevelProgress;
        _healthFill.fillAmount = frame.PlayerHealth;
        _timeText.SetText(FormatTimeElapsed(frame.Time));

        // remoe unused
        var unusedBehaviours = _idToBehaviour.Keys.Except(_usedBehaviours);
        foreach (var unusedID in unusedBehaviours.ToArray())
        {
            Destroy(_idToBehaviour[unusedID].gameObject);
            _idToBehaviour.Remove(unusedID);
        }
    }

    public void DrawObject(ReplayObject obj)
    {
        if (obj == null) { { Debug.LogWarning("Replay Object Null"); } return; }
        {
            
        }
        if (_idToBehaviour.ContainsKey(obj.id))
        {
            var behaviour = _idToBehaviour[obj.id];
            behaviour.UpdatePosition(obj, _replay.frameDuration);
        }
        else
        {
            var ins = Instantiate<ReplayObjectBehaviour>(_objectPrefab, transform);
            ins.Initialize(obj, _useRealprefabs);
            _idToBehaviour[obj.id] = ins;
        }

        _usedBehaviours.Add(obj.id);
    }

    public static string FormatTimeElapsed(float timeElapsed)
    {
        var min = Mathf.Floor(timeElapsed / 60);
        var sec = Mathf.Floor(timeElapsed % 60);
        return $"{min:00}:{sec:00}";
    }
}
using Firebase.Extensions;
using Firebase.Storage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
internal class ReplayData
{
    public float frameDuration = 1;

    public ReplayFrame[] replayFrames;

    private const string LAST_REPLAY_KEY = "lastReplay";
    private const string WAS_LAST_REPLAY_SENT_KEY = "lastReplaySent";
    private const string LAST_REPLAY_ID_KEY = "lastReplayID";

    public ReplayData(IEnumerable<ReplayFrame> replayFrames, float frameDuration)
    {
        this.replayFrames = replayFrames.ToArray();
        this.frameDuration = frameDuration;
    }

    public static ReplayData LoadLocal()
    {
        var json = PlayerPrefs.GetString(LAST_REPLAY_KEY);

        ReplayData data = JsonConvert.DeserializeObject<ReplayData>(json);
        return data;
    }

    internal static void LoadRemote(string name, Action<ReplayData> onCompleted)
    {
        DownloadFileFromFirebaseStorage(name).ContinueWithOnMainThread((task =>
        {
            if (task.Status != TaskStatus.RanToCompletion)
            {
                return;
            }
            if (task.Result == null)
            {
                return;
            }

            var json = DecompressString(task.Result);
            ReplayData data = JsonConvert.DeserializeObject<ReplayData>(json);
            onCompleted(data);
        }));
    }

    public void Save(Action<string> remoteSaveSuccessful, bool cacheReplay)
    {
        var settings = new JsonSerializerSettings();
        settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        string json = JsonConvert.SerializeObject(this, Formatting.Indented, settings);

        int replayID = 0;
        Debug.LogWarning("no game sessions id");

        PlayerPrefs.SetString(LAST_REPLAY_KEY, json);
        PlayerPrefs.SetString(WAS_LAST_REPLAY_SENT_KEY, cacheReplay ? false.ToString() : true.ToString());
        PlayerPrefs.SetInt(LAST_REPLAY_ID_KEY, replayID);

        SaveToFirebase(json, replayID, remoteSaveSuccessful);
    }

    private static void SaveToFirebase(string json, int replayId, Action<string> remoteSaveSuccessful)
    {
        string data = CompressString(json);
        UploadReplayToFirebase(data, replayId, remoteSaveSuccessful);
    }

    public static void UploadReplayToFirebase(string content, int replayId, Action<string> remoteSaveSuccessful)
    {
        string userID = FirebaseManager.AnonymousID;
        if (userID == "-")
        {
            Debug.LogError("No authenticated user found.");
        }
        StorageReference replaysRef = GetReplayRef();

        string fileName = $"{userID}_{replayId}.replay";
        StorageReference replayRef = replaysRef.Child(fileName);

        Debug.Log($"Attempting to upload replay: {fileName}");

        // Convert the string to a byte array
        byte[] contentBytes = Encoding.UTF8.GetBytes(content);

        MetadataChange metadata = new MetadataChange();
        metadata.ContentType = "replay"; // Set the appropriate content type
        metadata.CustomMetadata = new Dictionary<string, string>() {
            { "aid", userID },
        };

        // Upload the byte array
        replayRef.PutBytesAsync(contentBytes, metadata)
            .ContinueWithOnMainThread((Task<StorageMetadata> task) =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Upload failed: " + task.Exception);
                    // Handle the error...
                }
                else
                {
                    Debug.Log("Upload completed successfully");
                    remoteSaveSuccessful(fileName);
                    PlayerPrefs.SetString(WAS_LAST_REPLAY_SENT_KEY, true.ToString());
                    // Handle the success...
                    FirebaseManager.Ins.LogReplayUploaded(fileName, replayId);
                }
            });
    }

    public static async Task<string> DownloadFileFromFirebaseStorage(string fileName)
    {
        StorageReference storageRef = GetReplayRef().Child(fileName);

        const long maxAllowedSize = 1 * 1024 * 1024; // For example, 1MB limit
        try
        {
            byte[] fileBytes = await storageRef.GetBytesAsync(maxAllowedSize).ConfigureAwait(false);
            return Encoding.UTF8.GetString(fileBytes);
        }
        catch (StorageException e)
        {
            Debug.LogError("Error occurred while downloading the file: " + e);
            return null;
        }
    }

    private static StorageReference GetReplayRef()
    {
        FirebaseStorage storage = FirebaseStorage.DefaultInstance;

        StorageReference storageRef = storage.RootReference;
        StorageReference replaysRef = storageRef.Child("replays");
        return replaysRef;
    }

    public static string CompressString(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        using (var output = new MemoryStream())
        {
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
            {
                gzip.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(output.ToArray());
        }
    }

    public static string DecompressString(string compressedStr)
    {
        var compressedBytes = Convert.FromBase64String(compressedStr);

        using (var input = new MemoryStream(compressedBytes))
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        using (var reader = new StreamReader(gzip, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    public static void AttemptToSendCachedReplay()
    {
        if (PlayerPrefs.HasKey(WAS_LAST_REPLAY_SENT_KEY) && PlayerPrefs.GetString(WAS_LAST_REPLAY_SENT_KEY) == false.ToString())
        {
            Debug.Log($"<color=yellow>Attempting to send replay from cache.</color>");
            string json = PlayerPrefs.GetString(LAST_REPLAY_KEY);
            int id = PlayerPrefs.GetInt(LAST_REPLAY_ID_KEY, 0);
            SaveToFirebase(json, id, (string value) => { Debug.Log($"<color=green>Replay successfully sent from cache</color>: {value}"); });
        }
    }
}
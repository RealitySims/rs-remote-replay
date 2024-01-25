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

    public string replayName = "default";

    public ReplayFrame[] replayFrames;

    private const string LAST_REPLAY_KEY = "lastReplay";
    private const string WAS_LAST_REPLAY_SENT_KEY = "wasLastReplaySent";
    private const string LAST_REPLAY_ID_KEY = "lastReplayID";

    public ReplayData(string replayName, IEnumerable<ReplayFrame> replayFrames, float frameDuration)
    {
        this.replayName = replayName;
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

    public void Save(Action<string> remoteSaveSuccessful, bool cacheReplay, Action<string> logHandler = null)
    {
        Task.Run(() =>
        {
            try
            {
                // Perform JSON serialization
                var settings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                string json = JsonConvert.SerializeObject(this, Formatting.Indented, settings);
                return CompressString(json);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error during serialization: " + ex.Message);
                throw; // Rethrow the exception to be caught in ContinueWith
            }
        })
        .ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Handle or log the exception
                Exception ex = task.Exception.Flatten();
                Debug.LogError("Task encountered an error: " + ex.Message);
                // Optionally, invoke a callback or propagate the exception
            }
            else
            {
                // Process the result and perform Unity API calls
                string data = task.Result;
                PlayerPrefs.SetString(LAST_REPLAY_KEY, data);
                PlayerPrefs.SetString(WAS_LAST_REPLAY_SENT_KEY, cacheReplay ? false.ToString() : true.ToString());
                PlayerPrefs.SetString(LAST_REPLAY_ID_KEY, replayName);

                SaveToFirebase(data, replayName, remoteSaveSuccessful, logHandler);
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    private static void SaveToFirebase(string json, string replayId, Action<string> remoteSaveSuccessful, Action<string> logHandler = null)
    {
        logHandler ??= Debug.Log;
        string data = CompressString(json);
        UploadReplayToFirebase(data, replayId, remoteSaveSuccessful, logHandler);
    }

    public static void UploadReplayToFirebase(string content, string replayId, Action<string> remoteSaveSuccessful, Action<string> logHandler = null)
    {
        string userID = FirebaseManager.AnonymousID;
        if (userID == "-")
        {
            logHandler?.Invoke($"No authenticated user found.");
            return;
        }

        StorageReference replaysRef = GetReplayRef();

        string fileName = $"{userID}_{replayId}.replay";
        StorageReference replayRef = replaysRef.Child(fileName);

        logHandler?.Invoke($"Attempting to upload replay: {fileName}");

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
                    logHandler?.Invoke("Upload failed: " + task.Exception);
                    // Handle the error...
                }
                else
                {
                    logHandler?.Invoke("Upload completed successfully");
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
            string name = PlayerPrefs.GetString(LAST_REPLAY_ID_KEY, "default");
            SaveToFirebase(json, name, (string value) => { Debug.Log($"<color=green>Replay successfully sent from cache</color>: {value}"); });
        }
    }
}
#if !UNITY_WEBGL

using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

#endif

public class FirebaseManager : MonoBehaviour
{
    public static string AnonymousID { get; private set; } = "-";

    public static FirebaseManager Ins { get; private set; }

#if !UNITY_WEBGL
    private FirebaseApp app;
    private bool _isFirebaseReady;

    private static string SAVE_FILE => Application.persistentDataPath + "/firebase_persistence.json";

    private void Awake()
    {
        Ins = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(async task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                app = Firebase.FirebaseApp.DefaultInstance;

                // Set a flag here to indicate whether Firebase is ready to use by your app.
                _isFirebaseReady = true;

                SignInAnonymously();
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
                _isFirebaseReady = false;
            }
        });
    }

    private static void SignInAnonymously()
    {
        Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInAnonymouslyAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                return;
            }

            Firebase.Auth.AuthResult result = task.Result;
            AnonymousID = result.User.UserId;
            Debug.LogFormat("User signed in successfully: {0} ({1})", result.User.DisplayName, result.User.UserId);

            try
            {
                ReplayData.AttemptToSendCachedReplay();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        });
    }

    public void LogReplayUploaded(string fileName, string name)
    {
        if (!_isFirebaseReady) { return; }

        var parameters = new List<Parameter>
        {
            new Parameter("file_name", fileName),
            new Parameter("replay_name", name)
        };
        FirebaseAnalytics.LogEvent("replay_uploaded", parameters.ToArray());
    }

#endif
}
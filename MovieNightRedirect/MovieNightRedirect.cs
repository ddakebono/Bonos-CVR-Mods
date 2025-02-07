using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ABI_RC.Core.UI;
using ABI_RC.Systems.GameEventSystem;
using ABI_RC.VideoPlayer.Scripts.Players.AvPro;
using BTKUILib;
using BTKUILib.UIObjects.Components;
using HarmonyLib;
using MelonLoader;
using RenderHeads.Media.AVProVideo;
using UnityEngine;

namespace MovieNightRedirect
{
    public static class BuildInfo
    {
        public const string Name = "MovieNightRedirect";
        public const string Author = "DDAkebono";
        public const string Company = "BTKDevelopment";
        public const string Version = "1.2.1";
    }
    
    public class MovieNightRedirect : MelonMod
    {
        public static string RedirectMNToFolder;
        public static MelonLogger.Instance Log;

        private static bool _is3DModeReady;
        private static GameObject _3dScreen;
        private static GameObject _2dScreen;
        public static ToggleButton Try3DSource;
        private static HttpClient _client;
        private static string _currentOriginalMovieURL;
        private static string _current3DMovieURL;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            
            if (RegisteredMelons.Any(x => x.Info.Name.Equals("BTKCompanionLoader", StringComparison.OrdinalIgnoreCase)))
            {
                Log.Msg("Hold on a sec! Looks like you've got BTKCompanion installed, this mod is built in and not needed!");
                Log.Error("MovieNightRedirect has not started up! (BTKCompanion Running)");
                return;
            }

            //Setup Misc UI
            var movieNightCat = QuickMenuAPI.MiscTabPage.AddCategory("Movie Night");
            Try3DSource = movieNightCat.AddToggle("Try 3D Source", "Enabling this will set Movie Night to look for a 3D version of the movie source!", false);
            Try3DSource.OnValueUpdated += Toggle3DSearch;
            
            Log.Msg("Setting up Movie Night Redirect!");
            
            var videoRedirectDir = new DirectoryInfo("MovieNight");
            
            if(!videoRedirectDir.Exists)
                videoRedirectDir.Create();

            RedirectMNToFolder = videoRedirectDir.Name;

            ApplyPatches(typeof(AvProPlayerPatch));

            CVRGameEventSystem.Instance.OnConnected.AddListener(OnInstanceConnected);
            CVRGameEventSystem.Instance.OnConnectionRecovered.AddListener(OnInstanceConnected);

            Log.Msg($"Movie Night redirect ready! (Target: {videoRedirectDir.FullName})");
        }

        public static void Toggle3DMode(bool sbsMode)
        {
            if (!_is3DModeReady) return;

            _3dScreen.SetActive(sbsMode);
            _2dScreen.SetActive(!sbsMode);
        }

        public static void Toggle3DSearch(bool enabled)
        {
            Log.Msg("Enabling 3D Search, checking if this source has a 3D variant...");

            if (enabled)
            {
                MelonCoroutines.Start(MovieSearchCoroutine());
            }
            else
            {
                if(!string.IsNullOrWhiteSpace(_currentOriginalMovieURL))
                    AvProPlayerPatch.LastInstance.SetUrl(_currentOriginalMovieURL);
                Toggle3DMode(false);
            }
        }

        public static IEnumerator MovieSearchCoroutine()
        {
            //Check if 3D source doesn't 404
            if (_client == null)
            {
                _client = new HttpClient();
                _client.DefaultRequestHeaders.Add("User-Agent", "MovieNightRedirect");
            }

            if (AvProPlayerPatch.LastUrl.Equals(_currentOriginalMovieURL, StringComparison.InvariantCultureIgnoreCase) ||
                AvProPlayerPatch.LastUrl.Equals(_current3DMovieURL, StringComparison.InvariantCultureIgnoreCase))
            {
                AvProPlayerPatch.LastInstance.SetUrl(_current3DMovieURL);
                Toggle3DMode(true);
                Log.Msg("Current 3D URL is the same, firing SetUrl without checking!");
            }

            _currentOriginalMovieURL = AvProPlayerPatch.LastUrl;

            Log.Msg("Checking for 3D source....");

            Task<HttpResponseMessage> sourceCheck = _client.GetAsync(AvProPlayerPatch.LastUrl.Replace(".mp4", "3D.mp4"), HttpCompletionOption.ResponseHeadersRead);

            while (!sourceCheck.IsCompleted)
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (sourceCheck.Result.StatusCode == HttpStatusCode.OK)
            {
                Log.Msg("3D Source Detected! Replacing URL for player!");
                var newUrl = AvProPlayerPatch.LastUrl.Replace(".mp4", "3D.mp4");
                _current3DMovieURL = newUrl;
                AvProPlayerPatch.LastInstance.SetUrl(newUrl);
                Toggle3DMode(true);
            }
            else
            {
                Log.Msg("No 3D source detected");
                Toggle3DMode(false);
            }
        }

        private void OnInstanceConnected(string _)
        {
            _is3DModeReady = false;

            GameObject check3d = GameObject.Find("3DReadyNight");

            if (!check3d.activeSelf) return;

            Log.Msg("Found 3D Ready Flag! Grabbing gameobjects and preparing!");

            _3dScreen = GameObject.Find("VideoAndControls/Video & Speakers/ScreenParent/Screen3D");
            _2dScreen = GameObject.Find("VideoAndControls/Video & Speakers/ScreenParent/Screen169");

            if (_3dScreen != null && _2dScreen != null)
            {
                _is3DModeReady = true;
                Log.Msg("3D and 2D screens found, world is setup correctly!");
            }

            CohtmlHud.Instance.ViewDropText("Movie Night", "3D Source Ready!", "Open your QuickMenu to enable the 3D Movie Source!");
            QuickMenuAPI.ShowConfirm("Movie Night", "Movie Night has detected that this world is ready to show a 3D Movie! Would you like to attempt to grab the 3D movie source?", () =>
            {
                Toggle3DSearch(true);
                Try3DSource.ToggleValue = true;
                QuickMenuAPI.ShowAlertToast("Trying 3D Source, your video will likely reload!");
            });
        }

        private void ApplyPatches(Type type)
        {
            try
            {
                HarmonyInstance.PatchAll(type);
            }
            catch (Exception e)
            {
                Log.Error($"Failed while patching {type.Name}!");
                Log.Error(e);
            }
        }
    }
    
    [HarmonyPatch(typeof(AvProPlayer))]
    class AvProPlayerPatch 
    {
        private static FieldInfo _stateBufferingField = typeof(AvProPlayer).GetField("_stateBuffering", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _videoTitleField = typeof(AvProPlayer).GetField("_videoTitle", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _videoUrlField = typeof(AvProPlayer).GetField("_videoUrl", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _isLivestream = typeof(AvProPlayer).GetField("_isLivestream", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _videoPlayer = typeof(AvProPlayer).GetField("_videoPlayer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo _lastVideoTime = typeof(AvProPlayer).GetField("_lastVideoTime", BindingFlags.Instance | BindingFlags.NonPublic);

        private static MethodInfo _cleanPlayers = typeof(AvProPlayer).GetMethod("CleanupVideoPlayers", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo _invokeEvent = typeof(AvProPlayer).GetMethod("InvokeEvent", BindingFlags.Instance | BindingFlags.NonPublic);

        public static AvProPlayer LastInstance;
        public static string LastUrl;

        private static Regex _fileMatcher = new(@"\/(?<file>\w*.mp4)", RegexOptions.Compiled);
        
        [HarmonyPatch(nameof(AvProPlayer.SetUrl))]
        [HarmonyPrefix]
        static bool SetUrlPrefix(AvProPlayer __instance, ref string url, bool isPaused = false)
        {
            try
            {
                string path;
                LastInstance = __instance;
                LastUrl = url;

                if (url.StartsWith("http://bmn-res-od.potato.moe/vrc"))
                {
                    path = url.Replace("http://bmn-res-od.potato.moe/vrc", MovieNightRedirect.RedirectMNToFolder);
                }
                else if (url.StartsWith("https://cdn.malthbern.com/Personal/Movies"))
                {
                    path = url.Replace("https://cdn.malthbern.com/Personal/Movies", MovieNightRedirect.RedirectMNToFolder);
                }
                else
                {
                    var match = _fileMatcher.Match(url);
                    if (match.Success)
                    {
                        path = MovieNightRedirect.RedirectMNToFolder + "/" + match.Groups["file"].Value;
                    }
                    else
                    {
                        return true;
                    }
                }


                if (MovieNightRedirect.Try3DSource.ToggleValue && !url.EndsWith("3D.mp4"))
                {
                    //Start the search coroutine
                    MelonCoroutines.Start(MovieNightRedirect.MovieSearchCoroutine());
                }

                if (!MovieNightRedirect.Try3DSource.ToggleValue)
                {
                    MovieNightRedirect.Toggle3DMode(false);
                }

                if (File.Exists(path))
                {
                    MovieNightRedirect.Log.Msg($"Found local movie night file for {path}! Replacing online link!");
                    var oldUrl = url;
                    url = path;

                    _lastVideoTime.SetValue(__instance, 0.0);

                    _stateBufferingField.SetValue(__instance, true);
                    _cleanPlayers.Invoke(__instance, null);
                    _videoTitleField.SetValue(__instance, oldUrl.Split('/').Last());
                    _videoUrlField.SetValue(__instance, url);
                    _isLivestream.SetValue(__instance, false);
                    _invokeEvent.Invoke(__instance, new object[] { ABI_RC.VideoPlayer.Scripts.Events.EventType.VideoMetaDataReady });
                    MediaPlayer videoPlayer = (MediaPlayer)_videoPlayer.GetValue(__instance);
                    videoPlayer.OpenMedia(MediaPathType.RelativeToProjectFolder, url, !isPaused);
                    _stateBufferingField.SetValue(__instance, false);

                    return false;
                }
            }
            catch (Exception e)
            {
                MovieNightRedirect.Log.Error(e);
            }

            return true;
        }
    }
}
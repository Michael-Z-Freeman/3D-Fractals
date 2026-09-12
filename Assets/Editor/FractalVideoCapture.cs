using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace FractalLab.Editor
{
    public static class FractalVideoCapture
    {
        private const int FrameRate = 30;
        private const int FrameCount = 300;
        private static RecorderController controller;
        private static RecorderControllerSettings controllerSettings;
        private static MovieRecorderSettings movieSettings;

        [MenuItem("FractalLab/Capture Rotating Ray-Marched Fractal MP4")]
        public static void CaptureFromMenu()
        {
            Debug.Log(Start());
        }

        public static string Start()
        {
            if (!EditorApplication.isPlaying)
                return "Enter Play Mode before recording.";
            if (controller != null && controller.IsRecording())
                return "A FractalLab recording is already running.";

            var outputDirectory = Path.Combine(Application.dataPath, "..", "Captures");
            Directory.CreateDirectory(outputDirectory);
            var outputStem = Path.Combine(outputDirectory, "FractalLab_Raymarched_Rotating");

            controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.SetRecordModeToFrameInterval(0, FrameCount);
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            controllerSettings.FrameRate = FrameRate;
            controllerSettings.CapFrameRate = true;
            controllerSettings.ExitPlayMode = false;

            movieSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieSettings.OutputFile = outputStem;
            movieSettings.CaptureAudio = false;
            movieSettings.CaptureAlpha = false;
            movieSettings.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4
            };
            movieSettings.ImageInputSettings = new CameraInputSettings
            {
                Source = ImageSource.MainCamera,
                OutputWidth = 1920,
                OutputHeight = 1080,
                CaptureUI = false
            };

            controllerSettings.AddRecorderSettings(movieSettings);
            controller = new RecorderController(controllerSettings);
            controller.PrepareRecording();
            if (!controller.StartRecording())
            {
                Cleanup();
                return "Recorder failed to start; inspect the Unity Console.";
            }

            EditorApplication.update += FinishWhenComplete;
            return $"Recording {FrameCount} frames at {FrameRate} FPS to {outputStem}.mp4";
        }

        private static void FinishWhenComplete()
        {
            if (controller == null || controller.IsRecording())
                return;

            EditorApplication.update -= FinishWhenComplete;
            controller.StopRecording();
            Debug.Log("FractalLab MP4 capture completed: Captures/FractalLab_Raymarched_Rotating.mp4");
            Cleanup();
        }

        private static void Cleanup()
        {
            controller = null;
            if (movieSettings != null) Object.DestroyImmediate(movieSettings);
            if (controllerSettings != null) Object.DestroyImmediate(controllerSettings);
            movieSettings = null;
            controllerSettings = null;
        }
    }
}

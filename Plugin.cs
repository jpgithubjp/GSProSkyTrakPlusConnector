using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace SkytrakOpenAPI
{
    [BepInPlugin("GSProSkyTrakPlusConnector", "GSProSkyTrakPlusConnector", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static GSPApi api = new();
        internal static ManualLogSource Log;

        public void Awake()
        {
            Plugin.Log = base.Logger;
            Log.LogInfo("Plugin GSProSkyTrakPlusConnector is loaded!");

            Harmony harmony = new("com.skytrak.openapi");
            MethodBase calculateFlightTrajectory = AccessTools.Method(typeof(CBallFlightManager), "CalculateFlightTrajectory",
            [
                typeof(Vector3),
                typeof(double),
                typeof(double),
                typeof(double),
                typeof(double),
                typeof(double)
            ], null);
            MethodInfo postFixPatch_CalculateFlightTrajectory = AccessTools.Method(typeof(Plugin.Patch), "PostFix_CalculateFlightTrajectory", null, null);

            Log.LogInfo("Applying Patch");
            harmony.Patch(calculateFlightTrajectory, null, new HarmonyMethod(postFixPatch_CalculateFlightTrajectory), null, null, null);
            Log.LogInfo("Plugin Patched");

            Plugin.StartSocketConnectThread();
        }

        public static void StartSocketConnectThread()
        {
            Plugin.Log.LogInfo("Starting SocketConnectThread");
            new Thread(new ThreadStart(Plugin.api.ConnectToGSP)).Start();
        }

        [HarmonyPatch]
        public class Patch
        {
            private const double RADIANS_TO_DEGREES = 57.29577951308232;

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CBallFlightManager), "CalculateFlightTrajectory")]
            public static void PostFix_CalculateFlightTrajectory(object[] __args)
            {
                Plugin.Log.LogInfo("Got a shot from SkyTrak+!");

                double ballSpeedMPH = (double)__args[1];
                double verticalLaunchAngle = (double)__args[2];
                double horizontalLaunchAngle = (double)__args[5];
                double backSpin = (double)__args[3];
                double sideSpin = (double)__args[4] * -1.0;
                double totalSpin = Math.Abs(Math.Sqrt(Math.Pow(backSpin, 2.0) + Math.Pow(sideSpin, 2.0)));
                double spinAxisDegrees = Math.Asin(sideSpin / totalSpin) * RADIANS_TO_DEGREES;

                Plugin.Log.LogInfo("BallSpeedMPH: " + ballSpeedMPH.ToString());
                Plugin.Log.LogInfo("VLA: " + verticalLaunchAngle.ToString());
                Plugin.Log.LogInfo("HLA: " + horizontalLaunchAngle.ToString());
                Plugin.Log.LogInfo("BackSpin: " + backSpin.ToString());
                Plugin.Log.LogInfo("SideSpin: " + sideSpin.ToString());
                Plugin.Log.LogInfo("TotalSpin: " + totalSpin.ToString());
                Plugin.Log.LogInfo("SpinAxisDeg: " + spinAxisDegrees.ToString());

                GSPShotData gspshotData = new()
                {
                    DeviceID = "OpenApi",
                    Units = "Yards",
                    APIversion = "1",
                    BallData = new GSPBallData
                    {
                        Speed = (float)ballSpeedMPH,
                        SpinAxis = (float)spinAxisDegrees,
                        TotalSpin = (float)totalSpin,
                        BackSpin = (float)backSpin,
                        SideSpin = (float)sideSpin,
                        HLA = (float)horizontalLaunchAngle,
                        VLA = (float)verticalLaunchAngle
                    },
                    ShotDataOptions = new GSPShotDataOptions
                    {
                        ContainsBallData = true,
                        ContainsClubData = false,
                        LaunchMonitorIsReady = false,
                        LaunchMonitorBallDetected = false,
                        IsHeartBeat = false
                    }
                };

                Plugin.Log.LogInfo("Sending Shot Data to GSPro: " + JsonConvert.SerializeObject(gspshotData));
                Plugin.api.SendToGSP(JsonConvert.SerializeObject(gspshotData));
            }
        }
    }
}

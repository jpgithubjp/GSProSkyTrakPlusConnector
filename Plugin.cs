using System;
using System.Reflection;
using System.Threading;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using Security;
using SkyTrakWrapper;
using UnityEngine;

namespace SkytakOpenAPI
{
    [BepInPlugin("SkytrakOpenApi_b5", "SkytrakOpenApi_b5", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public void Awake()
        {
            Plugin.Log = base.Logger;
            base.Logger.LogInfo("Plugin SkytrakOpenApi_b5 is loaded!");
            Harmony harmony = new Harmony("com.skytrak.openapi");
            MethodBase methodBase = AccessTools.Method(typeof(CBallFlightManager), "CalculateFlightTrajectory", new Type[]
            {
                typeof(Vector3),
                typeof(double),
                typeof(double),
                typeof(double),
                typeof(double),
                typeof(double)
            }, null);
            MethodInfo methodInfo = AccessTools.Method(typeof(Plugin.Patch), "Postfix0", null, null);
            base.Logger.LogInfo("Applying Patch");
            harmony.Patch(methodBase, null, new HarmonyMethod(methodInfo), null, null, null);
            base.Logger.LogInfo("Plugin Patched");
            Plugin.StartSocketConnectThread();
        }

        public static void StartSocketConnectThread()
        {
            Plugin.Log.LogInfo("Starting SocketConnectThread");
            new Thread(new ThreadStart(Plugin.api.ConnectToGSP)).Start();
        }

        public static GSPApi api = new GSPApi();
        internal static ManualLogSource Log;
        public static EONIOHDNGND sTWrapper0;
        public static AOMKMFBGCGG sTWrapper1;
        public static bool puttingMode;

        [HarmonyPatch]
        public class Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(CBallFlightManager), "CalculateFlightTrajectory")]
            public static void Postfix0(object[] __args)
            {
                Plugin.Log.LogInfo("Got a normal shot!");
                double num = (double)__args[1];
                double num2 = (double)__args[2];
                double num3 = (double)__args[5];
                double num4 = (double)__args[3];
                double num5 = (double)__args[4] * -1.0;
                double num6 = Math.Abs(Math.Sqrt(Math.Pow((double)__args[3], 2.0) + Math.Pow((double)__args[4], 2.0)));
                double num7 = Math.Asin(num5 / num6) * 57.29577951308232;
                Plugin.Log.LogInfo("BallSpeedMPH: " + num.ToString());
                Plugin.Log.LogInfo("VLA: " + num2.ToString());
                Plugin.Log.LogInfo("HLA: " + num3.ToString());
                Plugin.Log.LogInfo("BackSpin: " + num4.ToString());
                Plugin.Log.LogInfo("SideSpin: " + num5.ToString());
                Plugin.Log.LogInfo("TotalSpin: " + num6.ToString());
                Plugin.Log.LogInfo("SpinAxisDeg: " + num7.ToString());
                GSPShotData gspshotData = new GSPShotData();
                gspshotData.DeviceID = "OpenApi";
                gspshotData.Units = "Yards";
                gspshotData.APIversion = "1";
                gspshotData.BallData = new GSPBallData
                {
                    Speed = (float)num,
                    SpinAxis = (float)num7,
                    TotalSpin = (float)num6,
                    BackSpin = (float)num4,
                    SideSpin = (float)num5,
                    HLA = (float)num3,
                    VLA = (float)num2
                };
                gspshotData.ShotDataOptions = new GSPShotDataOptions
                {
                    ContainsBallData = true,
                    ContainsClubData = false,
                    LaunchMonitorIsReady = false,
                    LaunchMonitorBallDetected = false,
                    IsHeartBeat = false
                };
                Plugin.Log.LogInfo("Sending BallData to GSPro: " + JsonConvert.SerializeObject(gspshotData));
                Plugin.api.SendToGSP(JsonConvert.SerializeObject(gspshotData));
            }
        }
    }
}

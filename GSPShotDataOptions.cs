﻿namespace SkytrakOpenAPI
{
    public class GSPShotDataOptions
    {
        public bool ContainsBallData { get; set; }
        public bool ContainsClubData { get; set; }
        public bool LaunchMonitorIsReady { get; set; }
        public bool LaunchMonitorBallDetected { get; set; }
        public bool IsHeartBeat { get; set; }
    }
}

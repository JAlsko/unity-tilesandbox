using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[Serializable]
public class TimeSetting {
    public string name;
    [Range(0, 1)]
    public float timeOccurrence = 0f;
    public Color ambientLightColor;
}

[RequireComponent(typeof(WorldController))]
[RequireComponent(typeof(LightController))]
public class WorldTimeHandler : MonoBehaviour
{
    WorldController wCon;
    LightController lCon;

    public float dayLengthInMinutes;
    public float dayLengthInSeconds;
    float timeScale = 1f;

    public List<TimeSetting> timeSettings = new List<TimeSetting>();

    public float elapsedTime = 0f;
    public int upcomingTimeSetting = 0;
    public int nextTimeSetting = 1;

    void Start()
    {
        wCon = GetComponent<WorldController>();
        lCon = GetComponent<LightController>();

        CheckTimeSettings();
        dayLengthInSeconds = dayLengthInMinutes * 60f;
    }

    void SortTimeSettings() {
        List<TimeSetting> sortedTimeSettings = timeSettings.OrderBy(o=>o.timeOccurrence).ToList();
        timeSettings = sortedTimeSettings;
    }

    void CheckTimeSettings() {
        List<float> occupiedTimes = new List<float>();
        foreach (TimeSetting ts in timeSettings) {
            if (occupiedTimes.Contains(ts.timeOccurrence)) {
                Debug.Log("Time setting '" + ts.name + "' has a duplicate time to another time setting! Removing from time settings!");
                timeSettings.Remove(ts);
            }
        }

        //If we have no time settings or just one, don't increase elapsed time, just set to existing time setting
        if (timeSettings.Count <= 1) {
            timeScale = 0;
            if (timeSettings.Count == 1) {
                UseTimeSetting(0);
            }
        } 
        //Otherwise sort the time settings by time occurrence
        else {
            SortTimeSettings();
        }
    }

    void Update()
    {
        if (timeScale == 0)
            return;

        elapsedTime = (elapsedTime + (Time.deltaTime * timeScale)) % dayLengthInSeconds;

        if (nextTimeSetting == 0) {
            if (elapsedTime > timeSettings[upcomingTimeSetting].timeOccurrence * dayLengthInSeconds) {
                UseTimeSetting(upcomingTimeSetting);
                upcomingTimeSetting = (upcomingTimeSetting + 1) % timeSettings.Count;
                nextTimeSetting = (upcomingTimeSetting + 1) % timeSettings.Count;
            }
        }

        //Continue to next time setting
        if (elapsedTime > timeSettings[upcomingTimeSetting].timeOccurrence * dayLengthInSeconds && elapsedTime < timeSettings[nextTimeSetting].timeOccurrence * dayLengthInSeconds) {
            UseTimeSetting(upcomingTimeSetting);
            upcomingTimeSetting = (upcomingTimeSetting + 1) % timeSettings.Count;
            nextTimeSetting = (upcomingTimeSetting + 1) % timeSettings.Count;
        }
    }

    void UseTimeSetting(int timeSettingIndex) {
        lCon.UpdateSkylight(timeSettings[timeSettingIndex].ambientLightColor);
    }
}

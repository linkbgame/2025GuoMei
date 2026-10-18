using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
public class TimeManager : Singleton2<TimeManager>
{
    private int gameSecond, gameMinute, gameHour, gameDay, gameMonth, gameYear;

    public bool gameClockPause;
    private float tikTime;

    public TimeSpan GameTime => new TimeSpan(gameHour, gameMinute, gameSecond);
    private void OnEnable()
    {
        EventHandler.BeforeSceneUnloadEvent += OnBeforeSceneUnloadEvent;
        EventHandler.AfterSceneLoadedEvent += OnAfterSceneLoadedEvent;
        EventHandler.UpdateGameStateEvent += OnUpdateGameStateEvent;
        EventHandler.StartNewGameEvent += OnStartNewGameEvent;
        EventHandler.EndGameEvent += OnEndGameEvent;
    }

    private void OnDisable()
    {
        EventHandler.BeforeSceneUnloadEvent -= OnBeforeSceneUnloadEvent;
        EventHandler.AfterSceneLoadedEvent -= OnAfterSceneLoadedEvent;
        EventHandler.UpdateGameStateEvent -= OnUpdateGameStateEvent;
        EventHandler.StartNewGameEvent -= OnStartNewGameEvent;
        EventHandler.EndGameEvent -= OnEndGameEvent;
    }



    private void Start()
    {
        // 【修复】单场景架构：默认初始化时间并启动时钟
        NewGameTime();
        gameClockPause = false;

        Debug.Log($"[TimeManager] Start() 初始化完成，时钟已启动。初始时间: {gameHour:D2}:{gameMinute:D2}, Day={gameDay}");
    }





    private void Update()
    {
        if (!gameClockPause)
        {
            tikTime += Time.deltaTime;

            if (tikTime >= Settings.secondThreshold)
            {
                tikTime -= Settings.secondThreshold;
                UpdateGameTime();
            }
        }

        if (Input.GetKey(KeyCode.T))
        {
            for (int i = 0; i < 60; i++)
            {
                UpdateGameTime();
            }
        }
    }

    private void OnEndGameEvent()
    {
        gameClockPause = true;
    }
    private void OnStartNewGameEvent(int obj)
    {
        NewGameTime();
        gameClockPause = false;  // 【修复】启动游戏时开启时钟
    }

    private void NewGameTime()
    {
        gameSecond = 0;
        gameMinute = 0;
        gameHour = 7;
        gameDay = 1;
        gameMonth = 1;
        gameYear = 2022;
    }

    private void OnUpdateGameStateEvent(GameState gameState)
    {
        gameClockPause = gameState == GameState.Pause;
    }

    private void OnAfterSceneLoadedEvent()
    {
        // gameClockPause = false;
        EventHandler.CallGameDateEvent(gameHour, gameDay, gameMonth, gameYear);
        EventHandler.CallGameMinuteEvent(gameMinute, gameHour, gameDay);
    }

    private void OnBeforeSceneUnloadEvent()
    {
        // gameClockPause = true;
    }

    private void UpdateGameTime()
    {
        gameSecond++;
        if (gameSecond > Settings.secondHold)
        {
            gameMinute++;
            gameSecond = 0;

            // 【修复】每分钟触发事件，通知NPC检查日程
            EventHandler.CallGameMinuteEvent(gameMinute, gameHour, gameDay);

            if (gameMinute > Settings.minuteHold)
            {
                gameHour++;
                gameMinute = 0;

                // 【修复】每小时也触发一次，确保整点日程不遗漏
                EventHandler.CallGameMinuteEvent(gameMinute, gameHour, gameDay);

                if (gameHour > Settings.hourHold)
                {
                    gameDay++;
                    gameHour = 0;

                    // 每天触发日期事件
                    EventHandler.CallGameDateEvent(gameHour, gameDay, gameMonth, gameYear);

                    if (gameDay > Settings.dayHold)
                    {
                        gameDay = 1;
                        gameMonth++;

                        if (gameMonth > 12)
                            gameMonth = 1;
                    }
                }
            }
        }
    }
}
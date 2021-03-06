﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections;

using Firmata.NET;

namespace p121029_KinectWatagashi
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        //public const bool WITH_ARDUINO = false;
        public const bool WITH_ARDUINO = true;
        public const int WIDTH = 960;
        public const int HEIGHT = 720;
        const string PORT = "COM3";

        enum CONTROLLER_DEVICE
        {
            KINECT, 
            GAMEPAD
        }

        // コントローラーの設定
        //CONTROLLER_DEVICE controllerDevice = CONTROLLER_DEVICE.GAMEPAD;
        CONTROLLER_DEVICE controllerDevice = CONTROLLER_DEVICE.KINECT;

        enum PHASE
        {
            INITIALIZING, WAITING, FINDING_USER, CALIBRATING,
            BEFORE_PLAY, PLAYING, AFTER_PLAY
        }
        PHASE phase;

        ControllerDevice c;
        DispatcherTimer dispatcherTimer;
        Judge j;
        ArrayList fallingRects;

        Arduino arduino;

        public MainWindow()
        {

            /*
             * 初期化関連
             */

            phase = PHASE.INITIALIZING;

            switch(controllerDevice)
            {
                case CONTROLLER_DEVICE.KINECT:
                    c = new Kinect(this);
                    break;
                case CONTROLLER_DEVICE.GAMEPAD:
                    // ゲームパッドの初期化
                    c = new GamePad(this);
                    break;
            }

            j = new Judge();
            j.FallenBottom += new Judge.FallenBottomEventHandler(j_FallenBottom);
            InitializeComponent();
            c.start();
            fallingRects = new ArrayList();

            if (WITH_ARDUINO)
            {
                arduino = new Arduino(PORT, 57600);
                arduino.pinMode(8, Arduino.OUTPUT);
                arduino.pinMode(9, Arduino.OUTPUT);
                arduino.pinMode(10, Arduino.OUTPUT);
                arduino.pinMode(13, Arduino.OUTPUT);
            }
            else
            {
                arduino = null;
            }

            /*
             * テスト
             */

            FallingRect f = new FallingRect(this, 500);
            fallingRects.Add(f);

            phase = PHASE.PLAYING;

            /*
             * タイマーにイベントを登録して0.05秒ごとにイベントを実行する
             */

            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(500000); // 500000（ゼロが5つで20分の1秒）
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            /*
             * スペースキーで開始、停止
             */

            KeyDown += new KeyEventHandler(MainWindow_KeyDown);
        }

        void j_FallenBottom(object sender, FallenBottomEventArgs e)
        {
            FallingRect f = new FallingRect(this, e.FallenX);
            f.state = FallingRect.STATE.NORMAL;
            fallingRects.Add(f);
            
        }

        void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            switch (phase)
            {
                case PHASE.PLAYING:
                    for ( int i = 0; i < fallingRects.Count; i++ )
                    {
                        FallingRect f = (FallingRect)fallingRects[i];
                        j.doJudge(c.getLeftTop(), c.getRightTop(), f, arduino);
                        f.update();
                    }
                break;
            }
        }

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (dispatcherTimer.IsEnabled)
                {
                    dispatcherTimer.Stop();
                }
                else
                {
                    dispatcherTimer.Start();
                }
            }
        }

        public void Window_Closing(object sender,
            System.ComponentModel.CancelEventArgs e)
        {
            c.stop();
        }
    }
}

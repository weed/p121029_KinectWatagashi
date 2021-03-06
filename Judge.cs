﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;

using Firmata.NET;

namespace p121029_KinectWatagashi
{
    /*
     * 衝突判定
     */

    public class FallenBottomEventArgs : EventArgs
    {
        public int FallenX;
        public FallingRect FallingRect;
    }

    class Judge
    {
        public Judge()
        {
            Debug.WriteLine("Judge instance was initialized.");
        }

        //デリゲートの宣言
        //ここでは"RectHasFallenToBottom"というイベントデリゲートを宣言する
        public delegate void FallenBottomEventHandler(object sender, FallenBottomEventArgs e);
    
        //イベントデリゲートの宣言
        public event FallenBottomEventHandler FallenBottom;

        protected virtual void OnFallenBottom(FallenBottomEventArgs e)
        {
            if (FallenBottom != null)
            {
                FallenBottom(this, e);
            }
        }

        public void doJudge(Point leftTop, Point rightTop, FallingRect fallingRect, Arduino arduino)
        {
            if (fallingRect.state == FallingRect.STATE.NORMAL || fallingRect.state == FallingRect.STATE.FALLING_AROUND_YOU)
            {

                // 輪っかのy軸は人物の高さより低く、かつ下端より高い
                // 人物＜輪っか＜端
                if (leftTop.Y <= fallingRect.Y & fallingRect.Y <= MainWindow.HEIGHT)
                {
                    // 輪っかが人物の右側に引っかかっている
                    // 
                    //        --------------------
                    //     ++++++++++
                    //     ++++++++++
                    //     ++++++++++
                    // 
                    if (leftTop.X <= fallingRect.X & fallingRect.X <= rightTop.X)
                    {
                        // 輪っかの内側から体が左に押した場合
                        // 
                        //         ++++++++++
                        //        --------------------
                        //         ++++++++++
                        //         ++++++++++
                        // 
                        //              ↓
                        // 
                        //     ++++++++++
                        //        --------------------
                        //     ++++++++++
                        //     ++++++++++
                        // 
                        if (fallingRect.state == FallingRect.STATE.FALLING_AROUND_YOU)
                        {
                            // 左へ飛ぶ
                            fallingRect.state = FallingRect.STATE.FLYING_LEFT;
                            Debug.WriteLine("*****Falling Rect is flying left*****");
                        }
                        // 輪っかの外側から体が右に押した場合
                        // 
                        //     ++++++++++
                        //     ++++++++++ --------------------
                        //     ++++++++++
                        //     ++++++++++
                        // 
                        //              ↓
                        // 
                        //         ++++++++++
                        //               --------------------
                        //         ++++++++++
                        //         ++++++++++
                        // 
                        else
                        {
                            // 右へ飛ぶ
                            fallingRect.state = FallingRect.STATE.FLYING_RIGHT;
                            Debug.WriteLine("*****Falling Rect is flying right*****");
                        }
                    }
                    // 人物が輪っかの中に入っている
                    // 
                    //         ++++++++++
                    //    --------------------
                    //         ++++++++++
                    //         ++++++++++
                    // 
                    else if (fallingRect.X <= leftTop.X & rightTop.X <= fallingRect.X + fallingRect.width)
                    {
                        fallingRect.state = FallingRect.STATE.FALLING_AROUND_YOU;
                        Debug.WriteLine("*****Falling Rect is flying around you*****");
                    }
                    // 輪っかが人物の左側に引っかかっている
                    // 
                    //  --------------------
                    //                   ++++++++++
                    //                   ++++++++++
                    //                   ++++++++++
                    // 
                    else if (leftTop.X <= fallingRect.X + fallingRect.width & fallingRect.X + fallingRect.width <= rightTop.X)
                    {
                        // 輪っかの内側から体が右に押した場合
                        // 
                        //          ++++++++++
                        // --------------------
                        //          ++++++++++
                        //          ++++++++++
                        // 
                        //              ↓
                        // 
                        //            ++++++++++
                        // --------------------
                        //            ++++++++++
                        //            ++++++++++
                        // 
                        if (fallingRect.state == FallingRect.STATE.FALLING_AROUND_YOU)
                        {
                            // 右へ飛ぶ
                            fallingRect.state = FallingRect.STATE.FLYING_RIGHT;
                            Debug.WriteLine("*****Falling Rect is flying right*****");
                        }
                        // 輪っかの外側から体が左に押した場合
                        // 
                        //                      ++++++++++
                        // -------------------- ++++++++++
                        //                      ++++++++++
                        //                      ++++++++++
                        // 
                        //              ↓
                        // 
                        //                    ++++++++++
                        // --------------------
                        //                    ++++++++++
                        //                    ++++++++++
                        // 
                        else
                        {
                            // 左へ飛ぶ
                            fallingRect.state = FallingRect.STATE.FLYING_LEFT;
                            Debug.WriteLine("*****Falling Rect is flying left*****");
                        }
                    }
                }
                // 輪っかが下端に達して、輪っかの中に入っている場合
                else if (MainWindow.HEIGHT < fallingRect.Y & fallingRect.state == FallingRect.STATE.FALLING_AROUND_YOU)
                {
                    fallingRect.state = FallingRect.STATE.SCORING;
                    Debug.WriteLine("*****Scoring*****");
                    switch (fallingRect.color)
                    {
                        case FallingRect.COLOR.RED:
                            // 赤のザラメを出す
                            if (arduino != null)
                            {
                                arduino.digitalWrite(8, Arduino.HIGH);
                            }
                            Debug.WriteLine("*****RED*****");
                            break;
                        case FallingRect.COLOR.GREEN:
                            // 緑のザラメを出す
                            if (arduino != null)
                            {
                            arduino.digitalWrite(9, Arduino.HIGH);
                            }
                            Debug.WriteLine("*****GREEN*****");
                            break;
                        case FallingRect.COLOR.BLUE:
                            // 青のザラメを出す
                            if (arduino != null)
                            {
                            arduino.digitalWrite(10, Arduino.HIGH);
                            }
                            Debug.WriteLine("*****BLUE*****");
                            break;
                    }
                }
                // 輪っかが下端を通り越して見えなくなった場合
                else if (MainWindow.HEIGHT + FallingRect.FALLING_RECTANGLE_DEPTH < fallingRect.Y)
                {
                    fallingRect.state = FallingRect.STATE.DISAPPEARED;
                    fallingRect.Y += 100;

                    //返すデータの設定
                    FallenBottomEventArgs e = new FallenBottomEventArgs();
                    e.FallenX = fallingRect.X;
                    e.FallingRect = fallingRect;
                    //イベントの発生
                    OnFallenBottom(e);
                }
            }
            else if (fallingRect.state == FallingRect.STATE.FLYING_LEFT)
            {
                if (fallingRect.X + fallingRect.width < 0)
                {
                    fallingRect.state = FallingRect.STATE.DISAPPEARED;

                    //返すデータの設定
                    FallenBottomEventArgs e = new FallenBottomEventArgs();
                    e.FallenX = MainWindow.WIDTH / 2;
                    e.FallingRect = fallingRect;
                    //イベントの発生
                    OnFallenBottom(e);
                }
            }
            else if (fallingRect.state == FallingRect.STATE.FLYING_RIGHT)
            {
                if (MainWindow.WIDTH < fallingRect.X)
                {
                    fallingRect.state = FallingRect.STATE.DISAPPEARED;

                    //返すデータの設定
                    FallenBottomEventArgs e = new FallenBottomEventArgs();
                    e.FallenX = MainWindow.WIDTH / 2;
                    e.FallingRect = fallingRect;
                    //イベントの発生
                    OnFallenBottom(e);
                }
            }
            else if (fallingRect.state == FallingRect.STATE.SCORING)
            {
                if (arduino != null)
                {
                    arduino.digitalWrite(13, Arduino.HIGH);
                }

                if (MainWindow.HEIGHT < fallingRect.weight / 2)
                {
                    fallingRect.state = FallingRect.STATE.DISAPPEARED;

                    if (arduino != null)
                    {
                        switch (fallingRect.color)
                        {
                            case FallingRect.COLOR.RED:
                                arduino.digitalWrite(8, Arduino.LOW);
                                Debug.WriteLine("Arduinoの8番をLOWに");
                                break;
                            case FallingRect.COLOR.GREEN:
                                arduino.digitalWrite(9, Arduino.LOW);
                                Debug.WriteLine("Arduinoの9番をLOWに");
                                break;
                            case FallingRect.COLOR.BLUE:
                                arduino.digitalWrite(10, Arduino.LOW);
                                Debug.WriteLine("Arduinoの10番をLOWに");
                                break;
                            default:
                                Debug.WriteLine("scoringの終了時のfallingRect.colorがおかしい");
                                break;
                        }

                    }
                    //返すデータの設定
                    FallenBottomEventArgs e = new FallenBottomEventArgs();
                    e.FallenX = fallingRect.X;
                    e.FallingRect = fallingRect;
                    //イベントの発生
                    OnFallenBottom(e);
                }
            }
            else if (fallingRect.state == FallingRect.STATE.DISAPPEARED)
            {
            }
            else
            {
                Debug.WriteLine("Judge#doJudgeの場合分けに該当しない");
            }
        }
    }
}

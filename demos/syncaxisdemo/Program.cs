﻿/*
 *                                                            ,--,      ,--,                              
 *             ,-.----.                                     ,---.'|   ,---.'|                              
 *   .--.--.   \    /  \     ,---,,-.----.      ,---,       |   | :   |   | :      ,---,           ,---,.  
 *  /  /    '. |   :    \ ,`--.' |\    /  \    '  .' \      :   : |   :   : |     '  .' \        ,'  .'  \ 
 * |  :  /`. / |   |  .\ :|   :  :;   :    \  /  ;    '.    |   ' :   |   ' :    /  ;    '.    ,---.' .' | 
 * ;  |  |--`  .   :  |: |:   |  '|   | .\ : :  :       \   ;   ; '   ;   ; '   :  :       \   |   |  |: | 
 * |  :  ;_    |   |   \ :|   :  |.   : |: | :  |   /\   \  '   | |__ '   | |__ :  |   /\   \  :   :  :  / 
 *  \  \    `. |   : .   /'   '  ;|   |  \ : |  :  ' ;.   : |   | :.'||   | :.'||  :  ' ;.   : :   |    ;  
 *   `----.   \;   | |`-' |   |  ||   : .  / |  |  ;/  \   \'   :    ;'   :    ;|  |  ;/  \   \|   :     \ 
 *   __ \  \  ||   | ;    '   :  ;;   | |  \ '  :  | \  \ ,'|   |  ./ |   |  ./ '  :  | \  \ ,'|   |   . | 
 *  /  /`--'  /:   ' |    |   |  '|   | ;\  \|  |  '  '--'  ;   : ;   ;   : ;   |  |  '  '--'  '   :  '; | 
 * '--'.     / :   : :    '   :  |:   ' | \.'|  :  :        |   ,/    |   ,/    |  :  :        |   |  | ;  
 *   `--'---'  |   | :    ;   |.' :   : :-'  |  | ,'        '---'     '---'     |  | ,'        |   :   /   
 *             `---'.|    '---'   |   |.'    `--''                              `--''          |   | ,'    
 *               `---`            `---'                                                        `----'   
 * 
 *
 * 
 * IRtcSyncAxis 인터페이스를 직접 사용하는 방법
 * XL-SCAN + ACS + ExcelliSCAN 조합의 OTF 솔류션을 사용할 경우 구현
 * Author : hong chan, choi / sepwind @gmail.com(https://sepwind.blogspot.com)
 * 
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace SpiralLab.Sirius
{

    class Program
    {
        static void Main(string[] args)
        {
            SpiralLab.Core.Initialize();

            #region initialize RTC 
            //var rtc = new RtcVirtual(0); ///create Rtc for dummy
            //var rtc = new Rtc5(0); ///create Rtc5 controller
            //var rtc = new Rtc6(0); ///create Rtc6 controller
            //var rtc = new Rtc6Ethernet(0, "192.168.0.200"); ///create Rtc6 ethernet controller
            //var rtc = new Rtc53D(0); ///create Rtc5 + 3D option controller
            //var rtc = new Rtc63D(0); ///create Rtc5 + 3D option controller
            //var rtc = new Rtc5DualHead(0); ///create Rtc5 + Dual head option controller
            //var rtc = new Rtc5MOTF(0); ///create Rtc5 + MOTF option controller
            //var rtc = new Rtc6MOTF(0); ///create Rtc6 + MOTF option controller
            //var rtc = new Rtc6SyncAxis(0); 
            var rtc = new Rtc6SyncAxis(0, "syncAXISConfig.xml"); ///create Rtc6 + XL-SCAN (ACS+SYNCAXIS) option controller

            /// 스캐너 보정파일(ct5)은 xml 에서 설정됨
            rtc.Initialize(0.0f, LaserMode.Yag1, string.Empty); 
            rtc.CtlFrequency(50 * 1000, 2); /// laser frequency : 50KHz, pulse width : 2usec
            rtc.CtlSpeed(100, 100); /// default jump and mark speed : 100mm/s
            rtc.CtlDelay(10, 100, 200, 200, 0); /// scanner and laser delays
            #endregion

            #region initialize Laser (virtial)
            ILaser laser = new LaserVirtual(0, "virtual", 20);
            #endregion

            ConsoleKeyInfo key;
            do
            {
                Console.WriteLine("Testcase for spirallab.sirius. powered by labspiral@gmail.com (https://sepwind.blogspot.com)");
                Console.WriteLine("");
                Console.WriteLine("'C' : draw circle (scanner only)");
                Console.WriteLine("'R' : draw rectangle (stage only)");
                Console.WriteLine("'L' : draw circle with lines (stage + scanner)");
                Console.WriteLine("'Q' : quit");
                Console.WriteLine("");
                Console.Write("select your target : ");
                key = Console.ReadKey(false);
                if (key.Key == ConsoleKey.Q)
                    break;
                Console.WriteLine("\r\nWARNING !!! LASER IS BUSY ...");
                var timer = Stopwatch.StartNew();
                switch (key.Key)
                {
                    case ConsoleKey.C:
                        DrawCircle(laser, rtc, 10);
                        break;
                    case ConsoleKey.R:
                        DrawRectangle(laser, rtc, 10, 10);
                        break;
                    case ConsoleKey.L:
                        DrawCircleWithLines(laser, rtc, 10);
                        break;
                }

                Console.WriteLine($"processing time = {timer.ElapsedMilliseconds / 1000.0:F3}s");
            } while (true);

            rtc.Dispose();
        }
        /// <summary>
        /// 지정된 반지름을 갖는 원 그리기
        /// </summary>
        /// <param name="rtc"></param>
        /// <param name="radius"></param>
        private static void DrawCircle(ILaser laser, IRtc rtc, double radius)
        {
            /// 스캐너만 구동하여 원 그리기
            rtc.ListBegin(laser, MotionType.ScannerOnly);
            rtc.ListJump(new Vector2((float)radius, 0));
            rtc.ListArc(new Vector2(0, 0), 360.0f);
            rtc.ListEnd();
            rtc.ListExecute(true);
        }
        /// <summary>
        /// 지정된 크기의 직사각형 그리기
        /// </summary>
        /// <param name="rtc"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private static void DrawRectangle(ILaser laser, IRtc rtc, double width, double height)
        {
            ///스테이지의 원점은 통상 0,0 이기 때문에 - 영역에서는 모션구동이 불가능하므로
            ///+ 영역에서 처리되도록 안전한 위치로 이동하는 코드
            rtc.MatrixStack.Push(width * 1.5f, height * 1.5f);///transit safety area
            /// 스테이지만 구동하여 원 그리기
            rtc.ListBegin(laser, MotionType.StageOnly);
            rtc.ListJump(new Vector2((float)-width / 2, (float)height / 2));
            rtc.ListMark(new Vector2((float)width / 2, (float)height / 2));
            rtc.ListMark(new Vector2((float)width / 2, (float)-height / 2));
            rtc.ListMark(new Vector2((float)-width / 2, (float)-height / 2));
            rtc.ListMark(new Vector2((float)-width / 2, (float)height / 2));
            rtc.ListEnd();
            rtc.ListExecute(true);
            rtc.MatrixStack.Pop();
        }
        /// <summary>
        /// 직선으로 원 그리기
        /// </summary>
        /// <param name="rtc"></param>
        /// <param name="radius"></param>
        /// <param name="durationMsec"></param>
        private static void DrawCircleWithLines(ILaser laser, IRtc rtc, float radius)
        {
            ///스테이지의 원점은 통상 0,0 이기 때문에 - 영역에서는 모션구동이 불가능하므로
            ///+ 영역에서 처리되도록 안전한 위치로 이동하는 코드
            rtc.MatrixStack.Push(radius * 2f, radius * 2f);///transit safety area

            /// 스테이지 + 스캐너 동시 구동하여 원 그리기
            rtc.ListBegin(laser, MotionType.StageAndScanner);
            double x = radius * Math.Sin(0 * Math.PI / 180.0);
            double y = radius * Math.Cos(0 * Math.PI / 180.0);
            rtc.ListJump(new Vector2((float)x, (float)y));

            for (float angle = 10; angle < 360; angle += 10)
            {
                x = radius * Math.Sin(angle * Math.PI / 180.0);
                y = radius * Math.Cos(angle * Math.PI / 180.0);
                rtc.ListMark(new Vector2((float)x, (float)y));
            }
            x = radius * Math.Sin(0 * Math.PI / 180.0);
            y = radius * Math.Cos(0 * Math.PI / 180.0);
            rtc.ListMark(new Vector2((float)x, (float)y));

            rtc.ListEnd();
            rtc.ListExecute(true);
            rtc.MatrixStack.Pop();
        }
    }
}

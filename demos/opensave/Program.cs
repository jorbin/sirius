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
 * Document 문서( 가공 데이타) 저장, 열기
 * 
 * 문서(document) 는 레이어, 블럭 , 환경 설정 및 가공에 필요한 다양한 객체(Entity : 선, 호, 원, 폴리라인, 레이저 파라메터 등) 정보를 가지고 있다.
 * 이 가공 객체(Entity)를 생성하고, 저장이 가능하며 또한 레이저 가공을 시도한다.
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

            #region create entities 
            var doc1 = new DocumentDefault("Unnamed");
            var layer = new Layer("default");
            doc1.Layers.Add(layer);
            layer.Add(new Line(0, 10, 20,20));
            layer.Add(new Circle(0, 0, 10));
            layer.Add(new Spiral(-20.0f, 0.0f, 0.5f, 2.0f, 5, true));
            #endregion

            Console.WriteLine("press any key to save ...");
            Console.ReadKey(false);
            string filename = "default.sirius";

            var ds = new DocumentSerializer();
            ds.Save(doc1, filename);

            Console.WriteLine("press any key to open ...");
            Console.ReadKey(false);
            var doc2 = DocumentSerializer.OpenSirius(filename);

            Console.WriteLine("press any key to rtc initialize ...");
            Console.ReadKey(false);

            #region initialize RTC 
            var rtc = new RtcVirtual(0, "output2.txt");
            //var rtc = new Rtc5(0); ///create Rtc5 controller
            //var rtc = new Rtc6(0); ///create Rtc6 controller
            //var rtc = new Rtc6Ethernet(0, "192.168.0.200"); ///create Rtc6 ethernet controller
            //var rtc = new Rtc53D(0); ///create Rtc5 + 3D option controller
            //var rtc = new Rtc63D(0); ///create Rtc5 + 3D option controller
            //var rtc = new Rtc5DualHead(0); ///create Rtc5 + Dual head option controller
            //var rtc = new Rtc5MOTF(0); ///create Rtc5 + MOTF option controller
            //var rtc = new Rtc6MOTF(0); ///create Rtc6 + MOTF option controller
            //var rtc = new Rtc6SyncAxis(0); 
            //var rtc = new Rtc6SyncAxis(0, "syncAXISConfig.xml"); ///create Rtc6 + XL-SCAN (ACS+SYNCAXIS) option controller

            float fov = 60.0f;    /// scanner field of view : 60mm            
            float kfactor = (float)Math.Pow(2, 20) / fov; /// k factor (bits/mm) = 2^20 / fov
            var correctionFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "correction", "cor_1to1.ct5");
            rtc.Initialize(kfactor, LaserMode.Yag1, correctionFile);    /// 스캐너 보정 파일 지정 : correction file
            rtc.CtlFrequency(50 * 1000, 2); /// laser frequency : 50KHz, pulse width : 2usec
            rtc.CtlSpeed(100, 100); /// default jump and mark speed : 100mm/s
            rtc.CtlDelay(10, 100, 200, 200, 0); /// scanner and laser delays
            #endregion

            #region initialize Laser (virtial)
            ILaser laser = new LaserVirtual(0, "virtual", 20);
            #endregion

            Console.WriteLine("press any key to laser processing ...WARNING !!!  LASER EMISSION");
            Console.ReadKey(false);
            DoBegin(laser, rtc, doc2);

            Console.WriteLine("press any key to terminate program");
            Console.ReadKey(false);
        }

        static void DoBegin(ILaser laser, IRtc rtc, IDocument doc)
        {
            var timer = Stopwatch.StartNew();
            bool success = true;
            rtc.ListBegin(laser);
            foreach (var layer in doc.Layers)
            {
                foreach (var entity in layer)
                {
                    var markerable = entity as IMarkerable;
                    if (null != markerable)
                        success &= markerable.Mark(rtc);
                    if (!success)
                        break;
                }
                if (!success)
                    break;
            }
            if (success)
            {
                rtc.ListEnd();
                rtc.ListExecute(true);
            }
            Console.WriteLine($"processing time = {timer.ElapsedMilliseconds / 1000.0:F3}s");
        }
    }
}

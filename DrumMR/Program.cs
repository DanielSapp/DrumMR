using StereoKit;
using System;
using Microsoft.MixedReality.QR;
using System.Threading;
using System.Diagnostics;

namespace DrumMR
{
    class Program
    {
        static Pose[] drumLocations = new Pose[3];
        static void Main(string[] args)
        {
            Debug.WriteLine("sleep");
            //Initialize drumLocations to a "default pose".  We will check if these have changed to determine if that drum has been located.
            InitializeDrumLocations();

            //Directly modifies drumLocations[i] with the location of the i'th drum.
            if (SetQRPoses() == false)
            {
                Console.WriteLine("Error starting QR code reading");
                return;
            }
            WaitFromDrumInitialization();

            // Initialize StereoKit
            SKSettings settings = new SKSettings
            {
                appName = "DrumMR",
                assetsFolder = "Assets",
            };
            if (!SK.Initialize(settings))
                Environment.Exit(1);

            Pose windowPose = new Pose(-.4f, 0, 0, Quat.LookDir(1, 0, 1));

            bool showHeader = true;
            float slider = 0.5f;

            Model clipboard = Model.FromFile("Clipboard.glb");
            Sprite grid = Sprite.FromFile("grd.png", SpriteType.Single);

            // Create assets used by the app
            Pose cubePose = new Pose(0, 0, -0.5f, Quat.Identity);
            Model cube = Model.FromMesh(
                Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                Default.MaterialUI);

            Matrix floorTransform = Matrix.TS(new Vec3(0, -1.5f, 0), new Vec3(30, 0.1f, 30));
            Material floorMaterial = new Material(Shader.FromFile("floor.hlsl"));
            floorMaterial.Transparency = Transparency.Blend;

            //Sprite grid = Sprite.FromFile("grid.png", SpriteType.Single);
            //Matrix gridMatrix = Pose.ToMatrix(drumLocations[0].position);
            // Core application loop
            while (SK.Step(() =>
            {
                if (SK.System.displayType == Display.Opaque)
                    Default.MeshCube.Draw(floorMaterial, floorTransform);
                UI.WindowBegin("Window", ref windowPose, new Vec2(20, 0) * U.cm, showHeader ? UIWin.Normal : UIWin.Body);
                if (UI.Toggle("Exit", ref showHeader))
                {
                    SK.Shutdown();
                }
                UI.Label("Slide");
                UI.SameLine();
                UI.HSlider("slider", ref slider, 0, 1, 0.2f, 72 * U.mm);
                UI.WindowEnd();


                UI.Handle("Cube", ref cubePose, cube.Bounds);
                cube.Draw(cubePose.ToMatrix());
            })) ;
            SK.Shutdown();
        }

        //Sets an event handler to fill drumLocations[i] with the found location of the QR code with the text i.  Returns whether the initialization was successful.
        private static bool SetQRPoses()
        {
            QRCodeWatcher watcher;
            DateTime watcherStart;
            var status = QRCodeWatcher.RequestAccessAsync().Result;
            if (status != QRCodeWatcherAccessStatus.Allowed)
            {
                Debug.WriteLine("ERROR: PERMISSION TO READ QR CODES NOT GRANTED");
                return false;
            }
            watcherStart = DateTime.Now;
            watcher = new QRCodeWatcher();
            watcher.Added += (o, qr) => {
                // QRCodeWatcher will provide QR codes from before session start,
                // so we often want to filter those out.
                if (qr.Code.LastDetectedTime > watcherStart)
                {
                    drumLocations[Int32.Parse(qr.Code.Data)] = World.FromSpatialNode(qr.Code.SpatialGraphNodeId);
                    Debug.WriteLine("QR Code number " + qr.Code.Data + " has been located.  Move to the next code");
                }
            };
            watcher.Start();
            Debug.WriteLine("init sucess");
            return true;
        }

        //Returns whether parameter p is the "default pose" or a meaningful one
        private static bool PoseIsInitialized(Pose p)
        {
            if (p.position.x == 0 && p.position.y == 0 && p.position.z == 0 && p.orientation.Equals(Quat.Identity))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //Initializes drumLocations[] with default values
        private static void InitializeDrumLocations()
        {
            for (int i = 0; i < drumLocations.Length; i++)
            {
                drumLocations[i] = new Pose(0, 0, 0, Quat.Identity);
            }
        }

        //Sleeps the current thread until all of drumLocations[] has been initialized.
        private static void WaitFromDrumInitialization()
        {
            bool allDrumsFound;
            do
            {
                allDrumsFound = true;
                for (int i = 0; i < drumLocations.Length; i++)
                {
                    if (!PoseIsInitialized(drumLocations[i]))
                    {
                        allDrumsFound = false;
                    }
                }
                Thread.Sleep(500);
                Debug.WriteLine("sleep");
            } while (!allDrumsFound);
        }
    }
}

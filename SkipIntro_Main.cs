using Planetbase;
using System;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tahvohck_Mods.JPFariasUpdates
{
    public class SkipIntro
    {
        private static IntroCinemetic Intro;
        private static ColonyShip Ship;
        private static GameManager _Manager;
        private static readonly BindingFlags instanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly int _LayerMask = 256;
        public const float VerticalAngle = 25f; // Taken from game source (CameraManager)

        [LoaderOptimization(LoaderOptimization.NotSpecified)]
        public static void Init(UnityModManager.ModEntry modData)
        {
            modData.OnUpdate = Update;
        }

        public static void Update(UnityModManager.ModEntry modData, float tDelta)
        {
            // Set up manager here, where it's guaranteed to be initialized.
            if (_Manager is null) {
                _Manager = GameManager.getInstance();
            }

            // Get intro and try to cast it as an IntroCinematic. If that fails, exit.
            if (Intro is null) {
                Intro = CameraManager.getInstance().getCinematic() as IntroCinemetic;
                if (Intro is null) { return; }
                // Reflection to get ship.
                Ship = Intro
                    .GetType()
                    .GetField("mColonyShip", instanceFlags)
                    .GetValue(Intro) as ColonyShip;
            }

            // Check if ship is done. Also run if ship is null.
            if (Ship?.isDone() ?? true) {
                Intro = null;
                return;
            }

            var state = _Manager.getGameState() as GameStateGame;
            var gameInterface = state
                .GetType()
                .GetField("mGameGui", instanceFlags)
                .GetValue(state) as GameGui;

            // Disable Menu
            if (gameInterface.getWindow() is GuiGameMenu) {
                gameInterface.setWindow(null);
            }

            if (Input.GetKeyUp(KeyCode.Escape)) {
                // I removed a null check for Intro here since we only get here if the cutscene isn't null
                // and that only happens during the intro cutscene.
                PhysicsUtil.findFloor(
                    Ship.getPosition(),
                    out Vector3 shipLandingPosition,
                    _LayerMask);
                shipLandingPosition.y += CameraManager.DefaultHeight;

                // Snap the camera to the ground and make it look at the landing spot
                Transform cameraTransform = CameraManager.getInstance().getTransform();
                cameraTransform.position = shipLandingPosition + Ship.getDirection().flatDirection();
                cameraTransform.LookAt(shipLandingPosition);

                // Set vertical angle
                Vector3 euler = cameraTransform.eulerAngles;
                euler.x = VerticalAngle;
                cameraTransform.rotation = Quaternion.Euler(euler);

                // Remove black bars
                Intro
                    .GetType()
                    .GetField("mBlackBars", instanceFlags)
                    .SetValue(Intro, 0f);
                CameraManager.getInstance().setCinematic(null);
                Intro = null;
            }
        }
    }
}

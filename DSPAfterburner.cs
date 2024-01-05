using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Net;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]



namespace DSPAfterburner
{
    [BepInPlugin("Appun.DSP.plugin.Afterburner", "DSPAfterburner", "0.0.1")]
    [HarmonyPatch]
    public class DSPAfterburner : BaseUnityPlugin
    {

        public static ConfigEntry<float> speedMultiplier;
        public static ConfigEntry<float> maximumSpeed;
        public static ConfigEntry<KeyCode> KeyConfig;



        public void Awake()
        {
            LogManager.Logger = Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        }


        [HarmonyPrefix, HarmonyPatch(typeof(PlayerMove_Fly), "GameTick")]
        public static bool PlayerMove_Fly_GameTick_Prefix(PlayerMove_Fly __instance)
        {
            bool boost = false;
            float num = 0.016666668f;
            if (__instance.player.movementState == EMovementState.Fly)
            {

                Vector3 vector = __instance.controller.mainCamera.transform.forward;
                Vector3 normalized = __instance.player.position.normalized;
                Vector3 normalized2 = Vector3.Cross(normalized, vector).normalized;
                vector = Vector3.Cross(normalized2, normalized);
                Vector3 vector2 = vector * __instance.controller.input0.y + normalized2 * __instance.controller.input0.x;
                if ((__instance.controller.cmd.type == ECommand.Build && !VFInput._godModeMechaMove && !PlayerController.operationWhenBuild) || __instance.navigation.navigating)
                {
                    vector2 = Vector3.zero;
                }
                bool flag = __instance.controller.actionBuild.blueprintMode > EBlueprintMode.None;
                if (flag && !VFInput._godModeMechaMove)
                {
                    vector2 = Vector3.zero;
                }
                PlanetData localPlanet = GameMain.localPlanet;
                bool flag2 = localPlanet != null && localPlanet.type != EPlanetType.Gas;
                float num2 = __instance.controller.softLandingRecover;
                num2 *= num2;
                float num3 = 0.022f * num2;
                float num4 = __instance.player.mecha.walkSpeed * 2.5f;
                /////////////////////////////////////////////////////////////////
                if (Input.GetKey(KeyConfig.Value))
                {
                    num4 = num4 * speedMultiplier.Value;
                    if (num4 > maximumSpeed.Value)
                    {
                        num4 = maximumSpeed.Value;
                    }
                    boost = true;
                }
                /////////////////////////////////////////////////////////////////
                float num5 = __instance.controller.input1.y;
                if ((__instance.controller.cmd.type == ECommand.Build && !VFInput._godModeMechaMove && !PlayerController.operationWhenBuild) || __instance.navigation.navigating)
                {
                    num5 = 0f;
                }
                if (flag && !VFInput._godModeMechaMove)
                {
                    num5 = 0f;
                }
                if (__instance.navigation.navigating)
                {
                    bool flag3 = false;
                    bool flag4 = false;
                    __instance.navigation.DetermineHighOperation(num4, ref flag3, ref flag4);
                    num5 = 0f;
                    if (flag3)
                    {
                        num5 += 1f;
                    }
                    if (flag4 && __instance.targetAltitude > 15.01f + num * 20f)
                    {
                        num5 += -1f;
                    }
                }
                __instance.targetAltitude += num5 * num * 20f;
                if (__instance.controller.cmd.type == ECommand.Build && !PlayerController.operationWhenBuild && __instance.targetAltitude > 40f)
                {
                    __instance.targetAltitude = 40f;
                }
                if (flag && !VFInput._godModeMechaMove && __instance.targetAltitude > 40f)
                {
                    __instance.targetAltitude = 40f;
                }
                if (num5 == 0f && __instance.targetAltitude > 15f)
                {
                    __instance.targetAltitude -= num * 20f * 0.3f;
                    if (__instance.targetAltitude < 15f)
                    {
                        __instance.targetAltitude = 15f;
                    }
                }
                else if (__instance.targetAltitude >= 50f)
                {
                    if (__instance.currentAltitude > 49f && __instance.controller.horzSpeed > 12.5f && __instance.mecha.thrusterLevel >= 2)
                    {
                        if (__instance.controller.cmd.type == ECommand.Build)
                        {
                            __instance.controller.cmd.SetNoneCommand();
                            __instance.controller.actionBuild.blueprintMode = EBlueprintMode.None;
                        }
                        __instance.controller.movementStateInFrame = EMovementState.Sail;
                        __instance.controller.actionSail.ResetSailState();
                        GameCamera.instance.SyncForSailMode();
                        GameMain.gameScenario.NotifyOnSailModeEnter();
                    }
                    __instance.targetAltitude = 50f;
                }
                if (flag2)
                {
                    if (__instance.targetAltitude < 14.5f)
                    {
                        if (num5 > 0f)
                        {
                            __instance.targetAltitude = 15f;
                        }
                        else
                        {
                            __instance.targetAltitude = 1f;
                        }
                        if (__instance.currentAltitude < 3f)
                        {
                            __instance.controller.movementStateInFrame = EMovementState.Walk;
                            __instance.controller.softLandingTime = 1.2f;
                        }
                    }
                }
                else if (__instance.targetAltitude < 20f)
                {
                    __instance.targetAltitude = 20f;
                }
                float realRadius = __instance.player.planetData.realRadius;
                float num6 = Mathf.Max(__instance.player.position.magnitude, realRadius * 0.9f);
                __instance.currentAltitude = num6 - realRadius;
                if (localPlanet.type == EPlanetType.Gas && __instance.mecha.coreEnergy < 10000000.0 && __instance.currentAltitude < 10f)
                {
                    __instance.targetAltitude = __instance.currentAltitude + 0.5f;
                }
                float num7 = __instance.targetAltitude - __instance.currentAltitude;
                float num8 = Mathf.Clamp(num / (Time.fixedDeltaTime + 1E-05f), 0.16f, 6f);
                if (__instance.targetAltitude > 17f || __instance.targetAltitude < 13f)
                {
                    num8 = 1f / num8;
                    num8 = Mathf.Sqrt(num8);
                }
                __instance.verticalThrusterForce = 0f;
                float num9 = Mathf.Clamp(num7 * 0.5f, -10f, 10f) * 100f * num8 + (float)__instance.controller.universalGravity.magnitude;
                num9 = Mathf.Max(0f, num9);
                __instance.verticalThrusterForce += num9;
                float num10 = 1f;
                if (localPlanet.type == EPlanetType.Gas && __instance.mecha.coreEnergy < 120000000.0)
                {
                    num10 = Mathf.Clamp01((float)(__instance.mecha.coreEnergy / 120000000.0 + 0.05));
                }
                __instance.verticalThrusterForce *= num10;
                __instance.UseThrustEnergy(ref __instance.verticalThrusterForce, __instance.controller.vertSpeed, (double)num);
                __instance.verticalThrusterForce /= num10;
                float num11 = (float)(Math.Sin(GlobalObject.timeSinceStart * 2.0) * 0.1 + 1.0);
                if (Mathf.Abs(__instance.verticalThrusterForce) > 0.001f)
                {
                    __instance.controller.AddLocalForce(normalized * (__instance.verticalThrusterForce * num11));
                }
                OrderNode currentOrder = __instance.player.currentOrder;
                if (currentOrder != null && !currentOrder.targetReached)
                {
                    Vector3 vector3 = currentOrder.target.normalized * localPlanet.realRadius - __instance.player.position.normalized * localPlanet.realRadius;
                    float magnitude = vector3.magnitude;
                    vector3 = Vector3.Cross(Vector3.Cross(normalized, vector3).normalized, normalized).normalized;
                    __instance.rtsVelocity = Vector3.Slerp(__instance.rtsVelocity, vector3 * num4, num3);
                }
                else
                {
                    __instance.rtsVelocity = Vector3.MoveTowards(__instance.rtsVelocity, Vector3.zero, num * 6f * num4);
                }
                if (__instance.navigation.navigating)
                {
                    __instance.navigation.DetermineHighVelocity(num4, num3, ref __instance.moveVelocity, num);
                }
                else
                {
                    __instance.moveVelocity = Vector3.Slerp(__instance.moveVelocity, vector2 * num4, num3);
                }
                Vector3 vector4 = __instance.moveVelocity + __instance.rtsVelocity;
                if ((double)num2 > 0.9)
                {
                    vector4 = Vector3.ClampMagnitude(vector4, num4);
                }
                __instance.UseFlyEnergy(ref vector4, __instance.mecha.walkPower * (double)num * (double)__instance.controller.softLandingRecover);
                Vector3 b = Vector3.Dot(vector4, normalized) * normalized;
                vector4 -= b;
                float num12 = __instance.controller.vertSpeed;
                if (num12 > 50f)
                {
                    num12 = 50f;
                }
                float num13 = Mathf.Lerp(0.95f, 0.8f, Mathf.Abs(num7) * 0.3f);
                float num14 = num13;
                num13 = Mathf.Lerp(1f, num13, Mathf.Clamp01(__instance.verticalThrusterForce));
                num14 = Mathf.Lerp(1f, num14, Mathf.Clamp01(__instance.verticalThrusterForce) * Mathf.Clamp01((float)(__instance.mecha.coreEnergy - 5000.0) * 0.0001f));
                if (num12 > 0f)
                {
                    num12 *= num13;
                }
                else if (num12 < 0f)
                {
                    num12 *= num14;
                }
                __instance.controller.velocity = num12 * normalized + vector4;
                if (vector2.sqrMagnitude > 0.25f)
                {
                    __instance.controller.turning_raw = Vector3.SignedAngle(vector4, vector2, normalized);
                }
                else
                {
                    __instance.controller.turning_raw = 0f;
                }
                if (flag2 && __instance.mecha.coreEnergy < 10000.0)
                {
                    __instance.controller.movementStateInFrame = EMovementState.Walk;
                    __instance.controller.softLandingTime = 1.2f;
                }
                __instance.controller.actionWalk.rtsVelocity = __instance.rtsVelocity;
                __instance.controller.actionWalk.moveVelocity = __instance.moveVelocity * 0.5f;
                __instance.controller.actionDrift.rtsVelocity = __instance.rtsVelocity;
                __instance.controller.actionDrift.moveVelocity = __instance.moveVelocity * 0.5f;
            }
            return false;
        }


        public class LogManager
        {
            public static ManualLogSource Logger;
        }
    }
}
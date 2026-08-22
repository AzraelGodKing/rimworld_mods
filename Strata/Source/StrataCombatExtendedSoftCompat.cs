using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Strata
{
    // Soft-compat: Combat Extended verbs are Verb_LaunchProjectileCE : Verb
    // (not Verb_LaunchProjectile), and bullets are ProjectileCE : ThingWithComps
    // (not Projectile). Vanilla gap fire therefore never finds a gun and cannot
    // spawn a CE round. All hooks are reflection; missing CE is a no-op.
    public static class StrataCombatExtendedSoftCompat
    {
        public const string PackageId = "CETeam.CombatExtended";

        private static bool bound;
        private static bool logged;
        private static Type verbLaunchType;
        private static Type projectileCeType;
        private static Type ammoType;
        private static Type turretCeType;
        private static MethodInfo launchMethod;
        private static MethodInfo prepareShot;
        private static MethodInfo notifyShotFired;
        private static MethodInfo tryStartReload;
        private static PropertyInfo projectileProp;
        private static PropertyInfo canBeFiredNow;
        private static FieldInfo intendedTargetField;
        private static FieldInfo minCollisionDistanceField;
        private static FieldInfo canTargetSelfField;
        private static FieldInfo offMapOriginField;
        private static FieldInfo turretTopField;

        public static bool Active => bound && verbLaunchType != null && projectileCeType != null;

        public static void TryPatch(Harmony harmony)
        {
            Bind();
            if (!Active)
            {
                return;
            }

            int n = 0;
            try
            {
                if (harmony != null && turretCeType != null)
                {
                    MethodInfo tryFind = AccessTools.Method(turretCeType, "TryFindNewTarget");
                    if (tryFind != null)
                    {
                        harmony.Patch(
                            tryFind,
                            postfix: new HarmonyMethod(
                                typeof(Patch_TurretGun_TryFindNewTarget_CrossLevel),
                                nameof(Patch_TurretGun_TryFindNewTarget_CrossLevel.Postfix)));
                        n++;
                    }

                    MethodInfo gizmos = AccessTools.Method(turretCeType, "GetGizmos");
                    if (gizmos != null)
                    {
                        harmony.Patch(
                            gizmos,
                            postfix: new HarmonyMethod(
                                typeof(Patch_TurretGun_CrossLevelGizmos),
                                nameof(Patch_TurretGun_CrossLevelGizmos.Postfix)));
                        n++;
                    }
                }

                Log.Message("[Strata] Combat Extended soft-compat: cross-level fire uses CE verbs/projectiles"
                    + (n > 0 ? " (turret hooks " + n + ")." : "."));
            }
            catch (Exception e)
            {
                Log.Warning("[Strata] Combat Extended turret patches failed: " + e.Message);
            }
        }

        private static void Bind()
        {
            if (bound)
            {
                return;
            }

            bound = true;
            try
            {
                verbLaunchType = AccessTools.TypeByName("CombatExtended.Verb_LaunchProjectileCE");
                projectileCeType = AccessTools.TypeByName("CombatExtended.ProjectileCE");
                ammoType = AccessTools.TypeByName("CombatExtended.CompAmmoUser");
                turretCeType = AccessTools.TypeByName("CombatExtended.Building_TurretGunCE");
                if (verbLaunchType == null || projectileCeType == null)
                {
                    return;
                }

                projectileProp = AccessTools.Property(verbLaunchType, "Projectile");
                launchMethod = AccessTools.Method(
                    projectileCeType,
                    "Launch",
                    new[]
                    {
                        typeof(Thing), typeof(Vector2), typeof(float), typeof(float),
                        typeof(float), typeof(float), typeof(Thing), typeof(float),
                    });
                intendedTargetField = AccessTools.Field(projectileCeType, "intendedTarget");
                minCollisionDistanceField = AccessTools.Field(projectileCeType, "minCollisionDistance");
                canTargetSelfField = AccessTools.Field(projectileCeType, "canTargetSelf");
                offMapOriginField = AccessTools.Field(projectileCeType, "OffMapOrigin");
                if (turretCeType != null)
                {
                    turretTopField = AccessTools.Field(turretCeType, "top");
                }

                if (ammoType != null)
                {
                    prepareShot = AccessTools.Method(ammoType, "TryPrepareShot");
                    notifyShotFired = AccessTools.Method(ammoType, "Notify_ShotFired", new[] { typeof(int) });
                    if (notifyShotFired == null)
                    {
                        notifyShotFired = AccessTools.Method(ammoType, "Notify_ShotFired");
                    }
                    tryStartReload = AccessTools.Method(ammoType, "TryStartReload");
                    canBeFiredNow = AccessTools.Property(ammoType, "CanBeFiredNow");
                }

                if (launchMethod == null)
                {
                    Log.Warning("[Strata] Combat Extended present but ProjectileCE.Launch was not found.");
                    verbLaunchType = null;
                    projectileCeType = null;
                }
            }
            catch (Exception e)
            {
                verbLaunchType = null;
                projectileCeType = null;
                Log.Warning("[Strata] Combat Extended bind failed: " + e.Message);
            }
        }

        public static bool IsLaunchVerb(Verb verb)
        {
            Bind();
            return verb != null && verbLaunchType != null && verbLaunchType.IsInstanceOfType(verb);
        }

        public static ThingDef GetProjectile(Verb verb)
        {
            Bind();
            if (verb == null || projectileProp == null || !IsLaunchVerb(verb))
            {
                return null;
            }

            try
            {
                return projectileProp.GetValue(verb) as ThingDef;
            }
            catch
            {
                return verb.verbProps?.defaultProjectile;
            }
        }

        public static bool CanFire(Verb verb)
        {
            if (!IsLaunchVerb(verb))
            {
                return true;
            }

            object ammo = AmmoComp(verb);
            if (ammo != null && canBeFiredNow != null)
            {
                try
                {
                    if (!(bool)canBeFiredNow.GetValue(ammo))
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }

            return GetProjectile(verb) != null;
        }

        public static void TryReload(Verb verb)
        {
            object ammo = AmmoComp(verb);
            if (ammo == null || tryStartReload == null)
            {
                return;
            }

            try
            {
                tryStartReload.Invoke(ammo, null);
            }
            catch
            {
                // Fail-open: the pawn / turret can still take CE's own reload job later.
            }
        }

        public static void FaceTurret(Building_Turret turret, float angle)
        {
            if (turret == null || turretTopField == null || turretCeType == null
                || !turretCeType.IsInstanceOfType(turret))
            {
                return;
            }

            try
            {
                if (turretTopField.GetValue(turret) is TurretTop top && top != null)
                {
                    top.CurRotation = angle;
                }
            }
            catch
            {
                // Cosmetic only.
            }
        }

        public static bool TryFire(
            Thing shooter,
            Verb verb,
            LocalTargetInfo target,
            Map targetMap,
            IntVec3 spawnCell,
            Vector3 launchPos,
            float distance,
            bool arc)
        {
            Bind();
            if (!Active || !IsLaunchVerb(verb) || shooter == null || targetMap == null || !target.IsValid)
            {
                return false;
            }

            ThingDef projDef = GetProjectile(verb);
            if (projDef == null)
            {
                return false;
            }

            try
            {
                object ammo = AmmoComp(verb);
                if (prepareShot != null && ammo != null)
                {
                    object prepared = prepareShot.Invoke(ammo, null);
                    if (prepared is bool ok && !ok)
                    {
                        TryReload(verb);
                        return false;
                    }
                }

                int pellets = PelletCount(projDef);
                float shotHeight = arc ? 2f : 1.5f;
                float targetHeight = TargetHeight(target);
                Vector3 dest = Destination(target);
                Vector3 source = new Vector3(launchPos.x, shotHeight, launchPos.z);
                Vector3 aim = new Vector3(dest.x, targetHeight, dest.z);
                Vector2 origin = new Vector2(launchPos.x, launchPos.z);
                float shotRotation = ShotRotation(aim - source);
                float shotAngle = ShotAngle(projDef, source, aim, arc);
                Thing equipment = verb.EquipmentSource;

                for (int i = 0; i < pellets; i++)
                {
                    Thing proj = ThingMaker.MakeThing(projDef);
                    if (proj == null || !projectileCeType.IsInstanceOfType(proj))
                    {
                        proj?.Destroy();
                        WarnOnce("CE projectile def is not a ProjectileCE: " + projDef.defName);
                        return false;
                    }

                    GenSpawn.Spawn(proj, spawnCell, targetMap);
                    intendedTargetField?.SetValue(proj, target);
                    minCollisionDistanceField?.SetValue(proj, 0.4f);
                    canTargetSelfField?.SetValue(proj, false);
                    offMapOriginField?.SetValue(proj, true);

                    launchMethod.Invoke(
                        proj,
                        new object[]
                        {
                            shooter,
                            origin,
                            shotAngle,
                            shotRotation,
                            shotHeight,
                            -1f,
                            equipment,
                            distance,
                        });
                }

                if (notifyShotFired != null && ammo != null)
                {
                    if (notifyShotFired.GetParameters().Length == 1)
                    {
                        notifyShotFired.Invoke(ammo, new object[] { 1 });
                    }
                    else
                    {
                        notifyShotFired.Invoke(ammo, null);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                WarnOnce("cross-level CE fire failed: " + (e.InnerException?.Message ?? e.Message));
                return false;
            }
        }

        private static Vector3 Destination(LocalTargetInfo target)
        {
            if (target.HasThing && target.Thing != null)
            {
                return target.Thing.DrawPos;
            }

            return target.Cell.ToVector3Shifted();
        }

        private static float TargetHeight(LocalTargetInfo target)
        {
            if (target.Thing is Pawn)
            {
                return 1f;
            }

            return 0.5f;
        }

        // Same formula as CombatExtended.BaseTrajectoryWorker.TryFindShotAngle.
        // Angle 0 makes CE DistanceTraveled ≈ 0, so the round dies at the muzzle.
        private static float ShotAngle(ThingDef projDef, Vector3 source, Vector3 dest, bool arc)
        {
            ProjectileProperties props = projDef?.projectile;
            float speed = props != null && props.speed > 1f ? props.speed : 70f;
            float gravity = GravityPerWidth(props);
            if (gravity < 0.01f)
            {
                gravity = 9.8f;
            }

            Vector2 from = new Vector2(source.x, source.z);
            Vector2 to = new Vector2(dest.x, dest.z);
            float range = (to - from).magnitude;
            if (range < 0.05f)
            {
                return 0.01f;
            }

            bool overhead = arc || (props != null && props.flyOverhead);
            float heightDifference = dest.y - source.y;
            float inner = Mathf.Pow(speed, 4f)
                - gravity * (gravity * range * range + 2f * heightDifference * speed * speed);
            if (inner < 0f || float.IsNaN(inner))
            {
                return 45f * Mathf.Deg2Rad;
            }

            float root = Mathf.Sqrt(inner);
            return Mathf.Atan((speed * speed + (overhead ? 1f : -1f) * root) / (gravity * range));
        }

        private static float GravityPerWidth(ProjectileProperties props)
        {
            if (props == null)
            {
                return 9.8f;
            }

            try
            {
                PropertyInfo prop = AccessTools.Property(props.GetType(), "GravityPerWidth");
                if (prop != null)
                {
                    object value = prop.GetValue(props);
                    if (value is float f && f > 0.01f)
                    {
                        return f;
                    }
                }

                FieldInfo field = AccessTools.Field(props.GetType(), "GravityPerWidth");
                if (field != null)
                {
                    object value = field.GetValue(props);
                    if (value is float f && f > 0.01f)
                    {
                        return f;
                    }
                }
            }
            catch
            {
                // Use CE's usual Earth-scale default.
            }

            return 9.8f;
        }

        // CE Verb_ShootCE: (-90 + atan2(z, x) in degrees) % 360
        private static float ShotRotation(Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            return (-90f + Mathf.Rad2Deg * Mathf.Atan2(delta.z, delta.x)) % 360f;
        }

        private static int PelletCount(ThingDef projDef)
        {
            ProjectileProperties props = projDef?.projectile;
            if (props == null)
            {
                return 1;
            }

            FieldInfo field = AccessTools.Field(props.GetType(), "pelletCount");
            if (field == null)
            {
                return 1;
            }

            try
            {
                return Mathf.Max(1, Convert.ToInt32(field.GetValue(props)));
            }
            catch
            {
                return 1;
            }
        }

        private static object AmmoComp(Verb verb)
        {
            if (ammoType == null || verb?.EquipmentSource == null)
            {
                return null;
            }

            ThingWithComps eq = verb.EquipmentSource;
            for (int i = 0; i < eq.AllComps.Count; i++)
            {
                ThingComp comp = eq.AllComps[i];
                if (comp != null && ammoType.IsInstanceOfType(comp))
                {
                    return comp;
                }
            }

            return null;
        }

        private static void WarnOnce(string message)
        {
            if (logged)
            {
                return;
            }

            logged = true;
            Log.Warning("[Strata] " + message);
        }
    }
}

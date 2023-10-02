using BepInEx;
using RoR2;
using EntityStates.Treebot.Weapon;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace FixREXCrash
{
  [BepInPlugin("com.Nuxlar.FixREXCrash", "FixREXCrash", "1.0.0")]

  public class FixREXCrash : BaseUnityPlugin
  {

    public void Awake()
    {
      On.EntityStates.Treebot.Weapon.FireSonicBoom.OnEnter += FireSonicBoom_OnEnter;
    }

    private void FireSonicBoom_OnEnter(On.EntityStates.Treebot.Weapon.FireSonicBoom.orig_OnEnter orig, FireSonicBoom self)
    {
      self.duration = self.baseDuration / self.attackSpeedStat;
      self.PlayAnimation("Gesture, Additive", nameof(FireSonicBoom));
      int num1 = (int)Util.PlaySound(self.sound, self.gameObject);
      Ray aimRay = self.GetAimRay();
      if (!string.IsNullOrEmpty(self.muzzle))
        EffectManager.SimpleMuzzleFlash(self.fireEffectPrefab, self.gameObject, self.muzzle, false);
      else
        EffectManager.SpawnEffect(self.fireEffectPrefab, new EffectData()
        {
          origin = aimRay.origin,
          rotation = Quaternion.LookRotation(aimRay.direction)
        }, false);
      aimRay.origin -= aimRay.direction * self.backupDistance;
      if (NetworkServer.active)
      {
        BullseyeSearch bullseyeSearch = new BullseyeSearch();
        bullseyeSearch.teamMaskFilter = TeamMask.all;
        bullseyeSearch.maxAngleFilter = self.fieldOfView * 0.5f;
        bullseyeSearch.maxDistanceFilter = self.maxDistance;
        bullseyeSearch.searchOrigin = aimRay.origin;
        bullseyeSearch.searchDirection = aimRay.direction;
        bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
        bullseyeSearch.filterByLoS = false;
        bullseyeSearch.RefreshCandidates();
        bullseyeSearch.FilterOutGameObject(self.gameObject);
        IEnumerable<HurtBox> hurtBoxes = Enumerable.Where<HurtBox>(bullseyeSearch.GetResults(), new Func<HurtBox, bool>(Util.IsValid)).Distinct<HurtBox>((IEqualityComparer<HurtBox>)new HurtBox.EntityEqualityComparer());
        TeamIndex team = self.GetTeam();
        foreach (HurtBox hurtBox in hurtBoxes)
        {
          if (FriendlyFireManager.ShouldSplashHitProceed(hurtBox.healthComponent, team))
          {
            Vector3 vector3_1 = hurtBox.transform.position - aimRay.origin;
            float magnitude1 = vector3_1.magnitude;
            double magnitude2 = (double)new Vector2(vector3_1.x, vector3_1.z).magnitude;
            Vector3 vector3_2 = vector3_1 / magnitude1;
            float time = 1f;
            CharacterBody body = hurtBox.healthComponent.body;
            if ((bool)(UnityEngine.Object)body.characterMotor)
              time = body.characterMotor.mass;
            else if ((bool)(UnityEngine.Object)hurtBox.healthComponent.GetComponent<Rigidbody>())
              time = self.rigidbody.mass;
            float num2 = FireSonicBoom.shoveSuitabilityCurve.Evaluate(time);
            self.AddDebuff(body);
            body.RecalculateStats();
            float acceleration = double.IsNaN(body.acceleration) ? 1 : body.acceleration;
            Vector3 vector3_3 = (vector3_2 * (Trajectory.CalculateInitialYSpeedForHeight(Mathf.Abs(self.idealDistanceToPlaceTargets - magnitude1), -acceleration) * Mathf.Sign(self.idealDistanceToPlaceTargets - magnitude1))) with
            {
              y = self.liftVelocity
            };
            DamageInfo damageInfo = new DamageInfo()
            {
              attacker = self.gameObject,
              damage = self.CalculateDamage(),
              position = hurtBox.transform.position,
              procCoefficient = self.CalculateProcCoefficient()
            };
            hurtBox.healthComponent.TakeDamageForce(vector3_3 * (time * num2), true, true);
            hurtBox.healthComponent.TakeDamage(new DamageInfo()
            {
              attacker = self.gameObject,
              damage = self.CalculateDamage(),
              position = hurtBox.transform.position,
              procCoefficient = self.CalculateProcCoefficient()
            });
            GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox.healthComponent.gameObject);
          }
        }
      }
      if (!self.isAuthority || !(bool)(UnityEngine.Object)self.characterBody || !(bool)(UnityEngine.Object)self.characterBody.characterMotor)
        return;
      double height = self.characterBody.characterMotor.isGrounded ? (double)self.groundKnockbackDistance : (double)self.airKnockbackDistance;
      float num3 = (bool)(UnityEngine.Object)self.characterBody.characterMotor ? self.characterBody.characterMotor.mass : 1f;
      double gravity = -(double)self.characterBody.acceleration;
      self.characterBody.characterMotor.ApplyForce(-Trajectory.CalculateInitialYSpeedForHeight((float)height, (float)gravity) * num3 * aimRay.direction);
    }

  }
}

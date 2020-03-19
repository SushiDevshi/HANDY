using System;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace Servos.Weapon
{
    public class HEALINGPAIN : BaseState
    {
        public static float baseDuration = .5f;
        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = HEALINGPAIN.baseDuration / this.attackSpeedStat;
            if (isAuthority && NetworkServer.active)
            {
                CharacterMaster characterMaster;
                characterMaster = new MasterSummon
                {
                    masterPrefab = MasterCatalog.FindMasterPrefab("Drone2Master"),
                    position = base.characterBody.footPosition + base.transform.up,
                    rotation = base.transform.rotation,
                    summonerBodyObject = null,
                    ignoreTeamMemberLimit = false,
                    teamIndexOverride = TeamIndex.Monster

                }.Perform();
            }
        }        
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}

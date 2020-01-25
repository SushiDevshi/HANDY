using System;
using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace HANDY.Weapon
{
    public class POWERSAVER : BaseState
    {
        public static float baseDuration = .5f;
        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = POWERSAVER.baseDuration / this.attackSpeedStat;
            HANDY.SendNetworkMessage(base.characterBody.netId, 2);
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
        // Token: 0x06002FF6 RID: 12278 RVA: 0x0000BDAE File Offset: 0x00009FAE
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}

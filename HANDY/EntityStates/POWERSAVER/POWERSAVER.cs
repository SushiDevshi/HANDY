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
            if (isAuthority) //&& NetworkServer.active)
            {
                if (base.characterBody.networkIdentity.netId.Value > 0)
                {
                    HANDY.SendNetworkMessage(base.characterBody.networkIdentity.netId, 2);
                }
                /*
                if (base.characterBody.master && base.characterBody.inventory)
                {
                    CharacterMaster characterMaster;
                    characterMaster = new MasterSummon
                    {
                        masterPrefab = MasterCatalog.FindMasterPrefab("Drone2Master"),
                        position = base.characterBody.footPosition + base.transform.up,
                        rotation = base.transform.rotation,
                        summonerBodyObject = null,
                        ignoreTeamMemberLimit = false,
                        teamIndexOverride = TeamIndex.Player

                    }.Perform();


                    if (base.characterBody.inventory && characterMaster.inventory)
                    {
                        characterMaster.inventory.CopyItemsFrom(base.characterBody.inventory);
                        characterMaster.inventory.ResetItem(ItemIndex.AutoCastEquipment);
                        characterMaster.inventory.ResetItem(ItemIndex.BeetleGland);
                        characterMaster.inventory.ResetItem(ItemIndex.ExtraLife);
                        characterMaster.inventory.ResetItem(ItemIndex.ExtraLifeConsumed);
                        characterMaster.inventory.ResetItem(ItemIndex.FallBoots);
                        characterMaster.inventory.ResetItem(ItemIndex.TonicAffliction);
                        characterMaster.inventory.ResetItem(ItemIndex.ExplodeOnDeath);
                    }

                    AIOwnership component4 = characterMaster.gameObject.GetComponent<AIOwnership>();
                    BaseAI component5 = characterMaster.gameObject.GetComponent<BaseAI>();

                    if (characterMaster.gameObject && characterMaster.bodyPrefab && HANDY.HANDHealingDrone)
                    {
                        characterMaster.gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = 8f;
                        characterMaster.bodyPrefab = HANDY.HANDHealingDrone;
                        characterMaster.Respawn(characterMaster.GetBody().footPosition + Vector3.up + Vector3.up, Quaternion.identity);
                    }
                    if (component4 && base.characterBody.master)
                    {
                        component4.ownerMaster = base.characterBody.master;
                    }
                    if (component5 && base.characterBody.master.gameObject)
                    {
                        component5.leader.gameObject = base.characterBody.master.gameObject;
                        component5.isHealer = true;
                        component5.fullVision = true;
                    }
                }*/
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

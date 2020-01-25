using BepInEx;
using EntityStates;
using HANDY.HAND;
using HANDY.Weapon;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Skills;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using static HANDY.Helpers;

namespace HANDY
{

    [R2APISubmoduleDependency(new string[]
    {
        "LoadoutAPI",
        "PrefabAPI",
        "SurvivorAPI",
        "AssetPlus",
    })]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.SushiDev.HAND", "HAN-D", "1.0.0")]

    public class HANDY : BaseUnityPlugin
    {
        public static GameObject HANDDrone { get; private set; }
        public static GameObject HANDHealingDrone { get; private set; }
        public void Awake()
        {
            GameObject HAND = Resources.Load<GameObject>("Prefabs/CharacterBodies/HANDBody").InstantiateClone("HAND_CLONE", true);
            HANDDrone = Resources.Load<GameObject>("Prefabs/CharacterBodies/Drone1Body").InstantiateClone("HAND_DRONE_CLONE", true);
            HANDHealingDrone = Resources.Load<GameObject>("Prefabs/CharacterBodies/Drone2Body").InstantiateClone("HAND_DRONEHEALER_CLONE", true);

            RegisterNewBody(HAND);
            RegisterNewBody(HANDDrone);
            RegisterNewBody(HANDHealingDrone);

            DontDestroyOnLoad(HANDDrone);
            DontDestroyOnLoad(HANDHealingDrone);

            GlobalEventManager.onCharacterDeathGlobal += GlobalEventManagerOnOnCharacterDeath;

            var display = HAND.GetComponent<ModelLocator>().modelTransform.gameObject;

            CharacterBody healingdronecharacterBody = HANDHealingDrone.GetComponent<CharacterBody>();
            CharacterBody dronecharacterBody = HANDDrone.GetComponent<CharacterBody>();

            CharacterBody characterBody = HAND.GetComponent<CharacterBody>();
            SkillLocator skillLocator = HAND.GetComponent<SkillLocator>();
            CharacterMotor characterMotor = HAND.GetComponent<CharacterMotor>();
            ModelLocator characterModel = HAND.GetComponent<ModelLocator>();
            CharacterDirection characterDirection = HAND.GetComponent<CharacterDirection>();

            display.AddComponent<HANDDisplayAnimation>();
            HAND.AddComponent<HANDOverclockController>();

            SetStateOnHurt hurtState = HAND.AddComponent<SetStateOnHurt>();

            SurvivorDef item = new SurvivorDef
            {
                //We're finding the body prefab here,
                bodyPrefab = HAND,
                //Description
                descriptionToken = "MANUAL NOT INCLUDED",
                //Display 
                displayPrefab = display,
                //Color on the select screen
                primaryColor = new Color(0.8039216f, 0.482352942f, 0.843137264f),
                //does literally nothing useful 
            };
            SurvivorAPI.AddSurvivor(item);

            R2API.AssetPlus.Languages.AddToken("HAND_CLONE_NAME_TOKEN", "HAN-D");
            R2API.AssetPlus.Languages.AddToken("HANDDRONE_CLONE_NAME_TOKEN", "HAN-D Gunner Drone");
            R2API.AssetPlus.Languages.AddToken("HANDDRONEHEALER_CLONE_NAME_TOKEN", "HAN-D Healing Drone");
            R2API.AssetPlus.Languages.AddToken("HAND_PRIMARY_NAME", "HURT");
            R2API.AssetPlus.Languages.AddToken("HAND_PRIMARY_DESCRIPTION", "APPLY FORCE TO ALL COMBATANTS FOR <color=#E5C962>550% DAMAGE.</color>");
            R2API.AssetPlus.Languages.AddToken("HAND_SECONDARY_NAME", "DRONE");
            R2API.AssetPlus.Languages.AddToken("HAND_SECONDARY_DESCRIPTION", "RELEASE A HEALING DRONE <style=cIsUtility>LIVES FOR 25 SECONDS</style>");
            R2API.AssetPlus.Languages.AddToken("HAND_UTILITY_NAME", "OVERCLOCK");
            R2API.AssetPlus.Languages.AddToken("HAND_UTILITY_DESCRIPTION", "INCREASE <color=#E5C962>ATTACK SPEED AND DAMAGE, AND SUMMON TEMPORARY DRONES ON COMBATANT DEATH. </color> ALL ATTACKS <color=#9CE562>HEAL 10% OF DAMAGE DONE</color>. <color=#95CDE5>INCREASE DURATION BY KILLING COMBATANTS.</color>");
            R2API.AssetPlus.Languages.AddToken("HAND_SPECIAL_NAME", "FORCED_REASSEMBLY");
            R2API.AssetPlus.Languages.AddToken("HAND_SPECIAL_DESCRIPTION", "APPLY GREAT FORCE TO THE GROUND, CAUSING AN EARTHQUAKE TO FORM. DEALS <color=#E5C962>600% DAMAGE</color> TO ENEMIES CAUGHT IN THE IMPACT. CAUSES <color=#E5C962>250% DAMAGE</color> TO ENEMIES CAUGHT IN THE EARTHQUAKE.");
            R2API.AssetPlus.Languages.AddToken("HAND_SPECIAL_ALT_NAME", "FORCED_REASSEMBLY");
            R2API.AssetPlus.Languages.AddToken("HAND_SPECIAL_ALT_DESCRIPTION", "APPLY GREAT FORCE TO THE GROUND, AND DEAL <color=#E5C962>600% DAMAGE</color> TO ENEMIES CAUGHT IN THE IMPACT.");

            /*GameObject prefab = Resources.Load<GameObject>("prefabs/characterbodies/Drone1Body");

            foreach (Component c in prefab.GetComponents<Component>())
            {
                Debug.Log("P================================");
                Debug.Log("Name: " + c.name + " Type: " + c.GetType().ToString());
                Debug.Log("================================P");
            }*/

            characterMotor.mass = 250;
            characterDirection.turnSpeed = 320;

            characterBody.baseAcceleration = 80f;
            characterBody.baseJumpPower = 16;
            characterBody.baseArmor = 25;
            characterBody.preferredPodPrefab = Resources.Load<GameObject>("prefabs/networkedobjects/robocratepod");
            characterBody.baseDamage = 14;
            characterBody.levelDamage = 2.4f;
            characterBody.baseMoveSpeed = 7;
            characterBody.baseMaxHealth = 300;
            characterBody.levelMaxHealth = 80;
            characterBody.baseNameToken = "HAND_CLONE_NAME_TOKEN";
            characterBody.subtitleNameToken = "New Servos";
            characterBody.crosshairPrefab = Resources.Load<GameObject>("Prefabs/CharacterBodies/HuntressBody").GetComponent<CharacterBody>().crosshairPrefab;

            CharacterDeathBehavior characterDeathBehavior = HANDDrone.GetComponent<CharacterDeathBehavior>();
            characterDeathBehavior.enabled = false;

            CharacterDeathBehavior healerDeathBehavior = HANDHealingDrone.GetComponent<CharacterDeathBehavior>();
            healerDeathBehavior.enabled = false;

            dronecharacterBody.baseNameToken = "HANDDRONE_CLONE_NAME_TOKEN";
            healingdronecharacterBody.baseNameToken = "HANDDRONEHEALER_CLONE_NAME_TOKEN";

            characterBody.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;
            characterBody.bodyFlags = CharacterBody.BodyFlags.SprintAnyDirection;
            characterBody.bodyFlags = CharacterBody.BodyFlags.ResistantToAOE;
            characterBody.bodyFlags = CharacterBody.BodyFlags.Mechanical;

            hurtState.canBeFrozen = true;
            hurtState.canBeHitStunned = false;
            hurtState.canBeStunned = false;
            hurtState.hitThreshold = 5f;

            characterModel.modelBaseTransform.transform.localScale = characterModel.modelBaseTransform.transform.localScale * 2;

            int i = 0;
            EntityStateMachine[] esmr = new EntityStateMachine[2];
            foreach (EntityStateMachine esm in HAND.GetComponentsInChildren<EntityStateMachine>())
            {
                switch (esm.customName)
                {
                    case "Body":
                        hurtState.targetStateMachine = esm;
                        break;
                    default:
                        if (i < 2)
                        {
                            esmr[i] = esm;
                            Debug.Log(esm.customName);
                        }
                        i++;
                        Debug.Log(i);
                        break;

                }
            }

            //GlobalEventManager.onCharacterDeathGlobal += GlobalEventManagerOnOnCharacterDeath;
            LoadoutAPI.AddSkill(typeof(HURT));
            LoadoutAPI.AddSkill(typeof(CHARGESLAM));
            LoadoutAPI.AddSkill(typeof(SLAM));
            LoadoutAPI.AddSkill(typeof(CHARGEBIGSLAM));
            LoadoutAPI.AddSkill(typeof(BIGSLAM));
            LoadoutAPI.AddSkill(typeof(OVERCLOCK));
            LoadoutAPI.AddSkill(typeof(POWERSAVER));

            SkillFamily primaryskillFamily = skillLocator.primary.skillFamily;
            SkillFamily secondaryskillFamily = skillLocator.secondary.skillFamily;
            SkillFamily utilityskillFamily = skillLocator.utility.skillFamily;
            SkillFamily specialskillFamily = skillLocator.special.skillFamily;
            SkillDef Primary = primaryskillFamily.variants[primaryskillFamily.defaultVariantIndex].skillDef;
            SkillDef Secondary = secondaryskillFamily.variants[secondaryskillFamily.defaultVariantIndex].skillDef;
            SkillDef Utility = utilityskillFamily.variants[utilityskillFamily.defaultVariantIndex].skillDef;
            SkillDef Special = specialskillFamily.variants[specialskillFamily.defaultVariantIndex].skillDef;

            var Special_Variants = specialskillFamily.variants;

            On.RoR2.CharacterModel.EnableItemDisplay += delegate (On.RoR2.CharacterModel.orig_EnableItemDisplay orig, CharacterModel self, ItemIndex itemIndex)
            {
                if (itemIndex != ItemIndex.Bear || self.name != "mdlHAND")
                {
                    orig(self, itemIndex);
                };
            };

            //Primary
            Primary.noSprint = false;
            Primary.canceledFromSprinting = false;
            Primary.baseRechargeInterval = 0;
            Primary.baseMaxStock = 1;
            Primary.rechargeStock = 1;
            Primary.shootDelay = 0.1f;
            Primary.beginSkillCooldownOnSkillEnd = false;
            Primary.isCombatSkill = true;
            Primary.mustKeyPress = false;
            Primary.requiredStock = 1;
            Primary.stockToConsume = 1;
            Primary.skillNameToken = "HAND_PRIMARY_NAME";
            Primary.skillDescriptionToken = "HAND_PRIMARY_DESCRIPTION";
            Primary.activationState = new EntityStates.SerializableEntityStateType(typeof(HURT));
            //Secondary
            Secondary.noSprint = false;
            Secondary.canceledFromSprinting = false;
            Secondary.baseRechargeInterval = 10;
            Secondary.baseMaxStock = 3;
            Secondary.rechargeStock = 1;
            Secondary.requiredStock = 1;
            Secondary.stockToConsume = 1;
            Secondary.isCombatSkill = true;
            Secondary.mustKeyPress = false;
            Secondary.isBullets = false;
            Secondary.shootDelay = 0.08f;
            Secondary.skillNameToken = "HAND_SECONDARY_NAME";
            Secondary.skillDescriptionToken = "HAND_SECONDARY_DESCRIPTION";
            Secondary.activationState = new EntityStates.SerializableEntityStateType(typeof(POWERSAVER));

            //Utility 
            Utility.baseRechargeInterval = 15;
            Utility.noSprint = false;
            Utility.baseMaxStock = 1;
            Utility.isCombatSkill = false;
            Utility.canceledFromSprinting = false;
            Utility.rechargeStock = 1;
            Utility.requiredStock = 1;
            Utility.stockToConsume = 1;
            Utility.isBullets = false;
            Utility.shootDelay = 0.08f;
            Utility.skillNameToken = "HAND_UTILITY_NAME";
            Utility.skillDescriptionToken = "HAND_UTILITY_DESCRIPTION";
            Utility.activationState = new EntityStates.SerializableEntityStateType(typeof(OVERCLOCK));

            //Special
            Special.baseRechargeInterval = 8;
            Special.rechargeStock = 1;
            Special.noSprint = false;
            Special.beginSkillCooldownOnSkillEnd = true;
            Special.stockToConsume = 1;
            Special.requiredStock = 1;
            Special.baseMaxStock = 1;
            Special.canceledFromSprinting = false;
            Special.skillNameToken = "HAND_SPECIAL_NAME";
            Special.skillDescriptionToken = "HAND_SPECIAL_DESCRIPTION";
            Special.activationState = new EntityStates.SerializableEntityStateType(typeof(CHARGESLAM));

            var LoadoutSpecial = ScriptableObject.CreateInstance<SkillDef>();
            LoadoutSpecial.activationState = new EntityStates.SerializableEntityStateType(typeof(CHARGEBIGSLAM));
            LoadoutSpecial.baseMaxStock = 1;
            LoadoutSpecial.baseRechargeInterval = 12;
            LoadoutSpecial.beginSkillCooldownOnSkillEnd = true;
            LoadoutSpecial.canceledFromSprinting = true;    
            LoadoutSpecial.fullRestockOnAssign = false;
            LoadoutSpecial.isBullets = false;
            LoadoutSpecial.isCombatSkill = true;
            //LoadoutSpecial.mustKeyPress = true;
            LoadoutSpecial.noSprint = true;
            LoadoutSpecial.rechargeStock = 1;
            LoadoutSpecial.requiredStock = 1;
            LoadoutSpecial.shootDelay = 0.1f;
            LoadoutSpecial.activationStateMachineName = specialskillFamily.defaultSkillDef.activationStateMachineName;
            LoadoutSpecial.skillDescriptionToken = "HAND_SPECIAL_ALT_DESCRIPTION";
            LoadoutSpecial.skillNameToken = "HAND_SPECIAL_ALT_NAME";
            LoadoutSpecial.skillName = "HAND_SPECIAL_ALT_NAME";
            LoadoutSpecial.stockToConsume = 1;

            LoadoutAPI.AddSkillDef(LoadoutSpecial);
            SkillFamily.Variant LoadoutSpecialVariant = new SkillFamily.Variant
            {
                skillDef = LoadoutSpecial,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node("R", false)
            };
            Array.Resize<SkillFamily.Variant>(ref Special_Variants, Special_Variants.Length + 1);
            Special_Variants[Special_Variants.Length - 1] = LoadoutSpecialVariant;
            specialskillFamily.variants = Special_Variants;
        }
        /*private void GlobalEventManagerOnOnCharacterDeath(DamageReport damageReport)
        {
            if (damageReport.attackerBody.baseNameToken == "")
            {
            }
        }*/
        private void GlobalEventManagerOnOnCharacterDeath(DamageReport damageReport)
        {

            if (damageReport.attackerBody.isPlayerControlled)
            {
                if (damageReport.attackerBody)
                {
                    if (damageReport.attackerMaster)
                    {
                        if (damageReport.attackerBody)
                        {
                            if (damageReport.attackerMaster != null)
                            {
                                if (damageReport.victimBody != null)
                                {
                                    if (damageReport.victimMaster)
                                    {
                                        if (damageReport.victimBody != null)
                                        {
                                            if (damageReport.victimMaster != null)
                                            {
                                                if (damageReport.victimBody.baseNameToken != "NULLIFIER_BODY_NAME")
                                                {
                                                    if (damageReport.victimBody.baseNameToken != "JELLYFISH_BODY_NAME")
                                                    {
                                                        if (damageReport.victimBody.baseNameToken != "HANDDRONE_CLONE_NAME_TOKEN")
                                                        {
                                                            if (damageReport.victimBody.baseNameToken != "HANDDRONEHEALER_CLONE_NAME_TOKEN")
                                                            {
                                                                if (damageReport.victimBody.baseNameToken != "DRONE_GUNNER_BODY_NAME")
                                                                {
                                                                    if (damageReport.victimBody.baseNameToken != "DRONE_HEALING_BODY_NAME")
                                                                    {
                                                                        if (damageReport.attackerBody.baseNameToken == "HAND_CLONE_NAME_TOKEN")
                                                                        {
                                                                            if (damageReport.attackerBody.baseNameToken == "HAND_CLONE_NAME_TOKEN" && damageReport.attacker.GetComponent<HANDOverclockController>().overclockOn)
                                                                            {
                                                                                damageReport.attacker.GetComponent<HANDOverclockController>().AddDurationOnHit();
                                                                                damageReport.attackerBody.healthComponent.Heal((damageReport.damageDealt / 15) * 100, default);
                                                                                HANDY.SendNetworkMessage(damageReport.attackerBody.netId, 1);
                                                                               
                                                                            };
                                                                        };
                                                                    };
                                                                };
                                                            };
                                                        };
                                                    };
                                                };
                                            };
                                        };
                                    };
                                };
                            };
                        };
                    };
                };
            };
        }

        
     public const Int16 HandleId = 265;

        public class MyMessage : MessageBase
        {
            public NetworkInstanceId objectID;
            public int summonType;

            public override void Serialize(NetworkWriter writer)
            {
                writer.Write(objectID);
                writer.Write(summonType);
            }

            public override void Deserialize(NetworkReader reader)
            {
                objectID = reader.ReadNetworkId();
                summonType = reader.ReadInt32();
            }
        }

        public static void SendNetworkMessage(NetworkInstanceId myObjectID, int summoningType)
        {
            NetworkServer.SendToAll(HandleId, new MyMessage
            {
                objectID = myObjectID,
                summonType = summoningType
            });
        }

        [RoR2.Networking.NetworkMessageHandler(msgType = HandleId, client = true)]
        public static void HandleDropItem(NetworkMessage netMsg)
        {
            var MyMessage = netMsg.ReadMessage<MyMessage>();


            if (NetworkServer.active)
            {
                CharacterBody characterBody = ClientScene.FindLocalObject(MyMessage.objectID).GetComponent<CharacterBody>();
                CharacterMaster characterMaster;
                if (characterBody)
                {
                    {
                        if (MyMessage.summonType == 1) 
                        {

                            characterMaster = new MasterSummon
                            {
                                masterPrefab = MasterCatalog.FindMasterPrefab("Drone1Master"),
                                position = characterBody.footPosition + characterBody.transform.up,
                                rotation = characterBody.transform.rotation,
                                summonerBodyObject = null,
                                ignoreTeamMemberLimit = false,
                                teamIndexOverride = TeamIndex.Neutral

                            }.Perform();



                            characterMaster.bodyPrefab = BodyCatalog.FindBodyPrefab("Drone1Body");
                            characterMaster.Respawn(characterMaster.GetBody().footPosition + Vector3.up + Vector3.up, Quaternion.identity);

                            characterMaster.inventory.CopyItemsFrom(characterBody.inventory);

                            characterMaster.inventory.ResetItem(ItemIndex.AutoCastEquipment);
                            characterMaster.inventory.ResetItem(ItemIndex.BeetleGland);
                            characterMaster.inventory.ResetItem(ItemIndex.ExtraLife);
                            characterMaster.inventory.ResetItem(ItemIndex.ExtraLifeConsumed);
                            characterMaster.inventory.ResetItem(ItemIndex.FallBoots);
                            characterMaster.inventory.ResetItem(ItemIndex.TonicAffliction);
                            characterMaster.inventory.ResetItem(ItemIndex.ExplodeOnDeath);

                            characterMaster.inventory.CopyEquipmentFrom(characterBody.inventory);

                        }
                        if (MyMessage.summonType == 2) 
                        {

                            characterMaster = new MasterSummon
                            {
                                masterPrefab = MasterCatalog.FindMasterPrefab("Drone2Master"),
                                position = characterBody.footPosition + characterBody.transform.up,
                                rotation = characterBody.transform.rotation,
                                summonerBodyObject = null,
                                ignoreTeamMemberLimit = false,
                                teamIndexOverride = TeamIndex.Player

                            }.Perform();



                            characterMaster.bodyPrefab = BodyCatalog.FindBodyPrefab("Drone2Body");
                            characterMaster.Respawn(characterMaster.GetBody().footPosition + Vector3.up + Vector3.up, Quaternion.identity);

                            characterMaster.inventory.CopyItemsFrom(characterBody.inventory);
                            characterMaster.inventory.ResetItem(ItemIndex.AutoCastEquipment);
                            characterMaster.inventory.ResetItem(ItemIndex.BeetleGland);
                            characterMaster.inventory.ResetItem(ItemIndex.ExtraLife);
                            characterMaster.inventory.ResetItem(ItemIndex.ExtraLifeConsumed);
                            characterMaster.inventory.ResetItem(ItemIndex.FallBoots);
                            characterMaster.inventory.ResetItem(ItemIndex.TonicAffliction);
                            characterMaster.inventory.ResetItem(ItemIndex.ExplodeOnDeath);

                            characterMaster.inventory.CopyEquipmentFrom(characterBody.inventory);
                        }
                    }
                }
            }

        }

        private class HANDDisplayAnimation : MonoBehaviour
        {

            internal void OnEnable()
            {
                if (gameObject.transform.parent.gameObject.name == "CharacterPad")
                {
                    Debug.Log("animation");
                    var animator = gameObject.GetComponent<Animator>();
                    Shooting(animator);

                }
                else
                {
                    Debug.Log("no animation");
                }
            }

            private void Shooting(Animator animator)
            {
                PlayAnimation("Gesture", "ChargeSlam", "ChargeSlam.playbackRate", EntityStates.HAND.Weapon.ChargeSlam.baseDuration, animator);

                var coroutine = Fire(animator);
                StartCoroutine(coroutine);
            }

            IEnumerator Fire(Animator animator)
            {
                yield return new WaitForSeconds(0.5f);

                PlayAnimation("Gesture", "Slam", "Slam.playbackRate", EntityStates.HAND.Weapon.Slam.baseDuration, animator);

                Destroy(this);
            }

            private void PlayAnimation(string layerName, string animationStateName, string playbackRateParam, float duration, Animator animator)
            {
                int layerIndex = animator.GetLayerIndex(layerName);
                animator.SetFloat(playbackRateParam, 1f);
                animator.PlayInFixedTime(animationStateName, layerIndex, 0f);
                animator.Update(0f);
                float length = animator.GetCurrentAnimatorStateInfo(layerIndex).length;
                animator.SetFloat(playbackRateParam, length / duration);
                Util.PlaySound("Play_MULT_shift_hit", this.gameObject);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RC_save_editor{
    class SaveGameInstance{
        public SaveGameInstance(){
            // populate datafields
            datafields["version"] = "30";
            datafields["randomSeedForRun"] = "123";
            datafields["startCredits"] = "2000";
            datafields["dropPodCount"] = "3";
            datafields["coins"] = "0";
            datafields["rerollCount"] = "0";
            datafields["researchPoints"] = "5";
            datafields["secondsPlayedOverall"] = "0";
            datafields["finishedLevelCount"] = "0";
            datafields["finishedMiniBossCount"] = "0";
        }

        // dynamic fields (because i cant be bothered)
        public Dictionary<string, string> datafields = new();

        // static fields, since they will all posses unique traits
        public string engineer = "ReachEngineer";
        public List<string> blueprints = new();
        public List<string> drops = new();
        public List<string> specialists = new();
        public List<string> relics = new();
        public List<KeyValuePair<string, List<string>>> upgrades = new();

        // unk fields
        public List<string> research_branches = new();

        // stage map
        public int currentStage = 0;
        public List<StageMap> stages = new();

        public class StageMap{
            // NOTE: level = row
            // NOTE: field = column

            public int coinReward = 100;
            public int startCrystalReward = 200;
            public int researchPointsReward = 5;

            public int currentLevel = 4;
            public int currentField = 0;
            public int chosenField = -1;
            public bool isCurrentLevelFinished = false;

            public string bossAiPrefabName = "_01_CF Hordes Enemy - constant weak attacks, some big waves, learn crowd control and AoE Variant";
            public string bossWorldPrefabName = "AcidWorld";
            public string bossMapScriptableObjectName = "27 Circles";
            
            public int levelCount = 4;
            public int maxWidth = 5;

            public RewardParameters cardRewardParameters = new();
            public RewardParameters upgradeRewardParameters = new();
            public RewardParameters relicRewardParameters = new();
            public RewardParameters researchRewardParameters = new();

            public string shopScenes = ""; // NOTE: we don't even bother deserializing this, just copy the inner json text

            // basic stuff connection stuff
            public List<List<uint>> fieldsPerLevel = new();
            public List<List<KeyValuePair<uint, uint>>> connectionsPerLevel = new();
            public List<uint> chosenPath = new();

            // field slot types
            public List<List<uint>> shopsPerLevel = new();
            public List<List<uint>> cardRewardsPerLevel = new();
            public List<List<uint>> upgradeRewardsPerLevel = new();
            public List<List<uint>> relicRewardsPerLevel = new();
            public List<List<uint>> coinRewardsPerLevel = new();
            public List<List<uint>> startCrystalRewardsPerLevel = new();
            public List<List<uint>> dropRewardsPerLevel = new();
            public List<List<uint>> restSitesPerLevel = new();
            public List<List<uint>> researchPointsRewardsPerLevel = new();
            public List<List<uint>> researchRewardsPerLevel = new();

            public class RewardParameters{
                public int rarity = 0;
                public float rareProbability = 0.3f;
                public float ultraRareProbability = 0.3f;
                public RewardParameters() { }
                public RewardParameters(RewardParameters source){
                    rarity = source.rarity;
                    rareProbability = source.rareProbability;
                    ultraRareProbability = source.ultraRareProbability;
                }
            }
            
            public StageMap(){ }
            public StageMap(StageMap source){
                static List<List<T>> Clone2D<T>(List<List<T>> list) { 
                    List<List<T>> clone = new List<List<T>>(); 
                    foreach (List<T> innerList in list) 
                        clone.Add(new List<T>(innerList)); 
                    return clone;
                }

                coinReward = source.coinReward;
                startCrystalReward = source.startCrystalReward;
                researchPointsReward = source.researchPointsReward;
                currentLevel = source.currentLevel;
                currentField = source.currentField;
                chosenField = source.chosenField;
                isCurrentLevelFinished = source.isCurrentLevelFinished;
                bossAiPrefabName = source.bossAiPrefabName;
                bossWorldPrefabName = source.bossWorldPrefabName;
                bossMapScriptableObjectName = source.bossMapScriptableObjectName;
                levelCount = source.levelCount;
                maxWidth = source.maxWidth;
                cardRewardParameters = new RewardParameters(source.cardRewardParameters);
                upgradeRewardParameters = new RewardParameters(source.upgradeRewardParameters);
                relicRewardParameters = new RewardParameters(source.relicRewardParameters);
                researchRewardParameters = new RewardParameters(source.researchRewardParameters);
                shopScenes = source.shopScenes;
                chosenPath = new List<uint>(source.chosenPath);

                fieldsPerLevel = Clone2D(source.fieldsPerLevel);
                connectionsPerLevel = Clone2D(source.connectionsPerLevel);

                shopsPerLevel = Clone2D(source.shopsPerLevel); // red
                cardRewardsPerLevel = Clone2D(source.cardRewardsPerLevel); // purple
                upgradeRewardsPerLevel = Clone2D(source.upgradeRewardsPerLevel); // dark blue
                relicRewardsPerLevel = Clone2D(source.relicRewardsPerLevel); // dark green
                coinRewardsPerLevel = Clone2D(source.coinRewardsPerLevel); // orange
                startCrystalRewardsPerLevel = Clone2D(source.startCrystalRewardsPerLevel); // light blue
                dropRewardsPerLevel = Clone2D(source.dropRewardsPerLevel); // light green
                restSitesPerLevel = Clone2D(source.restSitesPerLevel); // pink
                researchPointsRewardsPerLevel = Clone2D(source.researchPointsRewardsPerLevel); // yellow
                researchRewardsPerLevel = Clone2D(source.researchRewardsPerLevel); // black

            }
        }
    }
}

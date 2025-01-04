using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string stage_map = ""; // we don't even bother deserializing this, just copy the inner json text

    }
}

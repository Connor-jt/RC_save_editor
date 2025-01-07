using ArmanDoesStuff.Utilities;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.Json;
using static RC_save_editor.DataStuff;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel;
using Microsoft.Win32;
using System;
using doody;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using static RC_save_editor.SaveGameInstance;



namespace RC_save_editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            try{
                LoadAllIDs("id_data\\");
                AutoFetchGamePath_If_FirstTime();
            } catch (Exception ex){ NavigateToOutput(ex.ToString());}
        }
        SaveGameInstance savegame = new();

        #region savefile compile/decomp
        private void Menu_LoadPacked(object sender, RoutedEventArgs e){
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".dat";
            openFileDialog.Filter = "RC savegame files (.dat)|*.dat";
            if (openFileDialog.ShowDialog() == true)
                LoadEncrypted_Path(openFileDialog.FileName);
        }
        private void Menu_LoadJson(object sender, RoutedEventArgs e){
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".json";
            openFileDialog.Filter = "Exported RC savegame files (.json)|*.json";
            if (openFileDialog.ShowDialog() == true)
                LoadJsonPath(openFileDialog.FileName);
        }
        private void Menu_SaveJson(object sender, RoutedEventArgs e){
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "savegame";
            saveFileDialog.DefaultExt = ".json";
            saveFileDialog.Filter = "Exported RC savegame files (.json)|*.json";
            if (saveFileDialog.ShowDialog() == true)
                WriteJson(saveFileDialog.FileName);
        }
        private void Menu_SavePacked(object sender, RoutedEventArgs e){
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "savegame";
            saveFileDialog.DefaultExt = ".dat";
            saveFileDialog.Filter = "RC savegame files (.dat)|*.dat";
            if (saveFileDialog.ShowDialog() == true)
                WriteSaveGameFile(saveFileDialog.FileName);
        }
        private void Menu_ConvertDat(object sender, RoutedEventArgs e){
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".dat";
            openFileDialog.Filter = "RC savegame files (.dat)|*.dat";
            if (openFileDialog.ShowDialog() == true){
                try{
		            File.WriteAllText(openFileDialog.FileName + ".json", EncrypterAES.DecryptStringFromBytes_Aes(File.ReadAllBytes(openFileDialog.FileName)));
                } catch (Exception ex){ NavigateToOutput(ex.ToString()); }
            }
        }
        private void Menu_ConvertJson(object sender, RoutedEventArgs e){
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".json";
            openFileDialog.Filter = "Exported RC savegame files (.json)|*.json";
            if (openFileDialog.ShowDialog() == true){
                try{
		            File.WriteAllBytes(openFileDialog.FileName + ".dat", EncrypterAES.EncryptStringToBytes_Aes(File.ReadAllText(openFileDialog.FileName)));
                } catch (Exception ex){ NavigateToOutput(ex.ToString()); }
            }
        }
        private void Menu_TestJson(object sender, RoutedEventArgs e){
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.DefaultExt = ".json";
            openFileDialog.Filter = "Exported RC savegame files (.json)|*.json";
            if (openFileDialog.ShowDialog() == true){
                try{
                    FromJson.JsonValidityTest(File.ReadAllText(openFileDialog.FileName));
                    throw new Exception("Validity check all good. File should be compatible with the game.");
                } catch (Exception ex){ NavigateToOutput(ex.ToString()); }
            }
        }


        void LoadEncrypted_Path(string path)
	    => LoadJson(EncrypterAES.DecryptStringFromBytes_Aes(File.ReadAllBytes(path)));
        void LoadJsonPath(string path)
        => LoadJson(File.ReadAllText(path));
        void LoadJson(string json_contents){
            try{
                field_panel.Children.Clear();
                savegame = new();

                dynamic? data = JsonConvert.DeserializeObject(json_contents);
                if (data == null) throw new Exception("failed to deserialize!");

                foreach (var item in data){
                    switch (item.Name){
                        // default data field cases
                        case "secondsPlayedOverall":
                            savegame.datafields[item.Name] = item.Value.Value.ToString();
                            EditField float_editField = new EditField(item.Name, item.Value.Value.ToString(), this, true);
                            field_panel.Children.Add(float_editField);
                            break;
                        default:
                            savegame.datafields[item.Name] = item.Value.Value.ToString();
                            // create UI interface??
                            EditField editField = new EditField(item.Name, item.Value.Value.ToString(), this, false);
                            field_panel.Children.Add(editField);
                            break;
                        // unique static fields
                        case "engineer":
                            savegame.engineer = item.Value.Value;
                            break;
                        case "specialists":
                            foreach (var specialist in item.Value)
                                savegame.specialists.Add(specialist.Value);
                            break;
                        case "blueprints":
                            foreach (var blueprint in item.Value)
                                savegame.blueprints.Add(blueprint.Value);
                            break;
                        case "relics":
                            foreach (var relic in item.Value)
                                savegame.relics.Add(relic.Value);
                            break;
                        case "drops":
                            foreach (var drop in item.Value)
                                savegame.drops.Add(drop.Value);
                            break;
                        case "cardUpgrades":
                            foreach (var upgrade_unit in item.Value){
                                KeyValuePair<string, List<string>> upgrade_entry = new(upgrade_unit[0].Value, new List<string>());
                                foreach (var upgrade in upgrade_unit[1])
                                    upgrade_entry.Value.Add(upgrade.Value);
                                savegame.upgrades.Add(upgrade_entry);
                            }
                            break;
                        // unsupported cases
                        case "researchBranches":
                            break;
                        case "stageMap":
                            // TODO: remove once we have a proper stage map editor
                            savegame.stage_map = item.Value.ToString(Formatting.None); // this should hopefully just yoink the inner json text
                            
                            savegame.currentStage = (int)item.Value.currentStage.Value;
                            foreach (var stage in item.Value.levelMaps){
                                StageMap new_stage = new StageMap();
                                new_stage.chosenField = (int)stage.chosenField.Value;
                                new_stage.levelCount = (int)stage.levelCount.Value;
                                new_stage.maxWidth = (int)stage.maxWidth.Value;
                                new_stage.coinReward = (int)stage.coinReward.Value;
                                new_stage.startCrystalReward = (int)stage.startCrystalReward.Value;
                                new_stage.researchPointsReward = (int)stage.researchPointsReward.Value;
                                new_stage.currentLevel = (int)stage.currentLevel.Value;
                                new_stage.currentField = (int)stage.currentField.Value;
                                new_stage.isCurrentLevelFinished = (bool)stage.isCurrentLevelFinished.Value;
                                new_stage.bossAiPrefabName = (string)stage.bossAiPrefabName.Value;
                                new_stage.bossWorldPrefabName = (string)stage.bossWorldPrefabName.Value;
                                new_stage.bossMapScriptableObjectName = (string)stage.bossMapScriptableObjectName.Value;

                                new_stage.cardRewardParameters.rarity = (int)stage.cardRewardParameters.rarity.Value;
                                new_stage.cardRewardParameters.rareProbability = (float)stage.cardRewardParameters.rareProbability.Value;
                                new_stage.cardRewardParameters.ultraRareProbability = (float)stage.cardRewardParameters.ultraRareProbability.Value;
                                new_stage.upgradeRewardParameters.rarity = (int)stage.upgradeRewardParameters.rarity.Value;
                                new_stage.upgradeRewardParameters.rareProbability = (float)stage.upgradeRewardParameters.rareProbability.Value;
                                new_stage.upgradeRewardParameters.ultraRareProbability = (float)stage.upgradeRewardParameters.ultraRareProbability.Value;
                                new_stage.relicRewardParameters.rarity = (int)stage.relicRewardParameters.rarity.Value;
                                new_stage.relicRewardParameters.rareProbability = (float)stage.relicRewardParameters.rareProbability.Value;
                                new_stage.relicRewardParameters.ultraRareProbability = (float)stage.relicRewardParameters.ultraRareProbability.Value;
                                new_stage.researchRewardParameters.rarity = (int)stage.researchRewardParameters.rarity.Value;
                                new_stage.researchRewardParameters.rareProbability = (float)stage.researchRewardParameters.rareProbability.Value;
                                new_stage.researchRewardParameters.ultraRareProbability = (float)stage.researchRewardParameters.ultraRareProbability.Value;

                                new_stage.shopScenes = stage.shopScenes.ToString(Formatting.None); // dont bother processing, just copy the inner json so we can paste it back in later
                                
                                foreach (var row in stage.connectionsPerLevel){
                                    List<KeyValuePair<uint, uint>> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add(new KeyValuePair<uint, uint>((uint)column[0], (uint)column[1]));
                                    new_stage.connectionsPerLevel.Add(column_list);
                                }
                                foreach (var step in stage.chosenPath)
                                    new_stage.chosenPath.Add((uint)step.Value);
                                

                                foreach (var row in stage.fieldsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.fieldsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.shopsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.shopsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.cardRewardsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.cardRewardsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.upgradeRewardsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.upgradeRewardsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.relicRewardsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.relicRewardsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.coinRewardsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.coinRewardsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.startCrystalRewardsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.startCrystalRewardsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.dropRewardsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.dropRewardsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.restSitesPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.restSitesPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.researchPointsRewardsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.researchPointsRewardsPerLevel.Add(column_list);
                                }
                                foreach (var row in stage.researchRewardsPerLevel){
                                    List<uint> column_list = new();
                                    foreach (var column in row)
                                        column_list.Add((uint)column.Value);
                                    new_stage.researchRewardsPerLevel.Add(column_list);
                                }

                                savegame.stages.Add(new_stage);
                            }


                            break;
                    }
                }
                // then make everything refresh
                NavigateToEngineer(null, null);

            } catch (Exception ex){ NavigateToOutput(ex.ToString());}
        }
        void WriteSaveGameFile(string output_path){
            try{
                string serialized_json = CompileJson();
                if (string.IsNullOrWhiteSpace(serialized_json)) return;

		        File.WriteAllBytes(output_path, EncrypterAES.EncryptStringToBytes_Aes(serialized_json));
            } catch (Exception ex){ NavigateToOutput(ex.ToString()); }
	    }
        void WriteJson(string output_path){
            try{
                string serialized_json = CompileJson();
                if (string.IsNullOrWhiteSpace(serialized_json)) return;

		        File.WriteAllText(output_path, serialized_json);
            } catch (Exception ex){ NavigateToOutput(ex.ToString()); }
	    }
        string CompileJson(){
            try{
                // serialize the json manually
                StringBuilder json = new();
                json.Append("{");

                // generic data fields
                foreach (var item in savegame.datafields)
                    json.Append($"\"{item.Key}\":{item.Value},");

                // engineer
                json.Append($"\"engineer\":\"{savegame.engineer}\",");

                // specialists
                json.Append("\"specialists\":[");
                for (int i = 0; i < savegame.specialists.Count; i++){
                    if (i > 0) json.Append(", ");
                    json.Append($"\"{savegame.specialists[i]}\"");
                } json.Append("],");
            
                // blueprints
                json.Append("\"blueprints\":[");
                for (int i = 0; i < savegame.blueprints.Count; i++){
                    if (i > 0) json.Append(",");
                    json.Append($"\"{savegame.blueprints[i]}\"");
                } json.Append("],");

                // relics
                json.Append("\"relics\":[");
                for (int i = 0; i < savegame.relics.Count; i++){
                    if (i > 0) json.Append(",");
                    json.Append($"\"{savegame.relics[i]}\"");
                } json.Append("],");

                // drops
                json.Append("\"drops\":[");
                for (int i = 0; i < savegame.drops.Count; i++){
                    if (i > 0) json.Append(",");
                    json.Append($"\"{savegame.drops[i]}\"");
                } json.Append("],");
            
                // upgrades
                json.Append("\"cardUpgrades\":[");
                for (int i = 0; i < savegame.upgrades.Count; i++){
                    if (i > 0) json.Append(",");
                    var curr_upgrade = savegame.upgrades[i];
                    json.Append($"[\"{curr_upgrade.Key}\",[");
                
                    for (int j = 0; j < curr_upgrade.Value.Count; j++){
                        if (j > 0) json.Append(",");
                        json.Append($"\"{curr_upgrade.Value[j]}\"");
                    }
                    json.Append("]]");
                } json.Append("],");

                // unused research branches thing
                json.Append("\"researchBranches\":[],");

            
                json.Append("\"stageMap\":");
                json.Append(savegame.stage_map);
                json.Append("}");
                return json.ToString();
            } catch (Exception ex){ NavigateToOutput(ex.ToString()); return "";}
        }
        #endregion

        #region entity database stuff
        Dictionary<string, string> entity_names = new();
        Dictionary<string, string> entity_desc = new();
        Dictionary<string, UnitRole> entity_roles = new();

        Dictionary<string, string> relic_names = new();
        Dictionary<string, string> relic_desc = new();
        
        Dictionary<string, string> upgrade_names = new();
        Dictionary<string, string> upgrade_desc = new();

        List<string> EnemyDeck_names = new();
        List<string> Worlds_names = new();
        List<string> Landscape_names = new();
        Dictionary<string, int> EnemyDeck_to_index = new();
        Dictionary<string, int> Worlds_to_index = new();
        Dictionary<string, int> Landscape_to_index = new();

        void LoadAllIDs(string data_folder){

            // create dictionaries to match up lowercase IDs with their correct versions
            Dictionary<string, string> entity_lower_to_upper = new();
            Dictionary<string, string> relic_lower_to_upper = new();
            Dictionary<string, string> upgrade_lower_to_upper = new();
            foreach (string id in File.ReadAllText(data_folder + "entity_IDs.txt").Split("\t")){
                entity_lower_to_upper.Add(id.ToLower(), id);
                entity_names.Add(id, "");
                entity_desc.Add(id, "");
                entity_roles.Add(id, 0);
            }
            foreach (string id in File.ReadAllText(data_folder + "relic_IDs.txt").Split("\t")){
                relic_lower_to_upper.Add(id.ToLower(), id);
                relic_names.Add(id, "");
                relic_desc.Add(id, "");
            }
            foreach (string id in File.ReadAllText(data_folder + "upgrade_IDs.txt").Split("\t")){
                upgrade_lower_to_upper.Add(id.ToLower(), id);
                upgrade_names.Add(id, "");
                upgrade_desc.Add(id, "");
            }

            // load entity definitions
            var entity_names_txt = File.ReadAllText(data_folder + "entity_name.txt").Split("\t");
            for (int i = 0; i+1 < entity_names_txt.Length; i += 2)
                if (entity_lower_to_upper.TryGetValue(entity_names_txt[i], out string? value))
                    entity_names[value] = entity_names_txt[i + 1];

            var entity_desc_txt = File.ReadAllText(data_folder + "entity_desc.txt").Split("\t");
            for (int i = 0; i+1 < entity_desc_txt.Length; i += 2)
                if (entity_lower_to_upper.TryGetValue(entity_desc_txt[i], out string? value))
                    entity_desc[value] = entity_desc_txt[i + 1];

            var entity_roles_txt = File.ReadAllText(data_folder + "entity_roles.txt").Split("\t");
            for (int i = 0; i+1 < entity_roles_txt.Length; i += 2)
                if (entity_lower_to_upper.TryGetValue(entity_roles_txt[i], out string? value))
                    entity_roles[value] = (UnitRole)int.Parse(entity_roles_txt[i + 1]);

            // load relic definitions
            var relic_names_txt = File.ReadAllText(data_folder + "relic_name.txt").Split("\t");
            for (int i = 0; i+1 < relic_names_txt.Length; i += 2)
                if (relic_lower_to_upper.TryGetValue(relic_names_txt[i], out string? value))
                    relic_names[value] = relic_names_txt[i + 1];
            
            var relic_desc_txt = File.ReadAllText(data_folder + "relic_desc.txt").Split("\t");
            for (int i = 0; i+1 < relic_desc_txt.Length; i += 2)
                if (relic_lower_to_upper.TryGetValue(relic_desc_txt[i], out string? value))
                    relic_desc[value] = relic_desc_txt[i + 1];
            
            // load upgrade definitions
            var upgrade_names_txt = File.ReadAllText(data_folder + "upgrade_name.txt").Split("\t");
            for (int i = 0; i+1 < upgrade_names_txt.Length; i += 2)
                if (upgrade_lower_to_upper.TryGetValue(upgrade_names_txt[i], out string? value))
                    upgrade_names[value] = upgrade_names_txt[i + 1];
            
            var upgrade_desc_txt = File.ReadAllText(data_folder + "upgrade_desc.txt").Split("\t");
            for (int i = 0; i+1 < upgrade_desc_txt.Length; i += 2)
                if (upgrade_lower_to_upper.TryGetValue(upgrade_desc_txt[i], out string? value))
                    upgrade_desc[value] = upgrade_desc_txt[i + 1];

            // load stage map definitions
            foreach (string id in File.ReadAllText(data_folder + "enemy_decks.txt").Split("\t")){
                EnemyDeck_to_index.Add(id, EnemyDeck_names.Count);
                EnemyDeck_names.Add(id);
            }
            foreach (string id in File.ReadAllText(data_folder + "worlds.txt").Split("\t")){
                Worlds_to_index.Add(id, Worlds_names.Count);
                Worlds_names.Add(id);
            }
            foreach (string id in File.ReadAllText(data_folder + "landscapes.txt").Split("\t")){
                Landscape_to_index.Add(id, Landscape_names.Count);
                Landscape_names.Add(id);
            }
            
            bossAiPrefabName.ItemsSource = EnemyDeck_names;
            bossWorldPrefabName.ItemsSource = Worlds_names;
            bossMapScriptableObjectName.ItemsSource = Landscape_names;
        }
        #endregion

        #region metadata interactions
        public void field_edited(string field_name, string new_value)
        {
            savegame.datafields[field_name] = new_value;
        }
        #endregion

        #region entity list interactions
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if (entries_listview.SelectedIndex < 0 || entries_listview.SelectedIndex >= entries_list.Count){
                entry_label.Text = "No item selected.";
                entry_id.Text = "...";
                entry_desc.Text = "...";
                entry_extra.Text = "...";
                UpdateHeader();
                assign_button.IsEnabled = false;
                return;}

            string id = IDs_list[entries_listview.SelectedIndex];
            
            

            if (current_view == view_mode.Engineer 
            ||  current_view == view_mode.Specialists
            ||  current_view == view_mode.Blueprints
            ||  current_view == view_mode.Drops){
                entry_label.Text = entity_names[id];
                entry_id.Text = id;
                entry_desc.Text = entity_desc[id];
                entry_extra.Text = entity_roles[id].ToString(); // this should transcribe the role uint into their respective strings
                assign_button.IsEnabled = true;
            } else if (current_view == view_mode.Relics){
                entry_label.Text = relic_names[id];
                entry_id.Text = id;
                entry_desc.Text = relic_desc[id];
                entry_extra.Text = "...";
                assign_button.IsEnabled = true;
            } else if (current_view == view_mode.Upgrades){
                entry_label.Text = upgrade_names[id];
                entry_id.Text = id;
                entry_desc.Text = upgrade_desc[id];
                entry_extra.Text = "...";
                assign_button.IsEnabled = true;
            } else { // just clear it
                entry_label.Text = "No page open?";
                entry_id.Text = "...";
                entry_desc.Text = "...";
                entry_extra.Text = "...";
                assign_button.IsEnabled = false;
            }
            UpdateHeader();
        }
        void UpdateHeader(){
            if (current_view == view_mode.Engineer)
                select_label.Text = $"Select Engineer ({entries_list.Count}/{entity_names.Count})";
            else if (current_view == view_mode.Specialists)
                select_label.Text = $"Select Specialists ({entries_list.Count}/{entity_names.Count})";
            else if (current_view == view_mode.Blueprints)
                select_label.Text = $"Select Blueprints ({entries_list.Count}/{entity_names.Count})";
            else if (current_view == view_mode.Drops)
                select_label.Text = $"Select Drops ({entries_list.Count}/{entity_names.Count})";
            else if (current_view == view_mode.Relics)
                select_label.Text = $"Select Relics ({entries_list.Count}/{relic_names.Count})";
            else if (current_view == view_mode.Upgrades)
                select_label.Text = $"Select Upgrades ({entries_list.Count}/{upgrade_names.Count})";
            else 
                select_label.Text = "Select [none]";
        }

        private void SearchFilterUpdated(object sender, TextChangedEventArgs e)
        => ReloadEntries();
        private void FilterBoxChecked(object sender, RoutedEventArgs e)
        => ReloadEntries();
        


        enum view_mode{
            None,
            Engineer,
            Specialists,
            Blueprints,
            Drops,
            Relics,
            Upgrades,

            metadata,
            stagemap,
            output
        }
        view_mode current_view = view_mode.None;

        List<string> IDs_list = new();
        List<string> entries_list = new();

        List<string> IDs_list_extra = new();
        List<string> entries_list_extra = new();

        void ReloadEntries(){
            // toggle PCXcard filter visibility if NO_PCX is checked (for both regular panel + extra upgrade side panel)
            if (NoPCX_box.IsChecked == true){
                PCXCard_box.IsEnabled = false;
                if (PCXCard_box.IsChecked == true) PCXCard_box.IsChecked = false;
            } else PCXCard_box.IsEnabled = true;
            if (extra_NoPCX_box.IsChecked == true){
                extra_PCXCard_box.IsEnabled = false;
                if (extra_PCXCard_box.IsChecked == true) extra_PCXCard_box.IsChecked = false;
            } else extra_PCXCard_box.IsEnabled = true;

            if (current_view == view_mode.Engineer 
            ||  current_view == view_mode.Specialists
            ||  current_view == view_mode.Blueprints
            ||  current_view == view_mode.Drops){
                UnitRole forced_filter = UnitRole.None;
                if (current_view == view_mode.Specialists)
                    forced_filter = UnitRole.Building;
                if (current_view == view_mode.Blueprints)
                    forced_filter = UnitRole.Building;
                if (current_view == view_mode.Drops)
                    forced_filter = UnitRole.Drop;

                populate_from(entity_names, true, forced_filter);
                entry_extra.Visibility = Visibility.Visible;
                type_filter.Visibility = Visibility.Visible;
            }
            else if (current_view == view_mode.Relics){
                populate_from(relic_names, false, UnitRole.None);
                entry_extra.Visibility = Visibility.Collapsed;
                type_filter.Visibility = Visibility.Collapsed;
            }
            else if (current_view == view_mode.Upgrades){
                populate_from(upgrade_names, false, UnitRole.None);
                populate_from_extra_panel(entity_names);
                entry_extra.Visibility = Visibility.Collapsed;
                type_filter.Visibility = Visibility.Collapsed;
            }
            else { // just clear it
                IDs_list = new();
                entries_list = new();
                entries_listview.ItemsSource = entries_list;
                entries_listview.SelectedIndex = -1;
                entry_extra.Visibility = Visibility.Collapsed;
                type_filter.Visibility = Visibility.Collapsed;
            }
            ListView_SelectionChanged(null, null); // doesn't really matter if this gets called twice, just gotta make sure it gets called at least once
        }
        void populate_from(Dictionary<string, string> dict, bool role_filter, UnitRole required_filter){
            IDs_list = new();
            entries_list = new();
            UnitRole filter_role = role_filter? ComputeRoleFilters() | required_filter : required_filter;
            foreach (var entry in dict){
                if ((string.IsNullOrWhiteSpace(search_filter.Text) || entry.Value.Contains(search_filter.Text, StringComparison.OrdinalIgnoreCase))
                &&  (!role_filter || ((filter_role & entity_roles[entry.Key]) == filter_role) && (NoPCX_box.IsChecked != true || ((entity_roles[entry.Key] & UnitRole.PCXCard) == UnitRole.None)))){
                    IDs_list.Add(entry.Key);
                    entries_list.Add(entry.Value);
                }
            }
            entries_listview.ItemsSource = entries_list;
            if (entries_list.Count > 0)
                 entries_listview.SelectedIndex = 0;
            else entries_listview.SelectedIndex = -1;
        }
        void populate_from_extra_panel(Dictionary<string, string> dict){
            IDs_list_extra = new();
            entries_list_extra = new();
            UnitRole filter_role = ComputeRoleFiltersExtraPanel();
            foreach (var entry in dict){
                if ((string.IsNullOrWhiteSpace(search_filter_extra.Text) || entry.Value.Contains(search_filter_extra.Text, StringComparison.OrdinalIgnoreCase))
                &&  (filter_role & entity_roles[entry.Key]) == filter_role && (extra_NoPCX_box.IsChecked != true || ((entity_roles[entry.Key] & UnitRole.PCXCard) == UnitRole.None))){
                    IDs_list_extra.Add(entry.Key);
                    entries_list_extra.Add(entry.Value);
                }
            }
            entries_listview_extra.ItemsSource = entries_list_extra;
            if (entries_list.Count > 0)
                 entries_listview_extra.SelectedIndex = 0;
            else entries_listview_extra.SelectedIndex = -1;

            // update header of the extra upgrade page panel
            select_label_extra.Text = $"Select Entity ({entries_list_extra.Count}/{entity_names.Count})";
        }

        UnitRole ComputeRoleFilters() { 
            UnitRole role_filter = UnitRole.None;
            if (Spawn_box.IsChecked == true) role_filter |= UnitRole.Spawn;
            if (Harvester_box.IsChecked == true) role_filter |= UnitRole.Harvester;
            if (FrontalAttacker_box.IsChecked == true) role_filter |= UnitRole.FrontalAttacker;
            if (MassDestruction_box.IsChecked == true) role_filter |= UnitRole.MassDestruction;
            if (Fire_box.IsChecked == true) role_filter |= UnitRole.Fire;
            if (Sniper_box.IsChecked == true) role_filter |= UnitRole.Sniper;
            if (Supporter_box.IsChecked == true) role_filter |= UnitRole.Supporter;
            if (Rock_box.IsChecked == true) role_filter |= UnitRole.Rock;
            if (Robo_box.IsChecked == true) role_filter |= UnitRole.Robo;
            if (Spawner_box.IsChecked == true) role_filter |= UnitRole.Spawner;
            if (PCXCard_box.IsChecked == true) role_filter |= UnitRole.PCXCard;
            if (Building_box.IsChecked == true) role_filter |= UnitRole.Building;
            if (Factory_box.IsChecked == true) role_filter |= UnitRole.Factory;
            if (Turret_box.IsChecked == true) role_filter |= UnitRole.Turret;
            if (Refinery_box.IsChecked == true) role_filter |= UnitRole.Refinery;
            if (Unit_box.IsChecked == true) role_filter |= UnitRole.Unit;
            if (Heal_box.IsChecked == true) role_filter |= UnitRole.Heal;
            if (Specialist_box.IsChecked == true) role_filter |= UnitRole.Specialist;
            if (Skill_box.IsChecked == true) role_filter |= UnitRole.Skill;
            if (Engineer_box.IsChecked == true) role_filter |= UnitRole.Engineer;
            if (Bomb_box.IsChecked == true) role_filter |= UnitRole.Bomb;
            if (Scout_box.IsChecked == true) role_filter |= UnitRole.Scout;
            if (Chest_box.IsChecked == true) role_filter |= UnitRole.Chest;
            if (Tank_box.IsChecked == true) role_filter |= UnitRole.Tank;
            if (Vehicle_box.IsChecked == true) role_filter |= UnitRole.Vehicle;
            if (Mech_box.IsChecked == true) role_filter |= UnitRole.Mech;
            if (Tree_box.IsChecked == true) role_filter |= UnitRole.Tree;
            if (Melee_box.IsChecked == true) role_filter |= UnitRole.Melee;
            if (Research_box.IsChecked == true) role_filter |= UnitRole.Research;
            if (Crystal_box.IsChecked == true) role_filter |= UnitRole.Crystal;
            if (Builder_box.IsChecked == true) role_filter |= UnitRole.Builder;
            if (Drop_box.IsChecked == true) role_filter |= UnitRole.Drop;
            return role_filter;
        }
        UnitRole ComputeRoleFiltersExtraPanel() { 
            UnitRole role_filter = UnitRole.None;
            if (extra_Spawn_box.IsChecked == true) role_filter |= UnitRole.Spawn;
            if (extra_Harvester_box.IsChecked == true) role_filter |= UnitRole.Harvester;
            if (extra_FrontalAttacker_box.IsChecked == true) role_filter |= UnitRole.FrontalAttacker;
            if (extra_MassDestruction_box.IsChecked == true) role_filter |= UnitRole.MassDestruction;
            if (extra_Fire_box.IsChecked == true) role_filter |= UnitRole.Fire;
            if (extra_Sniper_box.IsChecked == true) role_filter |= UnitRole.Sniper;
            if (extra_Supporter_box.IsChecked == true) role_filter |= UnitRole.Supporter;
            if (extra_Rock_box.IsChecked == true) role_filter |= UnitRole.Rock;
            if (extra_Robo_box.IsChecked == true) role_filter |= UnitRole.Robo;
            if (extra_Spawner_box.IsChecked == true) role_filter |= UnitRole.Spawner;
            if (extra_PCXCard_box.IsChecked == true) role_filter |= UnitRole.PCXCard;
            if (extra_Building_box.IsChecked == true) role_filter |= UnitRole.Building;
            if (extra_Factory_box.IsChecked == true) role_filter |= UnitRole.Factory;
            if (extra_Turret_box.IsChecked == true) role_filter |= UnitRole.Turret;
            if (extra_Refinery_box.IsChecked == true) role_filter |= UnitRole.Refinery;
            if (extra_Unit_box.IsChecked == true) role_filter |= UnitRole.Unit;
            if (extra_Heal_box.IsChecked == true) role_filter |= UnitRole.Heal;
            if (extra_Specialist_box.IsChecked == true) role_filter |= UnitRole.Specialist;
            if (extra_Skill_box.IsChecked == true) role_filter |= UnitRole.Skill;
            if (extra_Engineer_box.IsChecked == true) role_filter |= UnitRole.Engineer;
            if (extra_Bomb_box.IsChecked == true) role_filter |= UnitRole.Bomb;
            if (extra_Scout_box.IsChecked == true) role_filter |= UnitRole.Scout;
            if (extra_Chest_box.IsChecked == true) role_filter |= UnitRole.Chest;
            if (extra_Tank_box.IsChecked == true) role_filter |= UnitRole.Tank;
            if (extra_Vehicle_box.IsChecked == true) role_filter |= UnitRole.Vehicle;
            if (extra_Mech_box.IsChecked == true) role_filter |= UnitRole.Mech;
            if (extra_Tree_box.IsChecked == true) role_filter |= UnitRole.Tree;
            if (extra_Melee_box.IsChecked == true) role_filter |= UnitRole.Melee;
            if (extra_Research_box.IsChecked == true) role_filter |= UnitRole.Research;
            if (extra_Crystal_box.IsChecked == true) role_filter |= UnitRole.Crystal;
            if (extra_Builder_box.IsChecked == true) role_filter |= UnitRole.Builder;
            if (extra_Drop_box.IsChecked == true) role_filter |= UnitRole.Drop;
            return role_filter;
        }

        void ReloadAssigned() {
            assigned_entries_list = new();
            assigned_upgrades_index_map = new();
            remove_button.IsEnabled = true;

            if (current_view == view_mode.Engineer){
                remove_button.IsEnabled = false;
                assigned_entries_list.Add(entity_names[savegame.engineer]);

            } else if (current_view == view_mode.Specialists){
                foreach (string id in savegame.specialists)
                    assigned_entries_list.Add(entity_names[id]);

            } else if (current_view == view_mode.Blueprints){
                foreach (string id in savegame.blueprints)
                    assigned_entries_list.Add(entity_names[id]);

            } else if (current_view == view_mode.Drops){
                foreach (string id in savegame.drops)
                    assigned_entries_list.Add(entity_names[id]);

            } else if (current_view == view_mode.Relics){
                foreach (string id in savegame.relics)
                    assigned_entries_list.Add(relic_names[id]);

            } else if (current_view == view_mode.Upgrades){
                for (int i = 0; i < savegame.upgrades.Count; i++){
                    var entry = savegame.upgrades[i];
                    for (int j = 0; j < entry.Value.Count; j++){
                        assigned_entries_list.Add(entity_names[entry.Key] + " -> " + upgrade_names[entry.Value[j]]);
                        assigned_upgrades_index_map.Add(new(i,j));
                    }
                }
            }

            assigned_listview.ItemsSource = assigned_entries_list;
            if (assigned_entries_list.Count <= 0){
                remove_button.IsEnabled = false;
                assigned_listview.SelectedIndex = -1;
            } else assigned_listview.SelectedIndex = 0;
        }
        List<string> assigned_entries_list = new();
        List<KeyValuePair<int, int>> assigned_upgrades_index_map = new();
        
        private void AssignSelected(object sender, RoutedEventArgs e){
            if (entries_listview.SelectedIndex < 0 || entries_listview.SelectedIndex >= IDs_list.Count)
                return;

            if (current_view == view_mode.Engineer)
                savegame.engineer = IDs_list[entries_listview.SelectedIndex];
            else if (current_view == view_mode.Specialists)
                savegame.specialists.Add(IDs_list[entries_listview.SelectedIndex]);
            else if (current_view == view_mode.Blueprints)
                savegame.blueprints.Add(IDs_list[entries_listview.SelectedIndex]);
            else if (current_view == view_mode.Drops)
                savegame.drops.Add(IDs_list[entries_listview.SelectedIndex]);
            else if (current_view == view_mode.Relics)
                savegame.relics.Add(IDs_list[entries_listview.SelectedIndex]);
            else if (current_view == view_mode.Upgrades){
                // we need to check if an entity is selected in the extra panel
                if (entries_listview_extra.SelectedIndex < 0 || entries_listview_extra.SelectedIndex >= IDs_list_extra.Count)
                    return;

                var entity_id = IDs_list_extra[entries_listview_extra.SelectedIndex];
                var upgrade_id = IDs_list[entries_listview.SelectedIndex];

                // check if the target entity already exists in our savegame list, append to that one if so
                if (savegame.upgrades.Any(x => x.Key == entity_id))
                    savegame.upgrades.First(x => x.Key == entity_id).Value.Add(upgrade_id);
                else // create a new entry to append the upgrade to
                    savegame.upgrades.Add(new(entity_id, new List<string> { upgrade_id }));
            }

            ReloadAssigned();
        }
        private void RemoveSelected(object sender, RoutedEventArgs e){
            if (assigned_listview.SelectedIndex < 0 || assigned_listview.SelectedIndex >= assigned_entries_list.Count)
                return;

            if (current_view == view_mode.Specialists)
                savegame.specialists.RemoveAt(assigned_listview.SelectedIndex);
            else if (current_view == view_mode.Blueprints)
                savegame.blueprints.RemoveAt(assigned_listview.SelectedIndex);
            else if (current_view == view_mode.Drops)
                savegame.drops.RemoveAt(assigned_listview.SelectedIndex);
            else if (current_view == view_mode.Relics)
                savegame.relics.RemoveAt(assigned_listview.SelectedIndex);
            else if (current_view == view_mode.Upgrades){
                var index = assigned_upgrades_index_map[assigned_listview.SelectedIndex];
                savegame.upgrades[index.Key].Value.RemoveAt(index.Value);
                if (savegame.upgrades[index.Key].Value.Count <= 0)
                    savegame.upgrades.RemoveAt(index.Key);
            } else return;

            ReloadAssigned();
        }
        #endregion

        #region stagemap interactions
        bool is_loading_stage_infos = false;
        StageMap? current_stagemap = null; // must null out if we close the current one?
        void ReloadStagemaps(StageMap target_stage){
            current_stagemap = target_stage;

            is_loading_stage_infos = true;
            ResetMapStage_ValidityStates();

            chosenField.Text = current_stagemap.chosenField.ToString();
            levelCount.Text = current_stagemap.levelCount.ToString();
            maxWidth.Text = current_stagemap.maxWidth.ToString();
            coinReward.Text = current_stagemap.coinReward.ToString();
            startCrystalReward.Text = current_stagemap.startCrystalReward.ToString();
            researchPointsReward.Text = current_stagemap.researchPointsReward.ToString();
            currentLevel.Text = current_stagemap.currentLevel.ToString();
            currentField.Text = current_stagemap.currentField.ToString();
            isCurrentLevelFinished.IsChecked = current_stagemap.isCurrentLevelFinished;

            cardRewardParameters_rarity.Text = current_stagemap.cardRewardParameters.rarity.ToString();
            cardRewardParameters_rareProbability.Text = current_stagemap.cardRewardParameters.rareProbability.ToString();
            cardRewardParameters_ultraRareProbability.Text = current_stagemap.cardRewardParameters.ultraRareProbability.ToString();
            upgradeRewardParameters_rarity.Text = current_stagemap.upgradeRewardParameters.rarity.ToString();
            upgradeRewardParameters_rareProbability.Text = current_stagemap.upgradeRewardParameters.rareProbability.ToString();
            upgradeRewardParameters_ultraRareProbability.Text = current_stagemap.upgradeRewardParameters.ultraRareProbability.ToString();
            relicRewardParameters_rarity.Text = current_stagemap.relicRewardParameters.rarity.ToString();
            relicRewardParameters_rareProbability.Text = current_stagemap.relicRewardParameters.rareProbability.ToString();
            relicRewardParameters_ultraRareProbability.Text = current_stagemap.relicRewardParameters.ultraRareProbability.ToString();
            researchRewardParameters_rarity.Text = current_stagemap.researchRewardParameters.rarity.ToString();
            researchRewardParameters_rareProbability.Text = current_stagemap.researchRewardParameters.rareProbability.ToString();
            researchRewardParameters_ultraRareProbability.Text = current_stagemap.researchRewardParameters.ultraRareProbability.ToString();

            // we have to do something a little special here to convert the strings to selected indexes
            // so we setup an index lookup dictionary
            try {
                bossAiPrefabName.SelectedIndex = EnemyDeck_to_index[current_stagemap.bossAiPrefabName];
                bossWorldPrefabName.SelectedIndex = Worlds_to_index[current_stagemap.bossWorldPrefabName];
                bossMapScriptableObjectName.SelectedIndex = Landscape_to_index[current_stagemap.bossMapScriptableObjectName];
            } catch (Exception ex){ NavigateToOutput(ex.ToString()); is_loading_stage_infos = false; return;}
            
            // also we have to setup our nice reward map visuals
            LoadNewRewardMap();
            is_loading_stage_infos = false;
        }
        void ReloadStageList(){
            int new_index = 0;
            List<string> new_list = new List<string>();
            for (int i = 0; i < savegame.stages.Count; i++){
                new_list.Add($"[{i}] {savegame.stages[i].bossMapScriptableObjectName}");
                if (savegame.stages[i] == current_stagemap)
                    new_index = i;
            }
            
            if (savegame.stages.Count <= 0){
                savegame.stages.Add(new StageMap());
                NavigateToOutput("your savegame had ZERO stages, i've created a new one and inserted it, but bewarned the values may be misconfigured! (also this shouldn't be possible)");
                return;
            }
            // show/hide the remove button depending on how many entries we have
            if (savegame.stages.Count == 1)
                 remove_stage_button.IsEnabled = false;
            else remove_stage_button.IsEnabled = true;


            is_loading_stage_infos = true;
            assigned_stageview.ItemsSource = new_list;
            assigned_stageview.SelectedIndex = new_index;
            is_loading_stage_infos = false;
            
            RedrawArrowButtons();

            // also update the active stage index (doing it here because it doesn't need to refresh with every selected stage change)
            select_stage_box.Text = $"Select Stage (Active: {savegame.currentStage})";

            if (current_stagemap == null)
                ReloadStagemaps(savegame.stages[new_index]); // this will always be 0
        }
        private void assigned_stageview_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if (is_loading_stage_infos) return;

            if (assigned_stageview.SelectedIndex < 0 || assigned_stageview.SelectedIndex >= savegame.stages.Count){
                NavigateToOutput("failed selected stage index!! impossible error");
                return;}

            ReloadStagemaps(savegame.stages[assigned_stageview.SelectedIndex]);
            RedrawArrowButtons();
        }
        void RedrawArrowButtons(){
            // show/hide the up/down buttons depending on the selected index
            if (assigned_stageview.SelectedIndex == 0)
                 up_stage_button.IsEnabled = false;
            else up_stage_button.IsEnabled = true;
            if (assigned_stageview.SelectedIndex == savegame.stages.Count-1)
                 down_stage_button.IsEnabled = false;
            else down_stage_button.IsEnabled = true;
        }
        private void CopyStage_click(object sender, RoutedEventArgs e){
            if (assigned_stageview.SelectedIndex < 0 || assigned_stageview.SelectedIndex >= savegame.stages.Count){
                NavigateToOutput("bad selected stage index!! impossible error");
                return;}

            Savegame_add_at(new StageMap(savegame.stages[assigned_stageview.SelectedIndex]), assigned_stageview.SelectedIndex+1);
            ReloadStageList();
        }
        private void RemoveStage_click(object sender, RoutedEventArgs e){
            if (assigned_stageview.SelectedIndex < 0 || assigned_stageview.SelectedIndex >= savegame.stages.Count){
                NavigateToOutput("bad selected stage index!! impossible error");
                return;}
            if (savegame.stages.Count <= 1) return; // do not remove the last one !!!

            savegame.stages.RemoveAt(assigned_stageview.SelectedIndex);
            current_stagemap = null;
            ReloadStageList();
        }
        private void SetCurrentStage_click(object sender, RoutedEventArgs e){
            savegame.currentStage = assigned_stageview.SelectedIndex;
            select_stage_box.Text = $"Select Stage (Active: {savegame.currentStage})";
        }

        private void UpStage_click(object sender, RoutedEventArgs e){ // decrements index
            if (assigned_stageview.SelectedIndex < 0 || assigned_stageview.SelectedIndex >= savegame.stages.Count){
                NavigateToOutput("bad selected stage index!! impossible error");
                return;}

            if (assigned_stageview.SelectedIndex == 0) return;

            StageMap test = savegame.stages[assigned_stageview.SelectedIndex];
            savegame.stages.RemoveAt(assigned_stageview.SelectedIndex);
            Savegame_add_at(test, assigned_stageview.SelectedIndex-1);
            ReloadStageList();
        }
        private void DownStage_click(object sender, RoutedEventArgs e){ // decrements index
            if (assigned_stageview.SelectedIndex < 0 || assigned_stageview.SelectedIndex >= savegame.stages.Count){
                NavigateToOutput("bad selected stage index!! impossible error");
                return;}

            if (assigned_stageview.SelectedIndex >= savegame.stages.Count-1) return;
            
            StageMap test = savegame.stages[assigned_stageview.SelectedIndex];
            savegame.stages.RemoveAt(assigned_stageview.SelectedIndex);
            Savegame_add_at(test, assigned_stageview.SelectedIndex+1);
            ReloadStageList();
        }
        void Savegame_add_at(StageMap map, int index){
            if (index >= savegame.stages.Count)
                 savegame.stages.Add(map);
            else savegame.stages.Insert(index, map);
        }

        #region reward map visuals
        Dictionary<string, Border> reward_tile_map = new();

        int curr_reward_rows = 0;
        int curr_reward_cols = 0;
        enum reward_type{
            None,
            Coin,
            StartCrystal,
            ResearchPoints,
            Card,
            Upgrade,
            Relic,
            Research
        }
        void LoadNewRewardMap(){
            if (current_stagemap == null) return;
            
            // check to see if our grid thingo needs to be recalculated
            if (current_stagemap.levelCount != curr_reward_rows || current_stagemap.maxWidth != curr_reward_cols){
                curr_reward_rows = current_stagemap.levelCount;
                curr_reward_cols = current_stagemap.maxWidth;
                RecreateRewardGrid();
            }
            // then repaint
            RedrawRewardGrid();
        }
        void ResizeRewardmapData(int rows, int columns){
            if (current_stagemap == null) return;
            // if no change, skip
            if (rows == curr_reward_rows && columns == curr_reward_cols)
                return;

            // if the columns increased, then we dont need to do anything
            // if columns decreased, then we need to search through all the arrays and remove those indicies

            // if rows decreased, remove last list in each thingo
            // if rows increased, add new list to each
            
            // regenerate the reward tile map if the size has changed
            curr_reward_rows = rows;
            curr_reward_cols = columns;
            RecreateRewardGrid();
            RedrawRewardGrid();
            

        }
        void RedrawRewardGrid(){
            if (current_stagemap == null) return;
            // update the visuals of each reward tile
            foreach (var v in reward_tile_map)
                v.Value.Background = Brushes.White;

            // shops
            for (int row = 0; row < current_stagemap.shopsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.shopsPerLevel[row].Count; j++){
                    uint col = current_stagemap.shopsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.Red;
                }
            }
            // cards
            for (int row = 0; row < current_stagemap.cardRewardsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.cardRewardsPerLevel[row].Count; j++){
                    uint col = current_stagemap.cardRewardsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.Purple;
                }
            }
            // upgrades
            for (int row = 0; row < current_stagemap.upgradeRewardsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.upgradeRewardsPerLevel[row].Count; j++){
                    uint col = current_stagemap.upgradeRewardsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.DarkBlue;
                }
            }
            // relics
            for (int row = 0; row < current_stagemap.relicRewardsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.relicRewardsPerLevel[row].Count; j++){
                    uint col = current_stagemap.relicRewardsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.Green;
                }
            }
            // coins
            for (int row = 0; row < current_stagemap.coinRewardsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.coinRewardsPerLevel[row].Count; j++){
                    uint col = current_stagemap.coinRewardsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.Orange;
                }
            }
            // start crystals
            for (int row = 0; row < current_stagemap.startCrystalRewardsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.startCrystalRewardsPerLevel[row].Count; j++){
                    uint col = current_stagemap.startCrystalRewardsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.LightBlue;
                }
            }
            // drops
            for (int row = 0; row < current_stagemap.dropRewardsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.dropRewardsPerLevel[row].Count; j++){
                    uint col = current_stagemap.dropRewardsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.LightGreen;
                }
            }
            // lives
            for (int row = 0; row < current_stagemap.restSitesPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.restSitesPerLevel[row].Count; j++){
                    uint col = current_stagemap.restSitesPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.HotPink;
                }
            }
            // research points
            for (int row = 0; row < current_stagemap.researchPointsRewardsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.researchPointsRewardsPerLevel[row].Count; j++){
                    uint col = current_stagemap.researchPointsRewardsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.Yellow;
                }
            }
            // research rewards
            for (int row = 0; row < current_stagemap.researchRewardsPerLevel.Count; row++){
                for (int j = 0; j < current_stagemap.researchRewardsPerLevel[row].Count; j++){
                    uint col = current_stagemap.researchRewardsPerLevel[row][j];
                    reward_tile_map[$"{row}_{col}"].Background = Brushes.Gray;
                }
            }
        }
        private void RecreateRewardGrid(){
            if (current_stagemap == null) return;
            reward_tile_map.Clear();
            reward_grid.RowDefinitions.Clear();
            reward_grid.ColumnDefinitions.Clear();
            reward_grid.Children.Clear();
            for (int i = 0; i < curr_reward_rows; i++)
                reward_grid.RowDefinitions.Add(new RowDefinition());
            for (int i = 0; i < curr_reward_cols; i++)
                reward_grid.ColumnDefinitions.Add(new ColumnDefinition());
            

            // Add Border elements to each cell for click event demonstration
            for (int row = 0; row < curr_reward_rows; row++){
                for (int col = 0; col < curr_reward_cols; col++){
                    Border border = new Border{
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1)
                    };
                    border.MouseDown += RewardTile_MouseDown;
                    border.MouseEnter += RewardTile_MouseEnter;
                    border.MouseLeave += RewardTile_MouseLeave;

                    string border_id = $"{curr_reward_rows-row-1}_{col}";
                    reward_tile_map.Add(border_id, border);
                    border.Tag = border_id;

                    Grid.SetRow(border, row);
                    Grid.SetColumn(border, col);
                    reward_grid.Children.Add(border);
                }
            }
        }


        private void RewardTile_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void RewardTile_MouseEnter(object sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
        }
        private void RewardTile_MouseLeave(object sender, MouseEventArgs e)
        {
            //throw new NotImplementedException();
        }

        #endregion

        #region static field interactions
        private void chosenField_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(chosenField, ref current_stagemap.chosenField);}
        private void currentLevel_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(currentLevel, ref current_stagemap.currentLevel);}
        private void currentField_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(currentField, ref current_stagemap.currentField);}
        private void levelCount_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(levelCount, ref current_stagemap.levelCount);}
        private void maxWidth_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(maxWidth, ref current_stagemap.maxWidth);}
        private void coinReward_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(coinReward, ref current_stagemap.coinReward);}
        private void startCrystalReward_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(startCrystalReward, ref current_stagemap.startCrystalReward);}
        private void researchPointsReward_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(researchPointsReward, ref current_stagemap.researchPointsReward);}
        private void isCurrentLevelFinished_Checked(object sender, RoutedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null || isCurrentLevelFinished.IsChecked == null) return;
            current_stagemap.isCurrentLevelFinished = (bool)isCurrentLevelFinished.IsChecked;
        }
        #endregion
        
        #region static struct field interactions
        private void cardRewardParameters_rarity_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(cardRewardParameters_rarity, ref current_stagemap.cardRewardParameters.rarity);}
        private void cardRewardParameters_rareProbability_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_float_change(cardRewardParameters_rareProbability, ref current_stagemap.cardRewardParameters.rareProbability);}
        private void cardRewardParameters_ultraRareProbability_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_float_change(cardRewardParameters_ultraRareProbability, ref current_stagemap.cardRewardParameters.ultraRareProbability);}

        private void upgradeRewardParameters_rarity_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(upgradeRewardParameters_rarity, ref current_stagemap.upgradeRewardParameters.rarity);}
        private void upgradeRewardParameters_rareProbability_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_float_change(upgradeRewardParameters_rareProbability, ref current_stagemap.upgradeRewardParameters.rareProbability);}
        private void upgradeRewardParameters_ultraRareProbability_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_float_change(upgradeRewardParameters_ultraRareProbability, ref current_stagemap.upgradeRewardParameters.ultraRareProbability);}

        private void relicRewardParameters_rarity_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(relicRewardParameters_rarity, ref current_stagemap.relicRewardParameters.rarity);}
        private void relicRewardParameters_rareProbability_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_float_change(relicRewardParameters_rareProbability, ref current_stagemap.relicRewardParameters.rareProbability);}
        private void relicRewardParameters_ultraRareProbability_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_float_change(relicRewardParameters_ultraRareProbability, ref current_stagemap.relicRewardParameters.ultraRareProbability);}
        
        private void researchRewardParameters_rarity_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_int_change(researchRewardParameters_rarity, ref current_stagemap.researchRewardParameters.rarity);}
        private void researchRewardParameters_rareProbability_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_float_change(researchRewardParameters_rareProbability, ref current_stagemap.researchRewardParameters.rareProbability);}
        private void researchRewardParameters_ultraRareProbability_TextChanged(object sender, TextChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_float_change(researchRewardParameters_ultraRareProbability, ref current_stagemap.researchRewardParameters.ultraRareProbability);}
        #endregion
        void ResetMapStage_ValidityStates(){
            chosenField.Foreground = Brushes.Black;
            currentLevel.Foreground = Brushes.Black;
            currentField.Foreground = Brushes.Black;
            levelCount.Foreground = Brushes.Black;
            maxWidth.Foreground = Brushes.Black;
            coinReward.Foreground = Brushes.Black;
            startCrystalReward.Foreground = Brushes.Black;
            researchPointsReward.Foreground = Brushes.Black;
            isCurrentLevelFinished.Foreground = Brushes.Black;
            cardRewardParameters_rarity.Foreground = Brushes.Black;
            cardRewardParameters_rareProbability.Foreground = Brushes.Black;
            cardRewardParameters_ultraRareProbability.Foreground = Brushes.Black;
            upgradeRewardParameters_rarity.Foreground = Brushes.Black;
            upgradeRewardParameters_rareProbability.Foreground = Brushes.Black;
            upgradeRewardParameters_ultraRareProbability.Foreground = Brushes.Black;
            relicRewardParameters_rarity.Foreground = Brushes.Black;
            relicRewardParameters_rareProbability.Foreground = Brushes.Black;
            relicRewardParameters_ultraRareProbability.Foreground = Brushes.Black;
            researchRewardParameters_rarity.Foreground = Brushes.Black;
            researchRewardParameters_rareProbability.Foreground = Brushes.Black;
            researchRewardParameters_ultraRareProbability.Foreground = Brushes.Black;
        }
        #region prefab dropdwon interactions
        private void bossAiPrefabName_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_dropdown_change(bossAiPrefabName, ref current_stagemap.bossAiPrefabName, EnemyDeck_names);}
        private void bossWorldPrefabName_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_dropdown_change(bossWorldPrefabName, ref current_stagemap.bossWorldPrefabName, Worlds_names );}
        private void bossMapScriptableObjectName_SelectionChanged(object sender, SelectionChangedEventArgs e){
            if (is_loading_stage_infos || current_stagemap == null) return;
            handle_dropdown_change(bossMapScriptableObjectName, ref current_stagemap.bossMapScriptableObjectName, Landscape_names);
            ReloadStageList();} // we actually have to reload stage list here, since it updates the name of the stage in the listview
        #endregion
        
        void handle_int_change(TextBox field, ref int write_to_value){
            if (int.TryParse(field.Text, out int result)){
                write_to_value = result;
                field.Foreground = Brushes.Black;
            } else field.Foreground = Brushes.Red;
        }
        void handle_float_change(TextBox field, ref float write_to_value){
            if (float.TryParse(field.Text, out float result)){
                write_to_value = result;
                field.Foreground = Brushes.Black;
            } else field.Foreground = Brushes.Red;
        }
        void handle_dropdown_change(ComboBox field, ref string write_to_value, List<string> item_source){
            if (field.SelectedIndex >= 0 && field.SelectedIndex < item_source.Count)
                write_to_value = item_source[field.SelectedIndex];
            else { // reset??
                is_loading_stage_infos = true;
                field.SelectedIndex = 0;
                is_loading_stage_infos = false;
            }
        }
        #endregion

        #region UI pages navigation
        GridLength upgrade_panel_stored_size = new GridLength(1, GridUnitType.Star);
        void SwapView(view_mode new_view){
            if (new_view == current_view) return;

            // process this at the top, so if we run into an error without anything open, then we skip accidently openning up all our UI
            if (new_view == view_mode.output){
                console_page.Visibility = Visibility.Visible;
                metadata_page.Visibility = Visibility.Collapsed;
                id_list_page.Visibility = Visibility.Collapsed;
                stagemap_list_page.Visibility = Visibility.Collapsed;
                
                if (current_view == view_mode.None)
                    return;
            }

            // reset visibility of savegame exporting buttons
            save_packed_button.IsEnabled = true;
            save_json_button.IsEnabled = true;   

            // reset visibilty of all buttons
            engineer_page_button.IsEnabled = true;
            specialists_page_button.IsEnabled = true;
            blueprints_page_button.IsEnabled = true;
            drops_page_button.IsEnabled = true;
            relics_page_button.IsEnabled = true;
            upgrades_page_button.IsEnabled = true;
            metadata_page_button.IsEnabled = true;
            stagemap_page_button.IsEnabled = true;
            
            // reset vis of filter boxes
            Drop_box.Visibility = Visibility.Visible;
            Building_box.Visibility = Visibility.Visible;

            // disable filter of any forced filter pages
            if (new_view == view_mode.Specialists
            ||  new_view == view_mode.Blueprints)
                Building_box.Visibility = Visibility.Collapsed;
            else if (new_view == view_mode.Drops)
                Drop_box.Visibility = Visibility.Collapsed;

            // enable savegame direct profile interfacing buttons
            if (!string.IsNullOrWhiteSpace(game_folder))
                profile_save.IsEnabled = true;


            // disable button of page we just navigated to
            if (new_view == view_mode.Engineer)
                engineer_page_button.IsEnabled = false;
            else if (new_view == view_mode.Specialists)
                specialists_page_button.IsEnabled = false;
            else if (new_view == view_mode.Blueprints)
                blueprints_page_button.IsEnabled = false;
            else if (new_view == view_mode.Drops)
                drops_page_button.IsEnabled = false;
            else if (new_view == view_mode.Relics)
                relics_page_button.IsEnabled = false;
            else if (new_view == view_mode.Upgrades)
                upgrades_page_button.IsEnabled = false;
            else if (new_view == view_mode.metadata)
                metadata_page_button.IsEnabled = false;
            else if (new_view == view_mode.stagemap)
                stagemap_page_button.IsEnabled = false;

            if (current_view == view_mode.Upgrades) {
                upgrade_panel_stored_size = id_list_page.ColumnDefinitions[0].Width;
                id_list_page.ColumnDefinitions[0].Width = new GridLength(0);
                extra_id_list.Visibility = Visibility.Collapsed; 
            }

            // now handle all the controls that need to be adjusted for page load
            if (new_view == view_mode.Engineer
            ||  new_view == view_mode.Specialists
            ||  new_view == view_mode.Blueprints
            ||  new_view == view_mode.Drops
            ||  new_view == view_mode.Relics
            ||  new_view == view_mode.Upgrades){
                console_page.Visibility = Visibility.Collapsed;
                metadata_page.Visibility = Visibility.Collapsed;
                id_list_page.Visibility = Visibility.Visible;
                stagemap_list_page.Visibility = Visibility.Collapsed;

                // hide the extra id list if we're not on upgrades
                
                if (new_view == view_mode.Upgrades){
                    id_list_page.ColumnDefinitions[0].Width = upgrade_panel_stored_size;
                    extra_id_list.Visibility = Visibility.Visible;

                } 

                current_view = new_view;
                ReloadEntries();
                ReloadAssigned();
                return;
            }

            if (new_view == view_mode.metadata){
                console_page.Visibility = Visibility.Collapsed;
                metadata_page.Visibility = Visibility.Visible;
                id_list_page.Visibility = Visibility.Collapsed;
                stagemap_list_page.Visibility = Visibility.Collapsed;
                
                current_view = new_view;
                return;
            }

            if (new_view == view_mode.stagemap){
                console_page.Visibility = Visibility.Collapsed;
                metadata_page.Visibility = Visibility.Collapsed;
                id_list_page.Visibility = Visibility.Collapsed;
                stagemap_list_page.Visibility = Visibility.Visible;
                
                current_view = new_view;
                ReloadStageList();
                return;
            }

            if (new_view == view_mode.output){
                current_view = new_view;
                return;
            }
        }
        private void NavigateToEngineer(object sender, RoutedEventArgs e)    => SwapView(view_mode.Engineer);
        private void NavigateToSpecialists(object sender, RoutedEventArgs e) => SwapView(view_mode.Specialists);
        private void NavigateToBlueprints(object sender, RoutedEventArgs e)  => SwapView(view_mode.Blueprints);
        private void NavigateToDrops(object sender, RoutedEventArgs e)       => SwapView(view_mode.Drops);
        private void NavigateToRelics(object sender, RoutedEventArgs e)      => SwapView(view_mode.Relics);
        private void NavigateToUpgrades(object sender, RoutedEventArgs e)    => SwapView(view_mode.Upgrades);
        private void NavigateToMetadata(object sender, RoutedEventArgs e)    => SwapView(view_mode.metadata);
        private void NavigateToStagemap(object sender, RoutedEventArgs e)    => SwapView(view_mode.stagemap);
        private void NavigateToOutput(string output){
            SwapView(view_mode.output);
            console_feed.Text = output;
        }
        #endregion

        #region RC profile interactions
        // steam interaction stuff //
        string? game_folder = null;
        void GetGamePath(object sender, RoutedEventArgs e){
            try{
                // try two different registry paths to find the steam folder??
                string? steam_folder = (string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null);
                if (string.IsNullOrWhiteSpace(steam_folder))
                    steam_folder = (string?)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", null);
                if (string.IsNullOrWhiteSpace(steam_folder))
                    throw new Exception("Steam path not found in registry!");


                string library_folders_path = steam_folder + "\\steamapps\\libraryfolders.vdf";
                // chat gpt gave me this regex, so no idea what it does
                if (!File.Exists(library_folders_path))
                    throw new Exception("libraryfolders.vdf missing from steam path!");

            
                var libraryFolders = new List<string>(); 
                var fileContent = File.ReadAllText(library_folders_path); 

                for (int index = 0;;) {
                    index = fileContent.IndexOf("\"path\"", index);
                    if (index == -1) break;

                    // iterate over all white spaces to find start of path string
                    index += 6;
                    while (fileContent[index++] != '\"');

                    // iterate over all characters and append to path var until we reach the delimiting quote character
                    string path = "";
                    while (fileContent[index] != '\"')
                        path += fileContent[index++];

                    libraryFolders.Add(path);
                }

                // search all the directories for the game folder: "Rogue Command"
                string game_folder_path = "";
                foreach (string library in libraryFolders){
                    string game_path = library + "\\steamapps\\common\\Rogue Command";
                    if (Directory.Exists(game_path)){
                        game_folder_path = game_path;
                        break;
                }}
                if (string.IsNullOrWhiteSpace(game_folder_path))
                    throw new Exception($"Rogue command folder was not found in any of {libraryFolders.Count} steam libraries!");
                VerifyGameFolder(game_folder_path);
            } catch (Exception ex){ NavigateToOutput(ex.ToString()); }
        }

        void SetGamePath(object sender, RoutedEventArgs e){
            try{
                var folderDialog = new OpenFolderDialog();
                folderDialog.Title = "Select \"Rogue Command\" folder";
                if (folderDialog.ShowDialog() == true)
                    VerifyGameFolder(folderDialog.FolderName);
            } catch (Exception ex) { NavigateToOutput(ex.ToString()); }
        }
        // folder dependent buttons
        void BrowseGamePath(object sender, RoutedEventArgs e){
            if (!string.IsNullOrWhiteSpace(game_folder))
                System.Diagnostics.Process.Start("explorer.exe", game_folder);
        }
        void LoadProfile1(object sender, RoutedEventArgs e) => TryLoadProfile(game_folder + "\\Profiles\\Profile1\\savegame.dat");
        void LoadProfile2(object sender, RoutedEventArgs e) => TryLoadProfile(game_folder + "\\Profiles\\Profile2\\savegame.dat");
        void LoadProfile3(object sender, RoutedEventArgs e) => TryLoadProfile(game_folder + "\\Profiles\\Profile3\\savegame.dat");
        void TryLoadProfile(string path){
            try{LoadEncrypted_Path(path);
            } catch (Exception ex){ NavigateToOutput(ex.ToString()); }
        }

        void SaveProfile1(object sender, RoutedEventArgs e) => WriteSaveGameFile(game_folder + "\\Profiles\\Profile1\\savegame.dat");
        void SaveProfile2(object sender, RoutedEventArgs e) => WriteSaveGameFile(game_folder + "\\Profiles\\Profile2\\savegame.dat");
        void SaveProfile3(object sender, RoutedEventArgs e) => WriteSaveGameFile(game_folder + "\\Profiles\\Profile3\\savegame.dat");
        


        void VerifyGameFolder(string folder){
            if (!Directory.Exists(folder + "\\Profiles"))
                throw new Exception("folder does not contain profiles folder!");

            if (!Directory.Exists(folder + "\\Profiles\\Profile1"))
                throw new Exception("folder is missing profile1");
            if (!Directory.Exists(folder + "\\Profiles\\Profile2"))
                throw new Exception("folder is missing profile2");
            if (!Directory.Exists(folder + "\\Profiles\\Profile3"))
                throw new Exception("folder is missing profile3");

            SaveGamePath(folder);
        }

        void SaveGamePath(string new_path){
            try{
                File.WriteAllText("game_path.txt", new_path);
                LoadedGamePath(new_path);
            } catch (Exception ex){ NavigateToOutput(ex.ToString()); }
        }
        void LoadedGamePath(string path){
            game_folder = path;
            profile_load.IsEnabled = true;
            profiles_browse.IsEnabled = true;
            // and also enable the save button if we have a savegame loaded
            if (current_view != view_mode.None)
                profile_save.IsEnabled = true;
        }
        void AutoFetchGamePath_If_FirstTime(){
            // if our game path file exists, then just check the file to see if we stored the game path
            if (File.Exists("game_path.txt")){
                string possible_game_path = File.ReadAllText("game_path.txt");
                if (!string.IsNullOrWhiteSpace(possible_game_path) && Directory.Exists(possible_game_path))
                    LoadedGamePath(possible_game_path);
                return;
            }

            // else we need to run our first time search for the game
            GetGamePath(null, null);
            // if that failed, then just put the file in but make it blank, so that we never run this logic again
            if (!File.Exists("game_path.txt"))
                File.WriteAllText("game_path.txt", "");
        }

        #endregion

    }
}
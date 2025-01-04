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
            LoadAllIDs("C:\\Users\\Joe bingle\\Downloads\\RC modding\\exports\\data\\");

            LoadEncrypted_Path("D:\\Programs\\Steam\\steamapps\\common\\Rogue Command\\Profiles\\Profile3\\savegame.dat");

            ListView_SelectionChanged(null, null); // clear our epic sample text
        }

        void WriteSaveGameFile()
	    {
		    //byte[] bytes = EncrypterAES.EncryptStringToBytes_Aes();
		    //File.WriteAllBytes("D:\\Programs\\Steam\\steamapps\\common\\Rogue Command\\Profiles\\Profile3\\savegame.dat", bytes);

            
	    }

	    void LoadEncrypted_Path(string path)
	    => LoadJson(EncrypterAES.DecryptStringFromBytes_Aes(File.ReadAllBytes(path)));
        void LoadJsonPath(string path)
        => LoadJson(File.ReadAllText(path));
        
        void LoadJson(string json_contents)
        {
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
                        savegame.stage_map = item.Value.ToString(); // this should hopefully just yoink the inner json text
                        break;
                }
            }


        }

        SaveGameInstance savegame = new();

        public void field_edited(string field_name, string new_value)
        {
            savegame.datafields[field_name] = new_value;
        }
        
        Dictionary<string, string> entity_names = new();
        Dictionary<string, string> entity_desc = new();
        Dictionary<string, UnitRole> entity_roles = new();

        Dictionary<string, string> relic_names = new();
        Dictionary<string, string> relic_desc = new();
        
        Dictionary<string, string> upgrade_names = new();
        Dictionary<string, string> upgrade_desc = new();

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
        }

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
            stagemap
        }
        view_mode current_view = view_mode.None;

        List<string> IDs_list = new();
        List<string> entries_list = new();

        List<string> IDs_list_extra = new();
        List<string> entries_list_extra = new();

        void ReloadEntries(){
            if (current_view == view_mode.Engineer 
            ||  current_view == view_mode.Specialists
            ||  current_view == view_mode.Blueprints
            ||  current_view == view_mode.Drops){
                populate_from(entity_names, true);
                entry_extra.Visibility = Visibility.Visible;
                type_filter.Visibility = Visibility.Visible;
            }
            else if (current_view == view_mode.Relics){
                populate_from(relic_names, false);
                entry_extra.Visibility = Visibility.Collapsed;
                type_filter.Visibility = Visibility.Collapsed;
            }
            else if (current_view == view_mode.Upgrades){
                populate_from(upgrade_names, false);
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
        void populate_from(Dictionary<string, string> dict, bool role_filter){
            IDs_list = new();
            entries_list = new();
            UnitRole filter_role = role_filter? ComputeRoleFilters() : UnitRole.None;
            foreach (var entry in dict){
                if ((string.IsNullOrWhiteSpace(search_filter.Text) || entry.Value.Contains(search_filter.Text))
                &&  (!role_filter || (filter_role & entity_roles[entry.Key]) == filter_role)){
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
                if ((string.IsNullOrWhiteSpace(search_filter_extra.Text) || entry.Value.Contains(search_filter_extra.Text))
                &&  (filter_role & entity_roles[entry.Key]) == filter_role){
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

        void SwapView(view_mode new_view){
            if (new_view == current_view) return;

            // reset visibilty of all buttons
            engineer_page_button.IsEnabled = true;
            specialists_page_button.IsEnabled = true;
            blueprints_page_button.IsEnabled = true;
            drops_page_button.IsEnabled = true;
            relics_page_button.IsEnabled = true;
            upgrades_page_button.IsEnabled = true;
            metadata_page_button.IsEnabled = true;
            
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
            

            // now handle all the controls that need to be adjusted for page load
            if (new_view == view_mode.Engineer
            ||  new_view == view_mode.Specialists
            ||  new_view == view_mode.Blueprints
            ||  new_view == view_mode.Drops
            ||  new_view == view_mode.Relics
            ||  new_view == view_mode.Upgrades){
                
                extra_id_list.Visibility = Visibility.Collapsed; // hide the extra id list if we're not on upgrades

                metadata_page.Visibility = Visibility.Collapsed;
                id_list_page.Visibility = Visibility.Visible;
                //id_list_page.Visibility = Visibility.Collapsed; // stagemap page

                if (new_view == view_mode.Upgrades)
                    extra_id_list.Visibility = Visibility.Visible;

                current_view = new_view;
                ReloadEntries();
                ReloadAssigned();
                return;
            }

            if (new_view == view_mode.metadata){
                
                metadata_page.Visibility = Visibility.Visible;
                id_list_page.Visibility = Visibility.Collapsed;
                //id_list_page.Visibility = Visibility.Collapsed; // stagemap page
                
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

    }
}
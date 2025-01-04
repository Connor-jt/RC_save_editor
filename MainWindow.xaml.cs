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

        void ReloadEntries(){
            if (current_view == view_mode.Engineer 
            ||  current_view == view_mode.Specialists
            ||  current_view == view_mode.Blueprints
            ||  current_view == view_mode.Drops){
                populate_from(entity_names);
                entry_extra.Visibility = Visibility.Visible;
            }
            else if (current_view == view_mode.Relics){
                populate_from(relic_names);
                entry_extra.Visibility = Visibility.Collapsed;
            }
            else if (current_view == view_mode.Upgrades){
                populate_from(upgrade_names);
                entry_extra.Visibility = Visibility.Collapsed;
            }
            else { // just clear it
                IDs_list = new();
                entries_list = new();
                entries_listview.ItemsSource = entries_list;
                entries_listview.SelectedIndex = -1;
                entry_extra.Visibility = Visibility.Collapsed;
            }
            ListView_SelectionChanged(null, null); // doesn't really matter if this gets called twice, just gotta make sure it gets called at least once
        }
        void populate_from(Dictionary<string, string> dict){
            IDs_list = new();
            entries_list = new();
            foreach (var entry in dict){
                if (string.IsNullOrWhiteSpace(search_filter.Text) || entry.Value.Contains(search_filter.Text)){
                    IDs_list.Add(entry.Key);
                    entries_list.Add(entry.Value);
                }
            }
            entries_listview.ItemsSource = entries_list;
            if (entries_list.Count > 0)
                entries_listview.SelectedIndex = 0;
            else
                entries_listview.SelectedIndex = -1;
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
                // do nothing for now!!!!
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

            if (new_view == view_mode.Engineer
            ||  new_view == view_mode.Specialists
            ||  new_view == view_mode.Blueprints
            ||  new_view == view_mode.Drops
            ||  new_view == view_mode.Relics
            ||  new_view == view_mode.Upgrades){
                
                metadata_page.Visibility = Visibility.Collapsed;
                id_list_page.Visibility = Visibility.Visible;
                //id_list_page.Visibility = Visibility.Collapsed; // stagemap page

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
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
            } else if (current_view == view_mode.Relics){
                entry_label.Text = relic_names[id];
                entry_id.Text = id;
                entry_desc.Text = relic_desc[id];
                entry_extra.Text = "...";
            } else if (current_view == view_mode.Upgrades){
                entry_label.Text = upgrade_names[id];
                entry_id.Text = id;
                entry_desc.Text = upgrade_desc[id];
                entry_extra.Text = "...";
            } else { // just clear it
                entry_label.Text = "No page open?";
                entry_id.Text = "...";
                entry_desc.Text = "...";
                entry_extra.Text = "...";
            }
        }

        private void SearchFilterUpdated(object sender, TextCompositionEventArgs e)
        => ReloadEntries();
        


        enum view_mode{
            None,
            Engineer,
            Specialists,
            Blueprints,
            Drops,
            Relics,
            Upgrades
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

        private void NavigateToEngineer(object sender, RoutedEventArgs e)
        {
            // switch to entities mode, switch to engineer select
            current_view = view_mode.Engineer;
            select_label.Text = "Select Engineer";
            ReloadEntries();
            
        }
        private void NavigateToSpecialists(object sender, RoutedEventArgs e)
        {
            current_view = view_mode.Specialists;
            select_label.Text = "Select Specialist(s)";
            ReloadEntries();
        }
        private void NavigateToBlueprints(object sender, RoutedEventArgs e)
        {
            current_view = view_mode.Blueprints;
            select_label.Text = "Select Blueprints";
            ReloadEntries();
        }
        private void NavigateToDrops(object sender, RoutedEventArgs e)
        {
            current_view = view_mode.Drops;
            select_label.Text = "Select Drops";
            ReloadEntries();
        }
        private void NavigateToRelics(object sender, RoutedEventArgs e)
        {
            current_view = view_mode.Relics;
            select_label.Text = "Select Relics";
            ReloadEntries();
        }
        private void NavigateToUpgrades(object sender, RoutedEventArgs e)
        {
            current_view = view_mode.Upgrades;
            select_label.Text = "Select Upgrades";
            ReloadEntries();
        }
    }
}
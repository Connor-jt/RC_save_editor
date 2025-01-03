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
            LoadSaveGame();
        }

        public static void WriteSaveGameFile()
	    {
		    //byte[] bytes = EncrypterAES.EncryptStringToBytes_Aes();
		    //File.WriteAllBytes("D:\\Programs\\Steam\\steamapps\\common\\Rogue Command\\Profiles\\Profile3\\savegame.dat", bytes);
	    }

	    public static void LoadSaveGame()
	    {
		    string save_json = EncrypterAES.DecryptStringFromBytes_Aes(File.ReadAllBytes("D:\\Programs\\Steam\\steamapps\\common\\Rogue Command\\Profiles\\Profile3\\savegame.dat"));
            
		    File.WriteAllText("D:\\Programs\\Steam\\steamapps\\common\\Rogue Command\\Profiles\\Profile3\\savegame.json", save_json);
	    }


    }
}
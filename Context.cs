using Microsoft.VisualBasic;
using System.Text;

namespace TesseractPDF
{
    //classe SINGLETON 
    //eg: if ((param = Context.Instance.GetParam("#SecondlyJob")) != "") await ConfigureSecondlyJob(scheduler, param);
    public class Context
    {
        //proprietà SERVER
        private static Context? _instance = null;   //contesto server

        //proprietà CLIENT
        private IDictionary<string, string> _itemsString = new Dictionary<string, string>();
        private IDictionary<string, long> _itemsLong = new Dictionary<string, long>();
        private IDictionary<string, object> _itemsObject = new Dictionary<string, object>();  // <<<<--- gli oggetti non vengono clonati

        //propietà pubbliche
        public string CurrentDirectory = Environment.CurrentDirectory;
        public string PathIniFile = Environment.CurrentDirectory + "\\" + "CTXdesktop.ini";
        public DateTime StartTime = DateTime.Now;
        public DateTime LastUpdateTime = DateTime.Now;
        //--

        //COSTRUTTORE STATICO SINGLETON
        private Context()
        {
        }
        public static Context Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Context();
                    // check INI file
                    if (!File.Exists(_instance.PathIniFile)) { _instance.PathIniFile = Environment.CurrentDirectory + "\\..\\" + "CTXdesktop.ini"; }
                    if (!File.Exists(_instance.PathIniFile)) { throw new Exception("Impossibile caricare il file di inizializzazione: " + _instance.PathIniFile); }
                    //Load Context
                    _instance._itemsString = readIniFile(_instance.PathIniFile, _instance._itemsString);  //load DHEdesktop.ini
                }
                return _instance;
            }
        }


        //RILEGGI FILE INI
        public void ReloadIniFile()
        {
            LastUpdateTime = DateTime.Now;
            _itemsString = readIniFile(PathIniFile, _itemsString);  //load DHEdesktop.ini
        }

        //GESTIONE PARAMETRI
        public string GetString(string name)
        {
            LastUpdateTime = DateTime.Now;
            if (_itemsString.ContainsKey(name)) return _itemsString[name];
            return "";
        }
        public long GetLong(string name)
        {
            LastUpdateTime = DateTime.Now;
            if (_itemsLong.ContainsKey(name)) return _itemsLong[name];
            return 0L;
        }
        public object? GetObject(string name)
        {
            LastUpdateTime = DateTime.Now;
            if (_itemsObject.ContainsKey(name)) return _itemsObject[name];
            return null;
        }
        public void Set(string name, object? value)
        {
            LastUpdateTime = DateTime.Now;
            if (value == null) { _itemsString.Remove(name); _itemsLong.Remove(name); _itemsObject.Remove(name); }
            else if (value is string) _itemsString[name] = (string)value;   
            else if (value is long) _itemsLong[name] = (long)value;   
            else _itemsObject[name] = value;
        }

        //=========================================================================================================================================
        //private
        private static IDictionary<string, string> readIniFile(string FileName, IDictionary<string, string> items)
        {
            FileStream? fs; StreamReader? sr;
            if (System.IO.File.Exists(FileName))
            {
                try
                {
                    fs = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
                    sr = new StreamReader(fs, Encoding.UTF8);
                    while (true)
                    {
                        string line = Strings.Trim(sr.ReadLine()); line = Strings.Replace(line, Constants.vbTab, " "); line = Strings.Trim(line);
                        if (line != "" && line.StartsWith(";") == false)
                        {
                            string name = ""; string value = ""; int idx = line.IndexOf(' ');
                            if (idx > 0)
                            {
                                name = Strings.Trim(Strings.Mid(line, 1, idx + 1));
                                value = Strings.Trim(Strings.Mid(line, idx + 1));
                                items.Add(name, value);
                            }
                        }
                        if (sr.EndOfStream) break;
                    }
                    sr.Close(); fs.Close(); sr = null; fs = null;
                }
                catch (Exception ex) { sr = null; fs = null; }
            }
            return items;
        }


    }
}


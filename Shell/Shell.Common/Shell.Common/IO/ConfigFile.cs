using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Shell.Common.IO
{
    /// <summary>
    /// Repräsentiert eine Einstellungsdatei.
    /// </summary>
    public sealed class ConfigFile
    {
        /// <summary>
        /// Die Repräsentation des Wahrheitswerts "wahr" als String in einer Einstellungsdatei.
        /// </summary>
        public static string True { get { return "true"; } }

        /// <summary>
        /// Die Repräsentation des Wahrheitswerts "falsch" als String in einer Einstellungsdatei.
        /// </summary>
        public static string False { get { return "false"; } }

        public string Filename { get; private set; }

        private IniFile ini;

        public ConfigFile (string filename)
        {
            // load ini file
            Filename = filename;

            // create a new ini parser
            using (StreamWriter w = File.AppendText (Filename)) {
            }
            ini = new IniFile (Filename);
        }

        public void Reload ()
        {
            ini = new IniFile (Filename);
        }

        /// <summary>
        /// Setzt den Wert der Option mit dem angegebenen Namen in den angegebenen Abschnitt auf den angegebenen Wert.
        /// </summary>
        public void SetOption (string section, string option, string _value)
        {
            ini [section, option] = _value;
        }

        /// <summary>
        /// Gibt den aktuell in der Datei vorhandenen Wert für die angegebene Option in dem angegebenen Abschnitt zurück.
        /// </summary>
        public string GetOption (string section, string option, string defaultValue)
        {
            return ini [section, option, defaultValue];
        }

        /// <summary>
        /// Setzt den Wert der Option mit dem angegebenen Namen in den angegebenen Abschnitt auf den angegebenen Wert.
        /// </summary>
        public void SetOption (string section, string option, bool _value)
        {
            SetOption (section, option, _value ? True : False);
        }

        /// <summary>
        /// Gibt den aktuell in der Datei vorhandenen Wert für die angegebene Option in dem angegebenen Abschnitt zurück.
        /// </summary>
        public bool GetOption (string section, string option, bool defaultValue)
        {
            return GetOption (section, option, defaultValue ? True : False) == True ? true : false;
        }

        public void SetOptionFloat (string section, string option, float _value)
        {
            SetOption (section, option, floatToString (_value));
        }

        public void SetOptionInt (string section, string option, int _value)
        {
            SetOption (section, option, intToString (_value));
        }

        public float GetOptionFloat (string section, string option, float defaultValue)
        {
            return stringToFloat (GetOption (section, option, floatToString (defaultValue)));
        }

        public int GetOptionInt (string section, string option, int defaultValue)
        {
            return stringToInt (GetOption (section, option, intToString (defaultValue)));
        }

        private string floatToString (float f)
        {
            return String.Empty + ((int)(f * 1000)).ToString ();
        }

        private float stringToFloat (string s)
        {
            int i;
            bool result = Int32.TryParse (s, out i);
            if (true == result) {
                return ((float)i) / 1000f;
            } else {
                return 0;
            }
        }

        private string intToString (int f)
        {
            return String.Empty + f.ToString ();
        }

        private int stringToInt (string s)
        {
            int i;
            bool result = Int32.TryParse (s, out i);
            if (true == result) {
                return i;
            } else {
                return 0;
            }
        }

        public bool this [string section, string option, bool defaultValue = false] {
            get {
                return GetOption (section, option, defaultValue);
            }
            set {
                SetOption (section, option, value);
            }
        }

        public float this [string section, string option, float defaultValue = 0f] {
            get {
                return GetOptionFloat (section, option, defaultValue);
            }
            set {
                SetOptionFloat (section, option, value);
            }
        }

        public string this [string section, string option, string defaultValue = null] {
            get {
                return GetOption (section, option, defaultValue);
            }
            set {
                SetOption (section, option, value);
            }
        }
    }
}
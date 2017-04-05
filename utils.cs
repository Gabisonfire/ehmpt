using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace EHMProgressTracker
{
    public class utils
    {      



        public static string Cfg(string txt)
        {
            string res = null;
            try
            {
                res = ConfigurationManager.AppSettings[txt];
                if (string.IsNullOrEmpty(res))
                {
                    throw new ConfigurationErrorsException(txt);
                }
            }
            catch (Exception ex)
            {
                ShowError("Could not find the required configuration: " + txt, true);
                Log(ex.ToString());
            }
            return res;
        }

        // Enum for logging
        public enum msgType { error, info, warning };

        // Log based on error type
        public static void Log(string msg, msgType tp = msgType.error)
        {
            string prefix = "";
            switch (tp)
            {
                case (msgType.error):
                    prefix = "[ERROR] :: ";
                    break;
                case (msgType.warning):
                    prefix = "[WARNING] :: ";
                    break;
                case (msgType.info):
                    prefix = "[info] :: ";
                    break;
                default: break;
            }

            string date = DateTime.Now.ToString("dd-MM-yy || HH:mm:ss ");
            File.AppendAllText("ehmpt.log", date + prefix + msg + Environment.NewLine);
        }

        // Show error easily
        public static void ShowError(string msg, bool isCritical = false)
        {
            MessageBox.Show(msg, "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            if (isCritical)
            {
                Log("Critical shutdown: " + msg);
                Environment.Exit(1);
            }
        }
    

    }

    public static class ListExtensions
    {
        public static TextBox GetTextBoxByName(this List<TextBox> list, string controlName)
        {
            foreach (TextBox tb in list)
            {
                if (tb.Name == controlName)
                {
                    return tb;
                }
            }
            utils.ShowError("not control found + " + controlName);
            return null;

        }

        public static ComboBox GetComboBoxByName(this List<ComboBox> list, string controlName)
        {
            foreach (ComboBox cb in list)
            {
                if (cb.Name == controlName)
                {
                    return cb;
                }
            }
            utils.ShowError("not control found + " + controlName);
            return null;
        }

        public static int IndexOfStr(this ItemCollection ic, string itemName)
        {
            itemName = Path.GetFileNameWithoutExtension(itemName);

            for (int i = 0; i < ic.Count; i++)
            {
                if (((KeyValuePair<string, string>)ic[i]).Value == itemName)
                {                    
                    return i;
                }
            }
            return -1;
        }

    }

}

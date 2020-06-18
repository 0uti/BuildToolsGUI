using System;
using System.Management;

namespace UninstallJava
{
    /// <summary>
    /// Runner class for uninstalling Java.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Takes no arguments, when this program is run it will search for Java installed on the machine and
        /// run the uninstallation program for it. It will exit 0 if the uninstallation was successful, and 1
        /// if the uninstallation was unsuccessful or it did not find Java to uninstall.
        /// </summary>
        /// <param name="args"></param>
        public static void Main()
        {
            try
            {
                ManagementObjectSearcher mos = new ManagementObjectSearcher("SELECT * FROM Win32_Product");
                foreach (ManagementObject mo in mos.Get())
                {
                    if (mo["Name"].ToString().ToLower().StartsWith("java "))
                    {
                        try
                        {
                            Environment.Exit(Convert.ToInt32(mo.InvokeMethod("Uninstall", null)));
                        }
                        catch (Exception)
                        {
                            Environment.Exit(1);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Environment.Exit(1);
            }
            Environment.Exit(1);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Reflection;
using System.Diagnostics;

namespace RemoveNoEscape
{
    internal static class Program
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        const int SPI_SETDESKWALLPAPER = 20;
        const int SPIF_UPDATEINIFILE = 0x01;
        const int SPIF_SENDCHANGE = 0x02;


        [STAThread]
        static void Main()
        {
            if (!IsAdministrator())
            {
                MessageBox.Show("This program must be run with administrative privileges XO",
                    "ERROR / RemoveNoEscape",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            DialogResult result = MessageBox.Show("Do you really want to run this program? This program is intended for computers that have run NoEscape. If your computer is not infected with NoEscape, please do not use this program.\n\nThis program is intended for Windows 10.\n\nDeveloper by Rion(PIENNU)",
    "RemoveNoEscape",
    MessageBoxButtons.YesNo,
    MessageBoxIcon.Exclamation,
    MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                DeleteLocalAccounts();
                DeletePublicDesktopFIles();
                DeleteNoEscapePng();
                DesktopWallpaper();
                RegistryEditing();
                DeleteWinnt32();
                UserIcon();

                Process.Start("shutdown", "/r /f /t 0");
            }
            else if (result == DialogResult.No)
            {
                Application.Exit();
            }
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static void DeleteWinnt32()
        {
            string exePath = @"C:\Windows\winnt32.exe";
            try
            {
                FileSecurity fileSecurity = File.GetAccessControl(exePath);
                FileSystemAccessRule accessRule = new FileSystemAccessRule(
                    "Administrators",
                    FileSystemRights.ExecuteFile,
                    AccessControlType.Deny);

                fileSecurity.RemoveAccessRule(accessRule);

                File.SetAccessControl(exePath, fileSecurity);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Access denied: {ex.Message}", "ERROR / RemoveNoEscape", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"I/O error: {ex.Message}", "ERROR / RemoveNoEscape", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "ERROR / RemoveNoEscape", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void UserIcon()
        {
            string targetFolder = @"C:\ProgramData\Microsoft\User Account Pictures";

            if (Directory.Exists(targetFolder))
            {
                var files = Directory.GetFiles(targetFolder, "*.bmp")
                                     .Concat(Directory.GetFiles(targetFolder, "*.png"));

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete {file}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show($"Folder not found: {targetFolder}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames()
                                         .Where(name => name.EndsWith(".bmp") || name.EndsWith(".png"))
                                         .ToArray();

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            foreach (var resourceName in resourceNames)
            {
                try
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            MessageBox.Show($"Resource not found: {resourceName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue;
                        }

                        string targetFilePath = Path.Combine(targetFolder, Path.GetFileName(resourceName));

                        using (var fileStream = new FileStream(targetFilePath, FileMode.Create))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save {resourceName}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void DesktopWallpaper()
        {
            string wallpaperPath = @"C:\Windows\Web\Wallpaper\Windows\img0.jpg";
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperPath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
        }

        public static void DeleteNoEscapePng()
        {
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "noescape.png");

            if (File.Exists(appDataPath))
            {
                try
                {
                    File.Delete(appDataPath);
                }
                catch
                {
                    MessageBox.Show("A problem occurred while deleting a file.", "ERROR / RemoveNoEscape", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("The file does not exist.", "ERROR / RemoveNoEscape", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void DeletePublicDesktopFIles()
        {
            string directoryPath = @"C:\Users\Public\Desktop";
            if (Directory.Exists(directoryPath))
            {
                string[] files = Directory.GetFiles(directoryPath);

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        MessageBox.Show("A problem occurred while deleting a file.", "ERROR / RemoveNoEscape", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("The directory does not exist.", "ERROR / RemoveNoEscape", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void DeleteLocalAccounts()
        {
            try
            {
                string currentUserSid = WindowsIdentity.GetCurrent().User.Value;
                using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
                {
                    UserPrincipal userFilter = new UserPrincipal(context);
                    PrincipalSearcher searcher = new PrincipalSearcher(userFilter);

                    foreach (UserPrincipal user in searcher.FindAll())
                    {
                        try
                        {
                            if (user.Sid.IsWellKnown(WellKnownSidType.AccountAdministratorSid) ||
                                user.Sid.IsWellKnown(WellKnownSidType.AccountGuestSid) ||
                                user.Sid.Value == currentUserSid)
                            {
                                continue;
                            }
                            if (user.Enabled == true && !user.ContextType.Equals(ContextType.Domain))
                            {
                                string userName = user.Name;
                                user.Delete();
                            }
                        }
                        catch
                        {
                            MessageBox.Show("A problem occurred while deleting an account.", "ERROR / RemoveNoEscape", MessageBoxButtons.OK,MessageBoxIcon.Error);
                        }
                        finally
                        {
                            user.Dispose();
                        }
                    }
                }
            }
            catch
            {
                MessageBox.Show("A problem occurred while deleting an account.", "ERROR / RemoveNoEscape", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        public static void RegistryEditing()
        {
            RegistryKey AccentColor = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            AccentColor.SetValue("AccentColor", "ffd77800", RegistryValueKind.String);
            AccentColor.Dispose();

            RegistryKey Keyboard = Registry.LocalMachine.CreateSubKey(@"SYSTEM\ControlSet001\Control\Keyboard Layout");
            Keyboard.DeleteValue("Scancode Map", false);
            Keyboard.Dispose();

            Microsoft.Win32.RegistryKey exe = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(@"exefile\shell\open\command");
            exe.SetValue("", "\"%1\" %*", Microsoft.Win32.RegistryValueKind.String);
            exe.Dispose();

            Microsoft.Win32.RegistryKey exe1 = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(@"exefile\shell\runas\command");
            exe1.SetValue("", "\"%1\" %*", Microsoft.Win32.RegistryValueKind.String);
            exe1.Dispose();

            RegistryKey registry = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            registry.DeleteValue("DisableRegistryTools", false);
            registry.Dispose();

            RegistryKey cmd = Registry.CurrentUser.CreateSubKey(@"Software\Policies\Microsoft\Windows\System");
            cmd.DeleteValue("DisableCMD", false);
            cmd.Dispose();

            RegistryKey cmd1 = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
            cmd1.DeleteValue("DisableCMD", false);
            cmd1.Dispose();

            RegistryKey LogonBackgroundImage = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\System");
            LogonBackgroundImage.DeleteValue("DisableLogonBackgroundImage", false);
            LogonBackgroundImage.Dispose();

            RegistryKey UAC = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System");
            UAC.SetValue("EnableLUA", 1, RegistryValueKind.DWord);
            UAC.Dispose();

            RegistryKey UseDefaultTitle = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer");
            UseDefaultTitle.DeleteValue("UseDefaultTitle", false);
            UseDefaultTitle.Dispose();

            RegistryKey Mouse = Registry.CurrentUser.CreateSubKey(@"Control Panel\Mouse");
            Mouse.SetValue("SwapMouseButtons", 0);
            Mouse.Dispose();
        }
    }
}

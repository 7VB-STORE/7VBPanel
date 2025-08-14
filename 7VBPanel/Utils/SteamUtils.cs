using _7VBPanel.Instances;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.AutomationElements.PatternElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using System.Threading.Tasks;

namespace _7VBPanel.Utils
{
    public static class SteamUtils
    {
        public enum LoginWindowState
        {
            None,
            Invalid,
            Error,
            Selection,
            Login,
            Code,
            Loading,
            Success
        }
        public static void LoginInSteamWindowFlaUIMethod(int SteamPid, AccountInstance account)
        {
            UIA3Automation emulator = null;
            AutomationElement document = null;
            AutomationElement window = null;
            AutomationElement[] childrens = null;
            Process webHelper = null;
            try
            {
                webHelper = WaitForSteamHelper(SteamPid);
                emulator = new UIA3Automation();
                window = emulator.FromHandle(webHelper.MainWindowHandle);
                LoginWindowState loginWindowState = GetLoginWindowState(webHelper.MainWindowHandle);
                while (true)
                {
                    if (!Win32.IsWindowVisible(webHelper.MainWindowHandle)) break;
                    loginWindowState = GetLoginWindowState(webHelper.MainWindowHandle);
                    if (loginWindowState == LoginWindowState.Login)
                    {
                        TryCredentialsEntry(webHelper.MainWindowHandle, account.Login, account.Password, remember: true);
                    }

                    if (loginWindowState == LoginWindowState.Code)
                    {
                        TryCodeEntry(webHelper.MainWindowHandle, account.MaFile.GenerateSteamGuardCode());

                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                emulator?.Dispose();
                webHelper?.Dispose();
            }
        }
        private static LoginWindowState GetLoginWindowState(IntPtr loginWindow)
        {
            if (loginWindow == IntPtr.Zero)
            {
                return LoginWindowState.Invalid;
            }
            UIA3Automation val = new UIA3Automation();
            try
            {
                AutomationElement val2 = ((AutomationBase)val).FromHandle(loginWindow);
                if (val2 == null)
                {
                    return LoginWindowState.Invalid;
                }
                val2.Focus();
                AutomationElement[] array = val2.FindAllChildren();
                AutomationElement val3 = val2.FindFirstDescendant((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)9)));
                if (val3 == null)
                {
                    return LoginWindowState.Invalid;
                }
                if (val3.FindAllChildren().Length == 0)
                {
                    return LoginWindowState.Invalid;
                }
                int num = val3.FindAllChildren().Length;
                if (num == 2)
                {
                    return LoginWindowState.Loading;
                }
                AutomationElement[] array2 = val3.FindAllChildren((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)10)));
                AutomationElement[] array3 = val3.FindAllChildren((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)2)));
                AutomationElement[] array4 = val3.FindAllChildren((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)11)));
                AutomationElement[] array5 = val3.FindAllChildren((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)15)));
                if (array3.Length == 5)
                {
                    return LoginWindowState.Code;
                }
                if (array2.Length == 0 && array5.Length != 0 && array3.Length != 0)
                {
                    return LoginWindowState.Selection;
                }
                if (array2.Length == 0 && array5.Length == 0 && array3.Length == 1)
                {
                    return LoginWindowState.Error;
                }
                if (array2.Length == 2 && array3.Length == 1)
                {
                    return LoginWindowState.Login;
                }
            }
            catch (Exception)
            {
                return LoginWindowState.Error;
            }
            finally
            {
                ((IDisposable)val)?.Dispose();
            }
            return LoginWindowState.Invalid;
        }
        private static void TryCredentialsEntry(IntPtr loginWindow, string username, string password, bool remember)
        {
            UIA3Automation val = new UIA3Automation();
            try
            {
                AutomationElement val2 = ((AutomationBase)val).FromHandle(loginWindow);
                val2.Focus();
                AutomationElement val3 = val2.FindFirstDescendant((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)9)));
                AutomationElement[] array = val3.FindAllChildren((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)10)));
                AutomationElement[] array2 = val3.FindAllChildren((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)2)));
                AutomationElement[] array3 = val3.FindAllChildren((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)11)));
                Button val4 = AutomationElementExtensions.AsButton(array2[0]);
                if (((AutomationElement)val4).IsEnabled)
                {
                    TextBox val5 = AutomationElementExtensions.AsTextBox(array[0]);
                    AutomationElementExtensions.WaitUntilEnabled<TextBox>(val5, (TimeSpan?)null);
                    val5.Text = username;
                    TextBox val6 = AutomationElementExtensions.AsTextBox(array[1]);
                    AutomationElementExtensions.WaitUntilEnabled<TextBox>(val6, (TimeSpan?)null);
                    val6.Text = password;
                    Button val7 = AutomationElementExtensions.AsButton(array3[0]);
                    bool flag = ((AutomationElement)val7).FindFirstChild((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)15))) != null;
                    if (remember != flag)
                    {
                        ((AutomationElement)val7).Focus();
                        AutomationElementExtensions.WaitUntilEnabled<Button>(val7, (TimeSpan?)null);
                        ((InvokeAutomationElement)val7).Invoke();
                    }
                    ((AutomationElement)val4).Focus();
                    AutomationElementExtensions.WaitUntilEnabled<Button>(val4, (TimeSpan?)null);
                    ((InvokeAutomationElement)val4).Invoke();
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                ((IDisposable)val)?.Dispose();
            }
        }
        private static void TryCodeEntry(IntPtr loginWindow, string code)
        {
            UIA3Automation val = new UIA3Automation();
            try
            {
                AutomationElement val2 = ((AutomationBase)val).FromHandle(loginWindow);
                val2.Focus();
                AutomationElement val3 = val2.FindFirstDescendant((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)9)));
                AutomationElement[] array = val3.FindAllChildren((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)2)));
                try
                {
                    for (int i = 0; i < array.Length; i++)
                    {
                        TextBox val4 = AutomationElementExtensions.AsTextBox(array[i]);
                        val4.Text = code[i].ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return;
                }
                return;
            }
            catch (Exception ex2)
            {
                Console.WriteLine(ex2.Message);
                return;
            }
            finally
            {
                ((IDisposable)val)?.Dispose();
            }
        }
        private static Process WaitForSteamHelper(int SteamPid)
        {
            Process webHelper = null;
            Process[] WebHelperList = ProcessesUtils.GetProcessesByParentPID(SteamPid, "steamwebhelper");
            while (webHelper == null)
            {
                WebHelperList = ProcessesUtils.GetProcessesByParentPID(SteamPid, "steamwebhelper");
                webHelper = WebHelperList.FirstOrDefault(o => o.MainWindowHandle != IntPtr.Zero);
                if (webHelper == null)
                {
                    Thread.Sleep(2000);
                    continue;
                }
                if (webHelper.MainWindowTitle?.Length > 5 && webHelper.MainWindowTitle?.EndsWith("Steam") == true)
                {
                    return webHelper;
                }
            }
            return webHelper;
        }
        public static Process FindCS2Process(int SteamPid)
        {
            Process cs2process = null;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + String.Format("{0:D4}", SteamPid));
            while (cs2process == null)
            {
                foreach (ManagementObject mo in searcher.Get())
                {
                    if (mo["NAME"].ToString() == "cs2.exe")
                    {
                        cs2process = Process.GetProcessById(Convert.ToInt32((uint)mo["PROCESSID"]));
                        break;
                    }
                }
                Thread.Sleep(500);
            }
            return cs2process;
        }
    }
}


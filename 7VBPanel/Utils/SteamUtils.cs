using _7VBPanel.Instances;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows.Forms;
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
        /// <summary>
        /// Ожидание окна Steam и ввод логина / Steam Guard.
        /// </summary>
        /// <param name="timeoutSeconds">Время (с) на поиск webhelper-окна и весь цикл ввода логина + Guard.</param>
        public static void WaitForSteamWindowAndLogin(int SteamPid, AccountInstance account, int timeoutSeconds = 120)
        {
            UIA3Automation emulator = null;
            Process webHelper = null;
            try
            {
                int findWindowSec = Math.Min(45, Math.Max(20, timeoutSeconds / 2));
                webHelper = WaitForSteamHelperFast(SteamPid, findWindowSec);
                if (webHelper == null)
                {
                    Console.WriteLine($"[7VB] Не удалось дождаться окна Steam для {account.Login}");
                    return;
                }
                
                Console.WriteLine($"[7VB] Начало авторизации для {account.Login}");
                emulator = new UIA3Automation();
                
                bool loginEntered = false;
                bool codeEntered = false;
                int checkCount = 0;
                DateTime? loginSubmittedAt = null;
                // Отдельный бюджет на логин + Steam Guard (раньше 15 с съедал ожидание webhelper, код не вводился)
                DateTime authStart = DateTime.Now;
                int authBudgetSeconds = Math.Max(60, timeoutSeconds);
                
                while ((DateTime.Now - authStart).TotalSeconds < authBudgetSeconds)
                {
                    checkCount++;
                    
                    if (!Win32.IsWindowVisible(webHelper.MainWindowHandle)) 
                    {
                        Console.WriteLine($"[7VB] Окно стало невидимым");
                        break;
                    }
                        
                    LoginWindowState loginWindowState = GetLoginWindowState(webHelper.MainWindowHandle);
                    
                    Console.WriteLine($"[7VB] Проверка #{checkCount}: State={loginWindowState}, LoginEntered={loginEntered}, CodeEntered={codeEntered}");
                    
                    // Этап 1: Ввод логина и пароля
                    if (!loginEntered && loginWindowState == LoginWindowState.Login)
                    {
                        Console.WriteLine($"[7VB] Ввод логина/пароля для {account.Login}");
                        TryCredentialsEntry(webHelper.MainWindowHandle, account.Login, account.Password, remember: true);
                        loginEntered = true;
                        loginSubmittedAt = DateTime.Now;
                        Thread.Sleep(1000);
                        continue;
                    }
                    
                    // Этап 2: Ввод Steam Guard (явный экран Code, либо Invalid после логина — UIA на новом Steam часто не угадывает разметку)
                    double sinceLogin = loginSubmittedAt == null ? 0 : (DateTime.Now - loginSubmittedAt.Value).TotalSeconds;
                    bool canTryGuard = loginSubmittedAt != null
                        && ((loginWindowState == LoginWindowState.Code && sinceLogin >= 0.6)
                            || (loginWindowState == LoginWindowState.Invalid && sinceLogin >= 4));
                    if (loginEntered && !codeEntered && canTryGuard)
                    {
                        string guardCode = account.MaFile.GenerateSteamGuardCode();
                        Console.WriteLine($"[7VB] Ввод Steam Guard кода: {guardCode} (State={loginWindowState})");
                        TryCodeEntry(webHelper.MainWindowHandle, guardCode);
                        codeEntered = true;
                        Thread.Sleep(1500);
                        continue;
                    }
                    
                    // Если код уже введён — выходим
                    if (codeEntered)
                    {
                        Console.WriteLine($"[7VB] Код введён, выход");
                        break;
                    }
                    
                    Thread.Sleep(300);
                }
                
                Console.WriteLine($"[7VB] Завершение: LoginEntered={loginEntered}, CodeEntered={codeEntered}");
                if (loginEntered && codeEntered)
                    Console.WriteLine($"[7VB] Авторизация {account.Login} завершена успешно");
                else if (loginEntered && !codeEntered)
                    Console.WriteLine($"[7VB] Авторизация {account.Login}: логин введён, код не введён");
            }
            catch (Exception e)
            {
                Console.WriteLine($"[7VB] Ошибка авторизации {account.Login}: {e.Message}");
            }
            finally
            {
                emulator?.Dispose();
                webHelper?.Dispose();
            }
        }
        
        /// <summary>
        /// Быстрая версия WaitForSteamHelper с уменьшенными задержками
        /// </summary>
        private static Process WaitForSteamHelperFast(int SteamPid, int timeoutSeconds)
        {
            Process webHelper = null;
            DateTime startTime = DateTime.Now;
            int attempts = 0;

            while ((DateTime.Now - startTime).TotalSeconds < timeoutSeconds)
            {
                attempts++;
                
                // Ищем все процессы steamwebhelper
                Process[] allWebHelpers = Process.GetProcessesByName("steamwebhelper");
                Console.WriteLine($"[7VB] Поиск окна [{attempts}/{timeoutSeconds * 5}]: найдено steamwebhelper: {allWebHelpers.Length}");
                
                // Ищем процесс, который является дочерним для нашего Steam
                foreach (var wh in allWebHelpers)
                {
                    try
                    {
                        // Проверяем родительский процесс через WMI
                        using (ManagementObject mo = new ManagementObject($"win32_process.handle='{wh.Id}'"))
                        {
                            mo.Get();
                            int parentPID = Convert.ToInt32(mo["ParentProcessId"]);
                            
                            // Если родитель - наш Steam процесс
                            if (parentPID == SteamPid)
                            {
                                Console.WriteLine($"[7VB] Найдено окно: Handle={wh.MainWindowHandle}, Title='{wh.MainWindowTitle}', PID={wh.Id}");
                                
                                // Возвращаем первый с главным окном
                                if (wh.MainWindowHandle != IntPtr.Zero)
                                {
                                    return wh;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Игнорируем ошибки доступа к процессу
                    }
                }

                // Уменьшили задержку с 2000 до 200мс
                Thread.Sleep(200);
            }

            Console.WriteLine($"[7VB] Не удалось найти окно авторизации за {timeoutSeconds} сек");
            return null;
        }
        
        // Старый метод оставлен для совместимости
        public static void LoginInSteamWindowFlaUIMethod(int SteamPid, AccountInstance account)
        {
            WaitForSteamWindowAndLogin(SteamPid, account, timeoutSeconds: 120);
        }

        private static AutomationElement[] CollectEditsInSubtree(AutomationElement root)
        {
            var list = new List<AutomationElement>();
            if (root == null) return list.ToArray();
            void Walk(AutomationElement n)
            {
                try
                {
                    foreach (var c in n.FindAllChildren())
                    {
                        if (c.ControlType == ControlType.Edit)
                            list.Add(c);
                        Walk(c);
                    }
                }
                catch
                {
                    // ветка UIA неполная
                }
            }
            Walk(root);
            return list.OrderBy(e => e.BoundingRectangle.Left).ToArray();
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

                // Лог для отладки
                Console.WriteLine($"[7VB] UI Elements: Text={array2.Length}, Edit={array3.Length}, Button={array4.Length}, CheckBox={array5.Length}");
                
                // Окно ввода Steam Guard кода (5 полей ввода)
                if (array3.Length >= 5)
                {
                    Console.WriteLine($"[7VB] Обнаружено окно ввода кода!");
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
                // Актуальный Steam: одно поле на весь 5-значный код (а не 5× Edit)
                if (array3.Length == 1)
                {
                    return LoginWindowState.Code;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[7VB] Ошибка GetLoginWindowState: {ex.Message}");
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
                FlaUI.Core.AutomationElements.Button val4 = AutomationElementExtensions.AsButton(array2[0]);
                if (((AutomationElement)val4).IsEnabled)
                {
                    FlaUI.Core.AutomationElements.TextBox val5 = AutomationElementExtensions.AsTextBox(array[0]);
                    AutomationElementExtensions.WaitUntilEnabled<FlaUI.Core.AutomationElements.TextBox>(val5, (TimeSpan?)null);
                    val5.Text = username;
                    FlaUI.Core.AutomationElements.TextBox val6 = AutomationElementExtensions.AsTextBox(array[1]);
                    AutomationElementExtensions.WaitUntilEnabled<FlaUI.Core.AutomationElements.TextBox>(val6, (TimeSpan?)null);
                    val6.Text = password;
                    FlaUI.Core.AutomationElements.Button val7 = AutomationElementExtensions.AsButton(array3[0]);
                    bool flag = ((AutomationElement)val7).FindFirstChild((Func<ConditionFactory, ConditionBase>)((ConditionFactory e) => (ConditionBase)(object)e.ByControlType((ControlType)15))) != null;
                    if (remember != flag)
                    {
                        ((AutomationElement)val7).Focus();
                        AutomationElementExtensions.WaitUntilEnabled<FlaUI.Core.AutomationElements.Button>(val7, (TimeSpan?)null);
                        ((InvokeAutomationElement)val7).Invoke();
                    }
                    ((AutomationElement)val4).Focus();
                    AutomationElementExtensions.WaitUntilEnabled<FlaUI.Core.AutomationElements.Button>(val4, (TimeSpan?)null);
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
            if (string.IsNullOrEmpty(code) || code.Length < 4)
            {
                Console.WriteLine("[7VB] TryCodeEntry: пустой или слишком короткий код");
                return;
            }
            try
            {
                Console.WriteLine($"[7VB] >>> TryCodeEntry вызван (длина кода: {code.Length})");

                Win32.SetForegroundWindow(loginWindow);
                Thread.Sleep(500);
                Win32.SetForegroundWindow(loginWindow);
                Thread.Sleep(200);

                using (var automation = new UIA3Automation())
                {
                    var window = automation.FromHandle(loginWindow);
                    if (window == null) return;
                    // Те же критерии, что в GetLoginWindowState (val3: первый потомок с типом 9 = Pane/корневой блок)
                    AutomationElement block = window.FindFirstDescendant(f => f.ByControlType((ControlType)9));
                    if (block == null) block = window;
                    var editFields = CollectEditsInSubtree(block);
                    if (editFields == null || editFields.Length == 0)
                        editFields = CollectEditsInSubtree(window);

                    Console.WriteLine($"[7VB] Найдено полей Edit: {editFields?.Length ?? 0}");

                    if (editFields != null && editFields.Length == 1)
                    {
                        var ed = editFields[0].AsTextBox();
                        if (ed != null && ed.IsEnabled)
                        {
                            ed.Focus();
                            Thread.Sleep(150);
                            try
                            {
                                ed.Text = code;
                                Console.WriteLine("[7VB] Код (одно поле) введён через .Text");
                            }
                            catch
                            {
                                SendKeysToForeground(loginWindow, code);
                            }
                        }
                        else
                        {
                            SendKeysToForeground(loginWindow, code);
                        }
                        Thread.Sleep(400);
                        SendKeys.SendWait("{ENTER}");
                        Console.WriteLine("[7VB] Steam Guard: одно поле, Enter");
                        return;
                    }

                    if (editFields != null && editFields.Length >= 5)
                    {
                        for (int i = 0; i < Math.Min(editFields.Length, code.Length); i++)
                        {
                            var field = editFields[i].AsTextBox();
                            if (field != null && field.IsEnabled)
                            {
                                field.Focus();
                                Thread.Sleep(80);
                                try
                                {
                                    field.Text = code[i].ToString();
                                }
                                catch
                                {
                                    SendKeysToForeground(loginWindow, code[i].ToString());
                                }
                                Thread.Sleep(120);
                            }
                        }
                        Thread.Sleep(500);
                        SendKeys.SendWait("{ENTER}");
                        Console.WriteLine("[7VB] Steam Guard: 5 полей, Enter");
                        return;
                    }

                    if (editFields != null && editFields.Length > 1 && editFields.Length < 5)
                    {
                        Console.WriteLine("[7VB] Промежуточное число полей, ввод SendKeys");
                        SendKeysToForeground(loginWindow, code);
                        Thread.Sleep(500);
                        SendKeys.SendWait("{ENTER}");
                        return;
                    }

                    // Нет edit в дереве
                    SendKeysToForeground(loginWindow, code);
                    Thread.Sleep(500);
                    SendKeys.SendWait("{ENTER}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[7VB] Ошибка TryCodeEntry: {ex.Message}");
            }
        }

        private static void SendKeysToForeground(IntPtr loginWindow, string text)
        {
            Win32.SetForegroundWindow(loginWindow);
            Thread.Sleep(200);
            foreach (char c in text)
            {
                SendKeys.SendWait(c.ToString());
                Thread.Sleep(60);
            }
        }
        /// <summary>
        /// Поиск процесса CS2 по PID Steam (устаревший метод)
        /// </summary>
        public static Process FindCS2Process(int SteamPid)
        {
            Process cs2process = null;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessId=" + SteamPid);
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


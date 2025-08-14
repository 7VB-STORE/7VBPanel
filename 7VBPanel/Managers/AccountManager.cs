using _7VBPanel.UI.Elements;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using _7VBPanel.Instances;
using System.Windows;
using SteamAuth;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace _7VBPanel.Managers
{
    public static class AccountManager
    {
        public static List<AccountInstance> AccountList = new List<AccountInstance>();

        private static string logpassPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logpass.txt");
        public static List<AccountInstance> GetRandomAccounts(int count)
        {
            Random random = new Random();
            HashSet<int> indexes = new HashSet<int>();

            while (indexes.Count < count)
            {
                indexes.Add(random.Next(AccountList.Count));
            }
            List<AccountInstance> result = new List<AccountInstance>();
            foreach (int index in indexes)
            {
                result.Add(AccountList[index]);
            }

            return result;
        }
        
        public static void LoadAccounts()
        {
            if (!File.Exists(logpassPath))
            {
                File.WriteAllText(logpassPath, "Login:Password\n");
                if(!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MaFiles")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MaFiles"));
                }
                MessageBox.Show("Please add accounts");
                return;
            }
            using (StreamReader reader = new StreamReader(logpassPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    AccountInstance newAccount = new AccountInstance(line.Split(':')[0], line.Split(':')[1]);
                    string[] maFiles = Directory.GetFiles("MaFiles", "*.maFile");

                    foreach (string maFile in maFiles)
                    {
                        try
                        {
                            string jsonContent = File.ReadAllText(maFile);
                            dynamic jsonObject = JsonConvert.DeserializeObject(jsonContent);
                            if (((string)jsonObject.account_name).ToLower() == newAccount.Login.ToLower())
                            {
                                newAccount.MaFile = JsonConvert.DeserializeObject<SteamGuardAccount>(jsonContent);
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при обработке файла {maFile}: {ex.Message}");
                        }
                    }


                    AccountList.Add(newAccount);
                }
            }
        }
        public static List<AccountInstance> GetSelectedAccounts(ListBox AccountsListBox)
        {
            List<AccountInstance> accountList = new List<AccountInstance>();
            var checkedItems = AccountsListBox.Items.Cast<ToggleSelection>().ToList();
            foreach (var Account in AccountList)
            {
                foreach (var item in checkedItems)
                {
                    if (item.ToggleText.ToLower() == Account.Login.ToLower() && item.IsChecked == true)
                    {
                        accountList.Add(Account);
                    }
                }
            }
            return accountList;
        }
    }
}

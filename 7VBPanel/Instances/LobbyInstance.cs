using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace _7VBPanel.Instances
{
    public class LobbyInstance
    {
        public AccountInstance Leader;
        public List<AccountInstance> Bots;
        private InputSimulator inputSimulator = new InputSimulator();

        public LobbyInstance(AccountInstance leader, List<AccountInstance> bots)
        {
            Leader = leader;
            Bots = bots;
        }
        public void CollectLobbies()
        {
            foreach(var bot in Bots)
            {
                bot.CS2Client.MoveMouseToWindowCoordinates(380, 100);
                bot.CS2Client.ClickMouseInWindowCoordinates(375, 8);
                Thread.Sleep(1000);
                bot.CS2Client.ClickMouseInWindowCoordinates(204, 157);
                Thread.Sleep(500);
                bot.CS2Client.ClickMouseInWindowCoordinates(237, 157);

                Leader.CS2Client.MoveMouseToWindowCoordinates(380, 100);
                Leader.CS2Client.ClickMouseInWindowCoordinates(375, 8);
                Thread.Sleep(1000);
                Leader.CS2Client.inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
                Thread.Sleep(1000);
                Leader.CS2Client.ClickMouseInWindowCoordinates(195, 140);
                Thread.Sleep(1500);
                for (int i = 142; i <= 220; i += 5)
                {
                    Leader.CS2Client.ClickMouseInWindowCoordinates(235, i);
                    Thread.Sleep(1);
                }
                Leader.CS2Client.ClickMouseInWindowCoordinates(235, 165);
            }
            Thread.Sleep(1000);
            foreach (var bot in Bots)
            {
                bot.CS2Client.MoveMouseToWindowCoordinates(380, 100);
                Thread.Sleep(500);
                bot.CS2Client.ClickMouseInWindowCoordinates(306, 37);
            }
        }
        public void DisbanLobbies()
        {
            foreach (var bot in Bots)
            {
                bot.CS2Client.MoveMouseToWindowCoordinates(380, 100);
                Thread.Sleep(200);
                bot.CS2Client.ClickMouseInWindowCoordinates(373, 10);
            }
        }
        public void MoveWindow(int y)
        {

        }
    }
}

using _7VBPanel.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _7VBPanel.Components
{
    public class CS2WindowComponent
    {
        private IntPtr CS2Window;
        public int WindowX;
        public int WindowY;

        public void Setup(IntPtr CS2Window)
        {
            this.CS2Window = CS2Window;
        }
       
        public IntPtr GetWindowHandle()
        {
            return CS2Window;
        }

        public void ChangeWindowTitle(string Title)
        {
            Win32.SetWindowText(CS2Window, Title);
        }
        public void MoveCSWindow(int x, int y)
        {

            if (CS2Window == IntPtr.Zero) return;
            const uint SWP_NOSIZE = 0x0001;
            const uint SWP_NOZORDER = 0x0004;
            IntPtr HWND_TOP = new IntPtr(0);
            const uint SWP_DPIScaled = 0x2000;
            Win32.SetProcessDPIAware();
            Win32.SetWindowPos(CS2Window, HWND_TOP, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_DPIScaled);
        }
    }

}

using System.Windows.Forms;

namespace School_Management_System.Services
{
    internal sealed class SessionActivityMessageFilter : IMessageFilter
    {
        private const int WmMouseMove = 0x0200;
        private const int WmLButtonDown = 0x0201;
        private const int WmRButtonDown = 0x0204;
        private const int WmMButtonDown = 0x0207;
        private const int WmMouseWheel = 0x020A;
        private const int WmKeyDown = 0x0100;
        private const int WmSysKeyDown = 0x0104;

        public bool PreFilterMessage(ref Message m)
        {
            if (SessionContext.CurrentUser == null)
            {
                return false;
            }

            switch (m.Msg)
            {
                case WmMouseMove:
                case WmLButtonDown:
                case WmRButtonDown:
                case WmMButtonDown:
                case WmMouseWheel:
                case WmKeyDown:
                case WmSysKeyDown:
                    SessionContext.Touch();
                    break;
            }

            return false;
        }
    }
}

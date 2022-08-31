using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FerrumGateService.Helper
{
    class CurrentUser
    {

        /// <summary>
        /// check if current user is administrator
        /// </summary>
        /// <returns></returns>
        public static bool IsAdministrator()
        {
#if !DEBUG
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
#else 
            return true;
#endif
        }
    }
}

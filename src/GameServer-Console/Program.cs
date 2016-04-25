using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameServer;

namespace GameServer_Console
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var serviceMain = new ServiceMain();
            serviceMain.Run(args);
        }
    }
}

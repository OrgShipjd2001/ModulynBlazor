using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServerInterface
{
    public interface IWebModuleNavEntry
    {
        string NavItemPath { get; }
        string NavItemName { get; }
        string Target { get; }
        string Icon { get; }

    }
}

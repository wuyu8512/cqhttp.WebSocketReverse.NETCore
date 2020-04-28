using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cqhttp.WebSocketReverse.NETCore.Model
{
    public enum GroupRole : byte
    {
        Member = 0,
        Admin = 1,
        Owner = 255,
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cqhttp.WebSocketReverse.NETCore.Model
{
    public sealed class CqHttpSender
    {
        public long UserId { get; set; }
        public string NickName { get; set; }
        public string Card { get; set; }
        public string Title { get; set; }
        public int Age { get; set; }
        public string Area { get; set; }
        public string Level { get; set; }
        public InGroupSex Sex { get; set; }
        public GroupRole Role { get; set; }
    }
}

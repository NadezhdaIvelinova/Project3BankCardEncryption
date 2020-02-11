using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Server
{
    public class Users
    {
        [XmlElement("User", typeof(User))]
        public List<User> User { get; set; }

        public Users()
        {

        }
    }
}

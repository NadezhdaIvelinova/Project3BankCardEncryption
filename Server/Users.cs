using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Server
{
    //Class responsible for serialization/deserialization object from XML file from type User
    public class Users
    {
        [XmlElement("User", typeof(User))]
        public List<User> User { get; set; }

        public Users()
        {

        }
    }
}

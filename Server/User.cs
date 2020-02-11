using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    [Serializable()]
    public class User
    {
        #region Data members      
        public enum Permissions { ADMIN, USER, GUEST };      
        #endregion

        #region Properties
        [System.Xml.Serialization.XmlElement("Username")]
        public string Username { get; set; }

        [System.Xml.Serialization.XmlElement("Password")]
        public string Password { get; set; }

        [System.Xml.Serialization.XmlElement("Permissions")]
        public Permissions Permission { get; set; }

        [System.Xml.Serialization.XmlElement("Card")]
        public string Card { get; set; }

        #endregion

        #region Constructors
        public User(string username, string password, string permissions, string card)
        {
            Username = username;
            Password = password;
            if (permissions.Contains("Admin")) Permission = Permissions.ADMIN;
            else if(permissions.Contains("User")) Permission = Permissions.USER;
            else Permission = Permissions.GUEST;
            Card = card;

        }

        public User(string username, string password, string permissions) : this(username, password, permissions, null)
        {

        }
        
        public User()
        {

        }
        #endregion

        public override string ToString()
        {           
            return string.Format("User: " + Username + ", Password: " + Password + ", Permissions: " + Permission + " Card: " + Card + "\n");
        }
    }
}

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
        
        #endregion

        #region Constructors
        public User(string username, string password, string permissions)
        {
            Username = username;
            Password = password;
            if (permissions.Equals("System.Windows.Controls.ComboBoxItem: Admin")) Permission = Permissions.ADMIN;
            else if(permissions.Equals("System.Windows.Controls.ComboBoxItem: User")) Permission = Permissions.USER;
            else Permission = Permissions.GUEST;

        }

        
        public User()
        {

        }
        #endregion

        public override string ToString()
        {
            return string.Format("User: " + Username + ", Password: " + Password + ", Permissions: " + Permission + "\n");
        }
    }
}

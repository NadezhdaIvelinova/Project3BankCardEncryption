using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server
{
    public static class Validation
    {
       public static bool ValidateCredentials(string username, string password)
       {
            if (String.IsNullOrEmpty(username)) return false;
            if (String.IsNullOrEmpty(password)) return false;
            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$")) return false;
            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9]+$")) return false;
            return true;
       }
    }
}

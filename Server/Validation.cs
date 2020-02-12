using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Server
{
    //Class responsible for account and card validation
    public static class Validation
    {
        private static int AMERICANEXPRESS_LENGTH = 15; //American express card has 15 digits
        private static int LENGTH = 16; //Other cards have 16 digits

        #region Utility methods
        public static bool ValidateCredentials(string username, string password)
        {
            if (String.IsNullOrEmpty(username)) return false;
            if (String.IsNullOrEmpty(password)) return false;
            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_]+$")) return false;
            if (!Regex.IsMatch(password, @"^[a-zA-Z0-9]+$")) return false;
            return true;
        }

        public static bool ValidateCardNumber(string cardNumber)
        {
            if (!Regex.IsMatch(cardNumber, @"^[0-9]+$")) return false;
            else if (IsValidNumberOfDigits(cardNumber))
            {
                if (IsValidPrefix(cardNumber)) return IsLuhnValid(cardNumber);
                else return false;
            }
            else return false;
        }

        private static bool IsValidPrefix(string cardNumber)
        {
            int firstDigit = (cardNumber[0] - 48);
            if (firstDigit != 3 && firstDigit != 4 && firstDigit != 5 && firstDigit != 6) return false;
            else if (firstDigit == 3) //This is first digit of American Express Bank Card
            {
                int secondDigit = (cardNumber[1] - 48);
                return secondDigit == 4 || secondDigit == 7; //For valid American Express Bank Card second digit must be 4 or 7
            }
            else if (firstDigit == 5) //This is the first digit of mastercard {
            {
                int secondDigit = (cardNumber[1] - 48);
                return secondDigit >= 1 && secondDigit <= 5;
            }
            else return true;
        }

        private static bool IsValidNumberOfDigits(string cardNumber)
        {
            int firstDigit = (cardNumber[0] - 48);
            if (firstDigit == 3 && cardNumber.Length != AMERICANEXPRESS_LENGTH) return false;
            else if (firstDigit != 3 && cardNumber.Length != LENGTH) return false;
            else return true; ;
        }

        private static bool IsLuhnValid(string cardNumber)
        {
            int sum = 0;
            int counter = 2;
            int number = cardNumber[cardNumber.Length - 1] - 48;
            int secondDigit = 0;
            sum = sum + number;

            while (counter <= cardNumber.Length)
            {
                number = cardNumber[cardNumber.Length - counter] - 48;
                if ((cardNumber.Length - counter) % 2 == 0) //double digit if odd
                {
                    number = number * 2;
                    if (number > 9)
                    {
                        secondDigit = number % 10;
                        number = number / 10 + secondDigit;
                    }
                }
                sum = sum + number;
                counter++;
            }
            return sum % 10 == 0;
        } 
        #endregion
    }
}

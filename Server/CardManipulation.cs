using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    //Encrypt bank card using Route Cipher algorithm
    public class CardManipulation
    {
        private static int key;

        public CardManipulation(int keyForEncryption)
        {
            Key = keyForEncryption % 16;
        }

        public int Key 
        { 
            get
            {
                return key;
            }
            set
            {
                key = value;
            }
        }

        public string Encrypt(string plainText)
        { 
            char[] plainTextChars = plainText.ToCharArray();
            int numOfColumns = Math.Abs(Key);
            int numOfRows = plainTextChars.Length / Math.Abs(Key);

            if (plainTextChars.Length % Math.Abs(Key) != 0)
                numOfRows += 1;

            //Construct the grid
            char[,] grid = new char[numOfRows, numOfColumns];
            int counter = 0;

            for (int i = 0; i < numOfRows; i++)
            {
                for (int j = 0; j < numOfColumns; j++)
                {
                    if (counter < plainTextChars.Length)
                    {
                        grid[i, j] = plainTextChars[counter];
                    }
                    else
                    {
                        grid[i, j] = '0';
                    }
                    counter++;
                }
            }

            char[] cipherText = new char[numOfRows * numOfColumns];

            //Counterclockwise form the top left corner
            int startRowIndex = 0;
            int endRowIndex = numOfRows;
            int startColumnIndex = 0;
            int endColumnIndex = numOfColumns;
            int index;
            int cnt = 0;
            int total = endColumnIndex * endRowIndex;
            while (startRowIndex < endRowIndex && startColumnIndex < endColumnIndex)
            {
                if (cnt == total)
                    break;


                for (index = startRowIndex; index < endRowIndex; ++index)
                {
                    cipherText[cnt] = grid[index, startColumnIndex];
                    cnt++;
                }
                startColumnIndex++;

                if (cnt == total)
                    break;

                for (index = startColumnIndex; index < endColumnIndex; ++index)
                {
                    cipherText[cnt] = grid[endRowIndex - 1, index];
                    cnt++;
                }
                endRowIndex--;

                if (cnt == total)
                    break;


                if (startRowIndex < endRowIndex)
                {
                    for (index = endRowIndex - 1; index >= startRowIndex; --index)
                    {
                        cipherText[cnt] = grid[index, endColumnIndex - 1];
                        cnt++;
                    }
                    endColumnIndex--;
                }

                if (cnt == total)
                    break;

                if (startColumnIndex < endColumnIndex)
                {
                    for (index = endColumnIndex - 1; index >= startColumnIndex; --index)
                    {
                        cipherText[cnt] = grid[startRowIndex, index];
                        cnt++;
                    }
                    startRowIndex++;
                }
                
            }
            return new string(cipherText);

        }

        public string Decrypt(string cipherText, int realLength)
        {
            char[] cipherTextChars = cipherText.ToCharArray();
            int numberOfColumns = Math.Abs(Key);
            int numberOfRows = cipherText.Length / numberOfColumns;
            char[,] grid = new char[numberOfRows, numberOfColumns];

            //Counterclockwise from top left corner
            int startRowIndex = 0;
            int endRowIndex = numberOfRows;
            int startColumnIndex = 0;
            int endColumnIndex = numberOfColumns;
            int index;
            int cnt = 0;
            int total = endColumnIndex * endRowIndex;
            while (startRowIndex < endRowIndex && startColumnIndex < endColumnIndex)
            {
                if (cnt == total)
                    break;

                for (index = startRowIndex; index < endRowIndex; ++index)
                {
                    grid[index, startColumnIndex] = cipherTextChars[cnt];
                    cnt++;
                }
                startColumnIndex++;

                if (cnt == total)
                    break;

                for (index = startColumnIndex; index < endColumnIndex; ++index)
                {
                    grid[endRowIndex - 1, index] = cipherTextChars[cnt];
                    cnt++;
                }
                endRowIndex--;

                if (cnt == total)
                    break;

                if (startRowIndex < endRowIndex)
                {
                    for (index = endRowIndex - 1; index >= startRowIndex; --index)
                    {
                        grid[index, endColumnIndex - 1] = cipherTextChars[cnt];
                        cnt++;
                    }
                    endColumnIndex--;
                }

                if (cnt == total)
                    break;

                if (startColumnIndex < endColumnIndex)
                {
                    for (index = endColumnIndex - 1; index >= startColumnIndex; --index)
                    {
                        grid[startRowIndex, index] = cipherTextChars[cnt];
                        cnt++;
                    }
                    startRowIndex++;
                }              
            }
            char[] plaintext = new char[numberOfRows * numberOfColumns];
            int counter = 0;
            for (int i = 0; i < numberOfRows; i++)
            {
                for (int j = 0; j < numberOfColumns; j++)
                {
                    plaintext[counter] = grid[i, j];
                    counter++;
                }
            }
            string decrypted = new string(plaintext);
            return decrypted.Substring(0, realLength);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyllableWrap
{
    class WrapInWord
    {
        static List<char> group6 = new List<char>() {'ь', 'ъ', ':', ';', '!', '?', '.', ',', ' '};
        static List<char> group5 = new List<char>() {'-'};
        static List<char> group4 = new List<char>() {'а','е', 'ё', 'и', 'о', 'у', 'э', 'ю', 'я'};
        static List<char> group3 = new List<char>() {'л', 'м', 'н', 'р', 'й'};
        static List<char> group2 = new List<char>() {'б', 'в', 'г', 'д', 'з', 'ж'};
        static List<char> group1 = new List<char>() {'к', 'п', 'с', 'ф', 'т', 'ш', 'щ', 'х', 'ц', 'ч'};

        public static List<int> getPosWraps(string word)
        {
            List<byte> gWord = new List<byte>(); //слово записанное принадлежностью букв к группе например слово - "13424"
            List<int> posWraps = new List<int>(); //позиции где можно вставить перенос 0 - перед первым символом, 1 - после первого
            
            for (int i = 0; i < word.Length; i++)
            {
                if (group1.Contains(word[i])) gWord.Add(1);
                else if (group2.Contains(word[i])) gWord.Add(2);
                else if (group3.Contains(word[i])) gWord.Add(3);
                else if (group4.Contains(word[i])) gWord.Add(4);
                else if (group5.Contains(word[i])) gWord.Add(5);
                else /*if (group6.Contains(word[i])) */ gWord.Add(6);
            }

            if (gWord.Count == 0) return posWraps;
            if (gWord.Last() == 6) gWord.RemoveAt(gWord.Count-1);
            for (int i = 0; i < gWord.Count-1; i++)
            {
                int a = gWord[i];
                int b = gWord[i+1];
                //дальше идут правила расставления переносов
                if (a - b == 0 && b == 4 && (i > 0 && i < gWord.Count - 2)) //а и б гласные и перенос не отделит одну букву, то ставим между ними перенос
                {
                    posWraps.Add(i+1);
                }
                else
                {
                    if (b != 5 && b != 6 && (a - b > 0) && (i > 0 && i < gWord.Count - 2)) // если b не дефис и b не мягкий или твердый знак и наблюдается спад звучности то ставим перенос  
                    {
                        posWraps.Add(i+1);
                    }
                }
            }
            return posWraps;
        }
    }
}

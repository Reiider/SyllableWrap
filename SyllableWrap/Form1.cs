using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;


namespace SyllableWrap
{
    public partial class Form1 : Form
    {
        int posCaret = 0; //для отслеживания места вставок ctrl+v
        int lastTextSize = 0; //для отслеживания вставок ctrl+v и удалений
        List<int> endLineSymbol = new List<int>(); // список позиций установленных символов новой строки
        List<int> arrWrap = new List<int>(); // список позиций установленных переносов
        string mainText = ""; //текст без переносов
        string wrapText = ""; //текст с переносами

[DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        const int GWL_STYLE = -16;
        const int WS_HSCROLL = 0x00100000;
        const int WS_VSCROLL = 0x00200000;

        //проверка на наличие вертикальной прокрутки
        bool IsVertScrollPresent(Control control)
        {
            int style = GetWindowLong(control.Handle, GWL_STYLE);
            return (style & WS_VSCROLL) > 0;
        }

        public Form1()
        {
            InitializeComponent();
            mainText = "От земли поднялся туман. На его седом фоне неясно вырисовываются ближайшие сосны. " +
                "В их неподвижности чувствуется что-то суровое. Я не знаю, много ли проходит времени. Внезапно "+
                "мой слух поражается странными звуками так, что я невольно вздрагиваю от неожиданности. Что бы "+
                "это могло быть? Я никак не могу определить, что это за звуки. Они торопятся, будто вторя друг "+
                "другу, и лес немедленно откликается на них звонким и чистым отзвуком.";
            textBox.Text = mainText;
            lastTextSize = textBox.Text.Length;
            setWrap(textBox.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void setWrap(string text)
        {
            // нужно что бы весть текст был 1ого шрифта и размера
            string[] words = text.Split(' ');
            string outText = "";
            string lineOfText = "";
            string tempLineOfText = "";
            
            int wTextBox;
            if(IsVertScrollPresent((Control)textBox))
            {
                wTextBox = textBox.Width - 16;
            }
            else wTextBox = textBox.Width;
            //wTextBox = 350;
            
            bool nativeWrap = false;
            arrWrap.Clear();
            endLineSymbol.Clear();
            endLineSymbol.Add(-1); // для того что бы уменьшить число проверок
            for (int i = 0; i < words.Length; i++) // формируем текст с расставленными в нужных местах переносами
            {
                if (i + 1 == words.Length) tempLineOfText += words[i];
                else tempLineOfText += words[i] + " ";

                int w = TextRenderer.MeasureText(tempLineOfText, textBox.Font).Width; //измерение ширины текста записанного указанным шрифтом
                if (wTextBox >= w) lineOfText = tempLineOfText; //слово влазит в строку   
                else //слово не влазит в строку
                {
                    tempLineOfText = lineOfText;
                    List<int> pWW = WrapInWord.getPosWraps(words[i]); //posWrapWord
                    while (pWW.Count != 0)
                    {
                        string tempWord = "";
                        if (words[i][pWW.Last() - 1] == '-')
                        {
                            tempWord = words[i].Remove(pWW.Last()); //тут уже есть дефис
                            nativeWrap = true;
                        }
                        else tempWord = words[i].Remove(pWW.Last()) + "-";// +// слово обрезанное по перенос
                        tempLineOfText += tempWord;

                        w = TextRenderer.MeasureText(tempLineOfText, textBox.Font).Width;
                        if (wTextBox >= w) //если влазит
                        {
                            ////помечаем номер последнего символа строки

                            if (!nativeWrap) arrWrap.Add(endLineSymbol.Last() + tempLineOfText.Length); // если там не родной дефис, до помечаем его
                            nativeWrap = false; 
                            if (arrWrap.Count != 0 && arrWrap.Last() < posCaret) posCaret++; //если вводимое слово переноситься, то каретку сдвинуть вправо на 1
                            if (outText.Length < posCaret) posCaret++; //и так как появляется символ переноса строки, то еще увеличиваем
                            tempLineOfText += "\n"; //добавляем перенос строки
                            endLineSymbol.Add(endLineSymbol.Last() + tempLineOfText.Length); // помечаем перенос строки

                            outText += tempLineOfText;//сохраняем часть в резальтирующий вывод
                            tempLineOfText = words[i].Remove(0, pWW.Last()) + " "; //записываем оставшуюся часть слова на новую строку
                            lineOfText = tempLineOfText;
                            break; // выходим из while
                        }
                        else // если не влазит, удаляем последний перенос и пробуем еще раз
                        {
                            tempLineOfText = lineOfText;
                            pWW.RemoveAt(pWW.Count - 1);
                        }
                    }
                    if (lineOfText == "")
                    {
                        tempLineOfText = words[i];
                        tempLineOfText += "\n"; //добавляем перенос строки
                        if (outText.Length < posCaret) posCaret++; //и так как появляется символ переноса строки, то еще увеличиваем
                        endLineSymbol.Add(endLineSymbol.Last() + tempLineOfText.Length); // помечаем перенос строки
                        outText += tempLineOfText; //сохраняем часть в резальтирующий вывод
                        tempLineOfText = "";
                        lineOfText = tempLineOfText;
                        break; // выходим из while
                    }
                    else if (pWW.Count == 0) //если с переносами ничего не влазит или переносов нет, переносим всё слово на следующую строку
                    {
                        lineOfText += "\n";
                        if(outText.Length < posCaret) posCaret++; //и так как появляется символ переноса строки, то еще увеличиваем
                        outText += lineOfText;
                        endLineSymbol.Add(endLineSymbol.Last() + lineOfText.Length); // помечаем перенос строки
                        tempLineOfText = "";
                        lineOfText = "";
                        i--; //уменьшаем, что бы записать невлезающее слово на новую строку
                    }
                }
                lineOfText = tempLineOfText;
                if (i+1 == words.Length) outText += lineOfText;
            }

            textBox.TextChanged -= textBox_TextChanged;
            lastTextSize = outText.Length;
            textBox.Clear();
            textBox.AppendText(outText);
            wrapText = outText;
            textBox.TextChanged += textBox_TextChanged;
            
            //что добавил слово, сверил с шириной окна, если мало - добавил следующее слово
            //если много убрал кусочек до последнего переноса, если сново много, еще 1 перенос
            //если слово убралось целиком, значит даже с переносом не помещается и переходим к следующей строке

            //на новой строке учитывать кусочек перенесенный с предыдущей строки


        }

        private void textBox_TextChanged(object sender, EventArgs e)
        {
           // textBox.Text = textBox.Text + " hello"; //курсор в начало
            string text = textBox.Text;
            if (text == "") return;

            posCaret = textBox.SelectionStart; //позиция коретки

            if (lastTextSize - textBox.Text.Length < -1) //записано много символов за раз (ctrl+v)
            {
                int startNumSelectionText = wrapText.IndexOf(text.Remove(0, posCaret));
                if (posCaret == textBox.Text.Length)
                {
                    startNumSelectionText = lastTextSize;
                }

                int i = arrWrap.Count - 1;
                int j = endLineSymbol.Count - 1;
                while(i >= 0 || j > 0)
                {
                    //пока массивы не пусты удаляем по порядку
                    if (i >= 0 && j > 0  && arrWrap[i] < endLineSymbol[j]) // j > 0, т.к. в endLineSymbol нулевым элементом записан 0
                    {
                        if (startNumSelectionText < endLineSymbol[j])
                        {
                            text = text.Remove(endLineSymbol[j] + (posCaret - startNumSelectionText), 1);
                        }
                        else text = text.Remove(endLineSymbol[j], 1);
                        j--;
                    }
                    else if (i >= 0)
                    {
                        if (startNumSelectionText < arrWrap[i])
                        {
                            text = text.Remove(arrWrap[i] + (posCaret - startNumSelectionText), 1);
                        }
                        else text = text.Remove(arrWrap[i], 1);
                        i--;
                    }
                    else if (i < 0) //если arrWrap пуст, удаляем из endLineSymbol
                    {
                        if (startNumSelectionText < endLineSymbol[j])
                        {
                            text = text.Remove(endLineSymbol[j] + (posCaret - startNumSelectionText), 1);
                        }
                        else text = text.Remove(endLineSymbol[j], 1);
                        j--;
                    }
                }
                arrWrap.Clear();
                endLineSymbol.Clear();
                mainText = text;
                int tempPos = posCaret;
                setWrap(text);
                posCaret = tempPos;
            }
            else if (lastTextSize - textBox.Text.Length == -1) //записан 1 символ
            {
                bool lastLine = false;
                if (posCaret <= endLineSymbol.Last()) lastLine = true;
                int i = arrWrap.Count - 1;
                int j = endLineSymbol.Count - 1;
                while (i >= 0 || j >= 1)
                {
                    if (i >= 0 && j > 0 && arrWrap[i] < endLineSymbol[j]) // j > 0, т.к. в endLineSymbol нулевым элементом записан 0
                    {
                        if (posCaret-1 > endLineSymbol[j])
                        {
                            posCaret--;
                            text = text.Remove(endLineSymbol[j], 1);
                        }
                        else text = text.Remove(endLineSymbol[j] + 1, 1);
                        j--;
                    }
                    else if (i >= 0)
                    {
                        if (posCaret -1> arrWrap[i])
                        {
                            posCaret--;
                            text = text.Remove(arrWrap[i], 1);
                        }
                        else text = text.Remove(arrWrap[i] + 1, 1);
                        i--;
                    }
                    else if (i < 0) //если arrWrap пуст, удаляем из endLineSymbol
                    {
                        if (posCaret -1 > endLineSymbol[j])
                        {
                            posCaret--;
                            text = text.Remove(endLineSymbol[j], 1);
                        }
                        else text = text.Remove(endLineSymbol[j] + 1, 1);
                        j--;
                    }
                }
                if(lastLine) posCaret--;
                arrWrap.Clear();
                endLineSymbol.Clear();
                mainText = text;
                setWrap(text);
                
            }
            else if (lastTextSize - textBox.Text.Length == 1) //удален 1 символ
            {
                int i = arrWrap.Count - 1;
                int j = endLineSymbol.Count - 1;

                bool delNewLine = false; //если удалили перевод строки

                while (i >= 0 || j >= 1)
                {
                    if (i >= 0 && j > 0 && arrWrap[i] < endLineSymbol[j]) // j > 0, т.к. в endLineSymbol нулевым элементом записан 0
                    {
                        if (delNewLine)
                        {
                            delNewLine = false;
                            text = text.Remove(endLineSymbol[j + 1] - 1, 1);
                            posCaret--;
                        }
                        if (posCaret == endLineSymbol[j])
                        {
                            delNewLine = true;
                            //posCaret = posCaret - 1;
                        }
                        else if (posCaret > endLineSymbol[j])
                        {
                            posCaret--;
                            text = text.Remove(endLineSymbol[j], 1);
                        }
                        else text = text.Remove(endLineSymbol[j] - 1, 1);
                        j--;
                    }
                    else if (i >= 0)
                    {
                        if (posCaret > arrWrap[i])
                        {
                            posCaret--;
                            text = text.Remove(arrWrap[i], 1);
                        }
                        else text = text.Remove(arrWrap[i] - 1, 1);
                        if (delNewLine)
                        {
                            delNewLine = false;
                            text = text.Remove(arrWrap[i] - 1, 1);
                            posCaret--;
                        }
                        i--;
                    }
                    else if (i < 0) //если arrWrap пуст, удаляем из endLineSymbol
                    {
                        if (posCaret - 1 > endLineSymbol[j])
                        {
                            posCaret--;
                            text = text.Remove(endLineSymbol[j], 1);
                        }
                        else text = text.Remove(endLineSymbol[j] - 1, 1);
                        j--;
                    }
                }

                arrWrap.Clear();
                endLineSymbol.Clear();
                mainText = text;
                setWrap(text);
                posCaret--;
            }
            else if (lastTextSize - textBox.Text.Length > 1) //удалено много символов
            {
                int endNumSelectionText = 0; // поиск конца выделения
                if (posCaret == 0)
                {
                    endNumSelectionText = wrapText.LastIndexOf(text);
                }
                else if (posCaret == text.Length)
                {
                    endNumSelectionText = wrapText.Length-1;
                }
                else
                {
                    endNumSelectionText = wrapText.LastIndexOf(text.Remove(0, posCaret));
                }

                if (endNumSelectionText == -1) //поменялся весь текст
                {
                    arrWrap.Clear();
                    endLineSymbol.Clear();
                    mainText = text;
                    int tempPoss = posCaret;
                    setWrap(text);
                    posCaret = tempPoss;
                    return;
                }

                int i = arrWrap.Count - 1;
                int j = endLineSymbol.Count - 1;
                while (i >= 0 || j >= 1)
                {
                    if (i >= 0 && j > 0 && arrWrap[i] < endLineSymbol[j]) // j > 0, т.к. в endLineSymbol нулевым элементом записан 0
                    {
                        if (endNumSelectionText < endLineSymbol[j])
                        {
                            text = text.Remove(endLineSymbol[j] - (endNumSelectionText - posCaret), 1);
                        }
                        else if (endLineSymbol[j] - 1 > posCaret) { } 
                        else text = text.Remove(endLineSymbol[j], 1);
                        j--;
                    }
                    else if (i >= 0)
                    {
                        if (endNumSelectionText < arrWrap[i])
                        {
                            text = text.Remove(arrWrap[i] - (endNumSelectionText - posCaret), 1);
                        }
                        else if (arrWrap[i] - 1 > posCaret) { } 
                        else text = text.Remove(arrWrap[i], 1);
                        i--;
                    }
                    else if (i < 0) //если arrWrap пуст, удаляем из endLineSymbol
                    {
                        if (endNumSelectionText < endLineSymbol[j])
                        {
                            text = text.Remove(endLineSymbol[j] - (endNumSelectionText - posCaret), 1);
                        }
                        else if (endLineSymbol[j] - 1 > posCaret) { }
                        else text = text.Remove(endLineSymbol[j], 1);
                        j--;
                    }
                }

                arrWrap.Clear();
                endLineSymbol.Clear();
                mainText = text;
                int tempPos = posCaret;
                setWrap(text);
                posCaret = tempPos;
            }
            if (posCaret >= 0) textBox.SelectionStart = posCaret;
            else textBox.SelectionStart = 0;
        }

        private void textBox_Resize(object sender, EventArgs e)
        {
            if (textBox.Text.Length != wrapText.Length) return;
            string text = mainText;
            setWrap(text);
        }

        private void b_loadFromFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }
            mainText = File.ReadAllText(ofd.FileName, System.Text.Encoding.Default);
            textBox.Text = mainText;
        }

        private void b_saveToFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
            if (sfd.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }
            string text = textBox.Text;
            text = text.Replace("\n", "\r\n");
            File.WriteAllText(sfd.FileName, text, System.Text.Encoding.Default);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


    }
}

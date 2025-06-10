using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Diagnostics;

using System.Data;
using Application = System.Windows.Forms.Application;


namespace Compiler
{
    public partial class Form1 : Form
    {
        //Переменная для сохранения пути файла
        private string filePath = null;

        //переменная для смены языка
        int i = 1;

        //Ограничение стека textbox
        private const int MaxStackSize = 100;

        // Стек для хранения истории изменений
        private Stack<string> undoStack;

        // Стек для хранения отмененных действий (повторение)
        private Stack<string> redoStack;

        // Флаг для отслеживания изменений
        private bool isTextChanged = false;

        // Начальный размер шрифта
        private float currentFontSize = 12;

        private Dictionary<string, Color> keywords = new Dictionary<string, Color>
        {
            { "while", Color.Red },
            { "for", Color.Red },
            { "do", Color.Red },
            { "select", Color.Red },
            { "insert", Color.Red },
            { "int", Color.Blue },
            { "integer", Color.Blue },
            { "float", Color.Blue },
            { "char", Color.Blue },
            { "numeric", Color.Blue },
            { "const", Color.Blue },
            { "constant", Color.Blue },
            { "declare", Color.Blue },
        };

        private Dictionary<string, int> keyword = new Dictionary<string, int>
        {
            {"DECLARE", 1},
            {"CONSTANT", 2},
            {"NUMERIC", 3}
        };

        // Состояния конечного автомата
        private enum ParserState
        {
            Start,
            AfterDeclare,
            AfterIdentifier,
            AfterConstant,
            AfterType,
            AfterAssign,
            AfterValue,
            Error
        }

        private int lineNumberOffset = 1; // Отступ для номеров строк

        public Form1()
        {
            InitializeComponent();
            Tool_tips();
            UpdateTextBoxFont(); // Устанавливаем начальный шрифт
            //AddNewTab(); // Добавляем начальную вкладкуAddNewTab(); // Добавляем начальную вкладку
            undoStack = new Stack<string>();//стек для отмены
            redoStack = new Stack<string>();//стек для повторения
            openFileDialog1.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
            saveFileDialog1.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
            // Разрешаем перетаскивание на форму
            richTextBox1.AllowDrop = true;
            richTextBox1.DragEnter += MainForm_DragEnter;
            richTextBox1.DragDrop += MainForm_DragDrop;
            richTextBox1.TextChanged += RichTextBox_TextChanged;
            richTextBox1.KeyDown += RichTextBox_KeyDown; // Подписываемся на событие нажатия клавиш
            richTextBox1.VScroll += RichTextBox_VScroll;
            //richTextBox1.Resize += RichTextBox_Resize;
        }
        private void RichTextBox_TextChanged(object sender, EventArgs e)
        {

            // Обновляем панель с номерами строк при изменении текста
            lineNumberPanel.Invalidate();
            // Сохраняем текущую позицию курсора и выделение
            int originalPosition = richTextBox1.SelectionStart;
            int originalLength = richTextBox1.SelectionLength;
            Color originalColor = richTextBox1.SelectionColor;

            // Отключаем обновление RichTextBox для предотвращения мерцания
            richTextBox1.SuspendLayout();

            // Сбрасываем цвет всего текста на стандартный
            richTextBox1.SelectAll();
            richTextBox1.SelectionColor = richTextBox1.ForeColor;

            // Проходим по каждому ключевому слову
            foreach (var keyword in keywords)
            {
                int startIndex = 0;
                while (startIndex < richTextBox1.TextLength)
                {
                    // Ищем ключевое слово без учета регистра
                    int wordStartIndex = richTextBox1.Find(keyword.Key, startIndex, RichTextBoxFinds.WholeWord | RichTextBoxFinds.None);
                    if (wordStartIndex == -1) break;

                    // Выделяем найденное слово
                    richTextBox1.SelectionStart = wordStartIndex;
                    richTextBox1.SelectionLength = keyword.Key.Length;

                    // Меняем цвет выделенного текста на указанный в словаре
                    richTextBox1.SelectionColor = keyword.Value;

                    // Сдвигаем стартовый индекс для поиска следующего вхождения
                    startIndex = wordStartIndex + keyword.Key.Length;
                }
            }

            // Восстанавливаем позицию курсора и выделение
            richTextBox1.SelectionStart = originalPosition;
            richTextBox1.SelectionLength = originalLength;
            richTextBox1.SelectionColor = originalColor;

            // Возобновляем обновление RichTextBox
            richTextBox1.ResumeLayout();
        }
        private void RichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Если нажата клавиша Enter
            if (e.KeyCode == Keys.Enter)
            {
                // Принудительно обновляем панель с номерами строк
                lineNumberPanel.Invalidate();
            }
            // Проверяем, нажата ли комбинация Ctrl + C (копирование)
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyText();
                e.SuppressKeyPress = true; // Предотвращаем стандартное поведение
            }

            // Проверяем, нажата ли комбинация Ctrl + V (вставка)
            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteText();
                e.SuppressKeyPress = true; // Предотвращаем стандартное поведение
            }

            // Проверяем, нажата ли комбинация Ctrl + V (вставка)
            if (e.Control && e.KeyCode == Keys.F)
            {
                SelectAll();
                e.SuppressKeyPress = true; // Предотвращаем стандартное поведение
            }
        }
        private void RichTextBox_VScroll(object sender, EventArgs e)
        {
            // Обновляем панель с номерами строк при прокрутке
            lineNumberPanel.Invalidate();
        }

        private void RichTextBox_Resize(object sender, EventArgs e)
        {
            // Обновляем панель с номерами строк при изменении размера RichTextBox
            lineNumberPanel.Invalidate();
        }

        private void LineNumberPanel_Paint(object sender, PaintEventArgs e)
        {
            // Получаем графический контекст для отрисовки
            Graphics g = e.Graphics;
            g.Clear(lineNumberPanel.BackColor);

            // Получаем текущий шрифт и цвет для номеров строк
            using (Font font = new Font(richTextBox1.Font.FontFamily, richTextBox1.Font.Size))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                // Получаем первую видимую строку
                int firstVisibleLine = richTextBox1.GetCharIndexFromPosition(new Point(0, 0));
                firstVisibleLine = richTextBox1.GetLineFromCharIndex(firstVisibleLine);

                // Получаем общее количество строк в RichTextBox
                int totalLines = richTextBox1.GetLineFromCharIndex(richTextBox1.TextLength) + 1;

                // Получаем количество видимых строк
                int visibleLineCount = (int)Math.Ceiling((double)richTextBox1.Height / richTextBox1.Font.Height);

                // Ограничиваем количество отображаемых строк до общего количества строк
                if (firstVisibleLine + visibleLineCount > totalLines)
                {
                    visibleLineCount = totalLines - firstVisibleLine;
                }

                // Отрисовываем номера строк только для задействованных строк
                for (int i = 0; i < visibleLineCount; i++)
                {
                    int lineNumber = firstVisibleLine + i + 1; // Нумерация с 1
                    string lineNumberText = lineNumber.ToString();
                    float y = i * richTextBox1.Font.Height;
                    g.DrawString(lineNumberText, font, brush, lineNumberOffset, y);
                }
            }
        }

        private void CopyText()
        {
            if (richTextBox1.SelectionLength > 0)
            {
                // Копируем выделенный текст в буфер обмена
                Clipboard.SetText(richTextBox1.SelectedText);
            }
        }

        private void PasteText()
        {
            if (Clipboard.ContainsText())
            {
                // Вставляем текст из буфера обмена
                richTextBox1.Paste();
            }
        }

        private void SelectAll()
        {
            richTextBox1.SelectAll();
        }

        // Метод для добавления новой вкладки
        //private void AddNewTab()
        //{
        //    // Создаем новую вкладку
        //    TabPage newTabPage = new TabPage();
        //    newTabPage.Text = "Новый текст"; // Название вкладки

        //    // Добавляем TextBox на вкладку
        //    RichTextBox textBox = new RichTextBox();
        //    textBox.Multiline = true;
        //    textBox.Dock = DockStyle.Fill;
        //    textBox.ScrollBars = ScrollBars.Vertical;
        //    newTabPage.Controls.Add(textBox);

        //    // Добавляем вкладку в TabControl
        //    tabControl.TabPages.Add(newTabPage);
        //    tabControl.SelectedTab = newTabPage; // Переключаемся на новую вкладку
        //}

        // Метод для закрытия текущей вкладки
        //private void CloseCurrentTab()
        //{
        //    if (tabControl.TabPages.Count > 0)
        //    {
        //        TabPage currentTab = tabControl.SelectedTab;

        //        // Проверяем, есть ли несохраненные изменения
        //        TextBox textBox = (TextBox)currentTab.Controls[0];
        //        if (!string.IsNullOrEmpty(textBox.Text))
        //        {
        //            DialogResult result = MessageBox.Show("Сохранить изменения перед закрытием?", "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        //            if (result == DialogResult.Yes)
        //            {
        //                SaveTabContent(currentTab);
        //            }
        //            else if (result == DialogResult.Cancel)
        //            {
        //                return; // Отменяем закрытие вкладки
        //            }
        //        }

        //        // Удаляем вкладку
        //        tabControl.TabPages.Remove(currentTab);
        //    }
        //}

        private void Tool_tips()
        {
            toolTip1.SetToolTip(this.Create, "Создать");
            toolTip1.SetToolTip(this.Open, "Открыть");
            toolTip1.SetToolTip(this.Save, "Сохранить");
            toolTip1.SetToolTip(this.Cansel, "Отменить");
            toolTip1.SetToolTip(this.Repeat, "Повторить");
            toolTip1.SetToolTip(this.Copy, "Копировать");
            toolTip1.SetToolTip(this.Cut, "Вырезать");
            toolTip1.SetToolTip(this.Paste, "Вставить");
            toolTip1.SetToolTip(this.Start, "Пуск");
            toolTip1.SetToolTip(this.Help, "Справка");
            toolTip1.SetToolTip(this.About, "О программе");
            toolTip1.SetToolTip(this.About, "О программе");
        }

        private void диагностикаИНейтрализацияToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolTip1_Popup(object sender, PopupEventArgs e)
        {

        }

        private void Open_file_button(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            CheckSaveChanges();
            // получаем выбранный файл
            string filename = openFileDialog1.FileName;
            // читаем файл в строку
            string fileText = System.IO.File.ReadAllText(filename);
            richTextBox1.Text = fileText;
            MessageBox.Show("Файл открыт");
            dataGridView1.Rows[0].Cells[0].Value = filename;
        }

        private void Save_as_button(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            // получаем выбранный файл
            string filename = saveFileDialog1.FileName;
            // сохраняем текст в файл
            System.IO.File.WriteAllText(filename, richTextBox1.Text);
            MessageBox.Show("Файл сохранен");
            dataGridView1.Rows[0].Cells[0].Value = filePath;
        }

        // Метод для проверки необходимости сохранения изменений
        private void CheckSaveChanges()
        {
            if (isTextChanged)
            {
                // Показываем диалоговое окно с предложением сохранить изменения
                DialogResult result = MessageBox.Show("Сохранить изменения в файле?", "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Save_button(null, null); // Сохраняем файл
                }
            }
        }

        private void Exit_button(object sender, EventArgs e)
        {

            Application.Exit();
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Language_Click(object sender, EventArgs e)
        {
            if (i == 1) { i = 0; }
            else i = 1;
            switch (i)
            {
                case 0:
                    файлToolStripMenuItem.Text = "File";
                    создатьToolStripMenuItem.Text = "Create";
                    открытьToolStripMenuItem.Text = "Open";
                    сохранитьToolStripMenuItem.Text = "Save";
                    сохранитьКакToolStripMenuItem.Text = "Save as";
                    выходToolStripMenuItem.Text = "Exit";
                    правкаToolStripMenuItem.Text = "Edit";
                    отменитьToolStripMenuItem.Text = "Undo";
                    повторитьToolStripMenuItem.Text = "Redo";
                    вырезатьToolStripMenuItem.Text = "Cut";
                    копироватьToolStripMenuItem.Text = "Copy";
                    вставитьToolStripMenuItem.Text = "Paste";
                    удалитьToolStripMenuItem.Text = "Delete";
                    выделитьВсеToolStripMenuItem.Text = "Select all";

                    пускToolStripMenuItem.Text = "Run";
                    справкаToolStripMenuItem.Text = "Help";
                    вызовСправкиToolStripMenuItem.Text = "Help";
                    оПрограммеToolStripMenuItem.Text = "About programm";

                    break;

                case 1:
                    файлToolStripMenuItem.Text = "Файл";
                    создатьToolStripMenuItem.Text = "Создать";
                    открытьToolStripMenuItem.Text = "Открыть";
                    сохранитьToolStripMenuItem.Text = "Сохранить";
                    сохранитьКакToolStripMenuItem.Text = "Сохранить как";
                    выходToolStripMenuItem.Text = "Выход";
                    правкаToolStripMenuItem.Text = "Правка";
                    отменитьToolStripMenuItem.Text = "Отменить";
                    повторитьToolStripMenuItem.Text = "Повторить";
                    вырезатьToolStripMenuItem.Text = "Вырезать";
                    копироватьToolStripMenuItem.Text = "Копировать";
                    вставитьToolStripMenuItem.Text = "Вставить";
                    удалитьToolStripMenuItem.Text = "Удалить";
                    выделитьВсеToolStripMenuItem.Text = "Выделить все";

                    пускToolStripMenuItem.Text = "Пуск";
                    справкаToolStripMenuItem.Text = "Справка";
                    вызовСправкиToolStripMenuItem.Text = "Вызов справки";
                    оПрограммеToolStripMenuItem.Text = "О программе";

                    break;
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void классификацияГрамматикиToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Help_button(object sender, EventArgs e)
        {
            string helpText = "В первой лабораторной работе необходимо, " +
                "чтобы работали все функции из меню «Файл», «Правка» и «Справка»." +
                "\r\n\r\nВыполнены 3 доп.Задания: Изменение размеров текста в окне редактирования и окне вывода результатов." +
                "\r\n\r\nВыбор языка интерфейса приложения (интернационализация)." +
                "\r\n\r\nОткрытие файла при перетаскивании иконки в окно программы.";
            MessageBox.Show(helpText);
        }

        private void Save_button(object sender, EventArgs e)
        {
            // Проверяем, создан ли файл
            if (string.IsNullOrEmpty(filePath))
            {
                DialogResult result = MessageBox.Show("Файл еще не создан. Сначала создайте файл.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                    Create_button(null, null);
                return;
            }

            // Получаем текст из TextBox
            string textToSave = richTextBox1.Text;

            try
            {
                // Сохраняем текст в файл
                File.WriteAllText(filePath, textToSave);

                // Сообщаем пользователю об успешном сохранении
                MessageBox.Show($"Текст успешно сохранен в файл: {filePath}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Обрабатываем ошибки
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            dataGridView1.Rows[0].Cells[0].Value = filePath;
        }


        private void Create_button(object sender, EventArgs e)
        {
            // Используем SaveFileDialog для выбора места сохранения файла
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Создать файл";
            saveFileDialog.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Сохраняем путь к файлу
                filePath = saveFileDialog.FileName;

                // Создаем пустой файл
                File.WriteAllText(filePath, string.Empty);

                // Сообщаем пользователю об успешном создании файла
                MessageBox.Show($"Файл успешно создан: {filePath}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            dataGridView1.Rows[0].Cells[0].Value = filePath;
        }

        private void richTextBox1_TextChanged_1(object sender, EventArgs e)
        {
            isTextChanged = true; // Устанавливаем флаг изменений

            if (undoStack.Count >= MaxStackSize)
            {
                // Удаляем самое старое состояние
                var tempStack = new Stack<string>(undoStack.Reverse().Skip(1).Reverse());
                undoStack = tempStack;
            }

            // Сохраняем текущий текст в стек
            undoStack.Push(richTextBox1.Text);
        }


        //Функция отмены
        private void Cansel_button(object sender, EventArgs e)
        {
            if (undoStack.Count > 1)
            {
                // Отключаем обработчик TextChanged
                richTextBox1.TextChanged -= richTextBox1_TextChanged_1;

                // Перемещаем текущее состояние в стек повторения
                redoStack.Push(undoStack.Pop());

                // Убираем текущее состояние из стека
                undoStack.Pop();

                // Восстанавливаем предыдущее состояние
                richTextBox1.Text = undoStack.Peek();

                // Включаем обработчик TextChanged
                richTextBox1.TextChanged += richTextBox1_TextChanged_1;
            }
            else
            {
                MessageBox.Show("Нечего отменять.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //Повтор действия
        private void Redo(object sender, EventArgs e)
        {
            if (redoStack.Count > 0) // Проверяем, есть ли что повторять
            {
                // Восстанавливаем состояние из стека повторения
                richTextBox1.Text = redoStack.Peek();

                // Перемещаем состояние обратно в стек отмены
                undoStack.Push(redoStack.Pop());

            }
            else
            {
                MessageBox.Show("Нечего повторять.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Обработчик кнопки "Вырезать"
        private void Cut_button(object sender, EventArgs e)
        {

            // Проверяем, есть ли выделенный текст
            if (richTextBox1.SelectionLength > 0)
            {
                // Вырезаем выделенный текст
                richTextBox1.Cut();
            }
            else
            {
                MessageBox.Show("Выделите текст для вырезания.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        //Копирование
        private void Copy_button(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionLength > 0)
            {
                richTextBox1.Copy();
            }
            else
            {
                MessageBox.Show("Выделите текст для копирования.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //Вставка
        private void Paste_button(object sender, EventArgs e)
        {
            richTextBox1.Paste();
        }

        //Кнопка удаления всего текста
        private void Delete_button(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        //Кнопка выделения всего текста
        private void Select_all_button(object sender, EventArgs e)
        {
            richTextBox1.SelectAll();
        }

        //Изменение шрифта
        private void UpdateTextBoxFont()
        {
            richTextBox1.Font = new Font(richTextBox1.Font.FontFamily, currentFontSize);
            FontSize.Text = $"Размер шрифта: {currentFontSize}"; // Обновляем Label
        }


        private void SizeUP_button(object sender, EventArgs e)
        {
            currentFontSize += 2; // Увеличиваем размер шрифта на 2 пункта
            UpdateTextBoxFont();
        }

        private void SizeDown_button(object sender, EventArgs e)
        {
            if (currentFontSize > 6) // Минимальный размер шрифта
            {
                currentFontSize -= 2; // Уменьшаем размер шрифта на 2 пункта
                UpdateTextBoxFont();
            }
            else
            {
                MessageBox.Show("Минимальный размер текста достигнут.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        // Обработчик события DragEnter
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            // Проверяем, что перетаскиваемый объект является файлом
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // Разрешаем копирование
            }
            else
            {
                e.Effect = DragDropEffects.None; // Запрещаем перетаскивание
            }
        }

        // Обработчик события DragDrop
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            // Получаем массив перетаскиваемых файлов
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Проверяем, что перетащен хотя бы один файл
            if (files != null && files.Length > 0)
            {
                string filePath = files[0]; // Берем первый файл

                // Проверяем, что файл имеет расширение .txt
                if (Path.GetExtension(filePath).ToLower() == ".txt")
                {
                    try
                    {
                        // Читаем содержимое файла и отображаем его в RichTextBox
                        richTextBox1.Text = File.ReadAllText(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Поддерживаются только текстовые файлы (.txt).", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void About_button(object sender, EventArgs e)
        {
            string helpText = "Выполнена 1 лабораторная работа";
            MessageBox.Show(helpText);
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void InitializeDataGridView()
        {
            // Настройка столбцов DataGridView
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Type", "Тип");
            dataGridView1.Columns.Add("Value", "Значение");
            dataGridView1.Columns.Add("Position", "Позиция");

            // Настройка внешнего вида
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.BackgroundColor = Color.White;
        }

        
        private DataTable tokenTable;

        public void LexicalAnalyzer()
        {
            // Инициализация таблицы для вывода результатов
            tokenTable = new DataTable();
            dataGridView1.Columns.Clear();
            tokenTable.Columns.Clear();
            tokenTable.Columns.Add("Лексема", typeof(string));
            tokenTable.Columns.Add("Тип", typeof(string));
            tokenTable.Columns.Add("Позиция", typeof(int));
        }
        public void Analyze(RichTextBox richTextBox, DataGridView dataGridView)
        {
            tokenTable.Rows.Clear(); // Очищаем таблицу перед новым анализом

            string input = richTextBox.Text;
            int position = 0;
            bool hasErrors = false;

            // Регулярные выражения для лексем
            var tokenPatterns = new Dictionary<string, string>
        {
            {"ID", @"^[a-zA-Z][a-zA-Z0-9]*"},       // Идентификаторы (латинские буквы и цифры)
            {"POW", @"^\^"},                        // Оператор степени
            {"MULT", @"^[\*/]"},                   // Операторы умножения/деления
            {"PLUS", @"^\+"},                       // Оператор сложения
            {"DOT", @"^\."},                        // Точка
            {"WHITESPACE", @"^\s+"},                // Пробельные символы
            {"RUSSIAN", @"^[а-яА-ЯёЁ]"},           // Русские буквы
            {"INVALID", @"^[^\s\w\^\*\/\+\.]"}     // Недопустимые символы (все кроме разрешенных)
        };

            while (position < input.Length)
            {
                bool matched = false;

                // Сначала проверяем на ошибки (русские буквы и недопустимые символы)
                var errorMatch = Regex.Match(input.Substring(position), tokenPatterns["RUSSIAN"]);
                if (errorMatch.Success && errorMatch.Index == 0)
                {
                    string errorChar = errorMatch.Value;
                    tokenTable.Rows.Add(errorChar, "Русские буквы недопустимы", position);
                    hasErrors = true;
                    position += errorMatch.Length;
                    continue;
                }

                errorMatch = Regex.Match(input.Substring(position), tokenPatterns["INVALID"]);
                if (errorMatch.Success && errorMatch.Index == 0)
                {
                    string errorChar = errorMatch.Value;
                    tokenTable.Rows.Add(errorChar, "Недопустимый спецсимвол", position);
                    hasErrors = true;
                    position += errorMatch.Length;
                    continue;
                }

                // Затем проверяем допустимые токены
                foreach (var pattern in tokenPatterns)
                {
                    // Пропускаем шаблоны для ошибок и пробелов
                    if (pattern.Key == "RUSSIAN" || pattern.Key == "INVALID" || pattern.Key == "WHITESPACE")
                        continue;

                    var match = Regex.Match(input.Substring(position), pattern.Value);

                    if (match.Success && match.Index == 0)
                    {
                        string value = match.Value;
                        string tokenType = GetTokenType(pattern.Key, value);

                        tokenTable.Rows.Add(value, tokenType, position);

                        position += match.Length;
                        matched = true;
                        break;
                    }
                }

                // Пропускаем пробелы
                var spaceMatch = Regex.Match(input.Substring(position), tokenPatterns["WHITESPACE"]);
                if (spaceMatch.Success && spaceMatch.Index == 0)
                {
                    position += spaceMatch.Length;
                    matched = true;
                }

                if (!matched && position < input.Length)
                {
                    // Если ничего не совпало, но есть еще символы - это ошибка
                    string errorChar = input[position].ToString();
                    tokenTable.Rows.Add(errorChar, position, "Неизвестная лексема");
                    hasErrors = true;
                    position++;
                }
            }

            // Выводим результаты в DataGridView
            dataGridView.DataSource = tokenTable;

            if (hasErrors)
            {
                MessageBox.Show("Обнаружены лексические ошибки!", "Ошибка",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                MessageBox.Show("Лексический анализ завершен успешно!", "Успех",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string GetTokenType(string patternKey, string value)
        {
            switch (patternKey)
            {
                case "ID": return "Идентификатор";
                case "POW": return "Оператор";
                case "MULT":
                    return value == "*" ? "Оператор" : "Оператор";
                case "PLUS": return "Оператор";
                case "MINUS": return "Оператор";
                case "DOT": return "Точка";
                default: return "Неизвестный токен";
            }
        }
        private void Start_Click(object sender, EventArgs e)
        {
            //dataGridView1.Rows.Clear();
            //var parser = new ExpressionParser(richTextBox1.Text, dataGridView1);
            //bool isValid = parser.Parse();
            //MessageBox.Show(isValid ? "Разбор успешен!" : "Ошибка разбора!");
            LexicalAnalyzer();
            Analyze(richTextBox1, dataGridView1);
        }
        public class ExpressionParser
        {
            private string input;
            private int position;
            private StringBuilder parseSequence;
            private DataGridView dataGridView;

            public ExpressionParser(string input, DataGridView dataGridView)
            {
                this.input = input.Replace(" ", "");
                this.position = 0;
                this.parseSequence = new StringBuilder();
                this.dataGridView = dataGridView;
            }

            private char CurrentChar => position < input.Length ? input[position] : '\0';
            private void Advance() => position++;
            private void AddToSequence(string rule) => parseSequence.Append(rule + "-");
            private bool IsLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            private bool IsDigit(char c) => c >= '0' && c <= '9';

            public bool Parse()
            {
                bool result = Expression();
                if (result && position == input.Length)
                {
                    dataGridView.Rows.Add("Полная последовательность:");
                    dataGridView.Rows.Add(parseSequence.ToString().TrimEnd('-'));
                    return true;
                }
                dataGridView.Rows.Add($"Ошибка на позиции {position}");
                return false;
            }

            private bool Expression()
            {
                AddToSequence("expression");
                if (!MultExpression()) return false;
                return ExpressionTail();
            }

            private bool ExpressionTail()
            {
                while (CurrentChar == '+' || CurrentChar == '-')
                {
                    char op = CurrentChar;
                    Advance();
                    AddToSequence(op.ToString());

                    if (!MultExpression()) return false;
                }
                return true;
            }

            private bool MultExpression()
            {

                if (!PowExpression()) return false;
                
                return MultExpressionTail();
            }

            private bool MultExpressionTail()
            {
                AddToSequence("multExpression");
                while (CurrentChar == '*' || CurrentChar == '/')
                {
                    char op = CurrentChar;
                    Advance();
                    AddToSequence(op.ToString());

                    if (!PowExpression()) return false;
                }
                return true;
            }

            private bool PowExpression()
            {
                AddToSequence("powExpression");
                if (!ID()) return false;

                if (CurrentChar == '^')
                {
                    Advance();
                    AddToSequence("^");
                    if (!PowExpression()) return false;
                }
                return true;
            }

            private bool ID()
            {
                if (!IsLetter(CurrentChar)) return false;

                AddToSequence("ID");
                Advance();

                while (IsLetter(CurrentChar) || IsDigit(CurrentChar))
                    Advance();

                return true;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // Очистка предыдущих результатов
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("String", "Подстрока");
            // Получение текста из RichTextBox
            string text = richTextBox1.Text;

            // Регулярное выражение для целых чисел и чисел с плавающей точкой
            string pattern = @"-?\d+(?:,\d+)?";
            Regex regex = new Regex(pattern);

            // Поиск всех совпадений
            MatchCollection matches = regex.Matches(text);

            // Добавление результатов в DataGridView
            foreach (Match match in matches)
            {
                int startIndex = match.Index;
                string value = match.Value;
                dataGridView1.Rows.Add(value, startIndex);
            }

            // Сброс предыдущего выделения в RichTextBox
            richTextBox1.SelectAll();
            richTextBox1.SelectionBackColor = richTextBox1.BackColor;
            richTextBox1.DeselectAll();

            // Выделение найденных подстрок
            foreach (Match match in matches)
            {
                richTextBox1.Select(match.Index, match.Length);
                richTextBox1.SelectionColor = Color.Blue;
            }

            // Снятие выделения текста
            richTextBox1.SelectionStart = 0;
            richTextBox1.SelectionLength = 0;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Очистка предыдущих результатов
            dataGridView1.Rows.Clear();

            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Type", "Тип");
            dataGridView1.Columns.Add("Value", "Значение");
            dataGridView1.Columns.Add("Position", "Позиция");
            SearchUsernames();
        }
        private void ResetSearch()
        {
            // Очистка результатов и выделения
            dataGridView1.Rows.Clear();
            richTextBox1.SelectAll();
            richTextBox1.SelectionColor = richTextBox1.ForeColor;
            richTextBox1.DeselectAll();
        }
        private void SearchUsernames()
        {
            ResetSearch();
            FindPattern(
                @"\b[a-z0-9_-]{5,20}\b",
                "Юзернейм",
                Color.Orange
            );
        }
        private void FindPattern(string pattern, string typeName, Color color)
        {
            var regex = new Regex(pattern);
            var matches = regex.Matches(richTextBox1.Text);

            foreach (Match match in matches)
            {
                // Добавление в таблицу
                dataGridView1.Rows.Add(
                    typeName,
                    match.Value,
                    match.Index
                );

                // Выделение в тексте
                richTextBox1.SelectionStart = match.Index;
                richTextBox1.SelectionLength = match.Length;
                richTextBox1.SelectionColor = color;
            }
        }

        // Переменные для автомата широты
        private enum LatitudeState { Start, Sign, Integer, Ninety, Comma, Fraction }
        private LatitudeState currentLatState;
        private int currentPosition;
        private int matchStartIndex;
        private bool isNegative;
        private readonly StringBuilder latitudeBuffer = new StringBuilder();
        private readonly List<Tuple<string, int>> latitudeMatches = new List<Tuple<string, int>>();

        private void button3_Click(object sender, EventArgs e)
        {
            // Сброс предыдущих результатов
            dataGridView1.Rows.Clear();
            richTextBox1.SelectAll();
            richTextBox1.SelectionColor = Color.Black;

            // Инициализация автомата
            currentLatState = LatitudeState.Start;
            currentPosition = 0;
            matchStartIndex = 0;
            isNegative = false;
            latitudeBuffer.Clear();
            latitudeMatches.Clear();

            // Обработка текста
            ProcessLatitude();

            // Вывод результатов
            foreach (var match in latitudeMatches)
            {
                dataGridView1.Rows.Add("Широта", match.Item1, match.Item2);
                HighlightMatch(match.Item2, match.Item1.Length, Color.Red);
            }
        }

        private void ProcessLatitude()
        {
            string text = richTextBox1.Text;

            for (currentPosition = 0; currentPosition < text.Length; currentPosition++)
            {
                char c = text[currentPosition];

                switch (currentLatState)
                {
                    case LatitudeState.Start:
                        if (c == '-')
                        {
                            isNegative = true;
                            currentLatState = LatitudeState.Sign;
                            matchStartIndex = currentPosition;
                            latitudeBuffer.Append(c);
                        }
                        else if (c == '9')
                        {
                            currentLatState = LatitudeState.Ninety;
                            matchStartIndex = currentPosition;
                            latitudeBuffer.Append(c);
                        }
                        else if (char.IsDigit(c) && c != '0')
                        {
                            currentLatState = LatitudeState.Integer;
                            matchStartIndex = currentPosition;
                            latitudeBuffer.Append(c);
                        }
                        else if (c == '0')
                        {
                            matchStartIndex = currentPosition;
                            latitudeBuffer.Append(c);
                            CommitLatitudeMatch();
                        }
                        break;

                    case LatitudeState.Sign:
                        if (c == '9')
                        {
                            currentLatState = LatitudeState.Ninety;
                            latitudeBuffer.Append(c);
                        }
                        else if (char.IsDigit(c) && c != '0')
                        {
                            currentLatState = LatitudeState.Integer;
                            latitudeBuffer.Append(c);
                        }
                        else
                        {
                            ResetLatitudeState();
                        }
                        break;

                    case LatitudeState.Integer:
                        if (char.IsDigit(c))
                        {
                            latitudeBuffer.Append(c);
                            var numPart = latitudeBuffer.ToString().Replace("-", "");
                            if (numPart.Length > 2 || int.Parse(numPart) > 89)
                                ResetLatitudeState();
                        }
                        else if (c == ',')
                        {
                            currentLatState = LatitudeState.Comma;
                            latitudeBuffer.Append(c);
                        }
                        else
                        {
                            CommitLatitudeMatch();
                            ResetLatitudeState();
                        }
                        break;

                    case LatitudeState.Ninety:
                        if (c == '0')
                        {
                            latitudeBuffer.Append(c);
                            currentLatState = LatitudeState.Comma;
                        }
                        else
                        {
                            ResetLatitudeState();
                        }
                        break;

                    case LatitudeState.Comma:
                        if (char.IsDigit(c))
                        {
                            currentLatState = LatitudeState.Fraction;
                            latitudeBuffer.Append(c);
                        }
                        else
                        {
                            ResetLatitudeState();
                        }
                        break;

                    case LatitudeState.Fraction:
                        if (char.IsDigit(c))
                        {
                            latitudeBuffer.Append(c);
                        }
                        else
                        {
                            CommitLatitudeMatch();
                            ResetLatitudeState();

                            // Обработка текущего символа как нового числа
                            currentPosition--; // Повторная обработка символа
                        }
                        break;
                }
            }
            CommitLatitudeMatch(); // Проверка последнего буфера
        }


        private bool IsValidLatitude(string value)
        {
            var culture = new CultureInfo("ru-RU");
            return double.TryParse(value, NumberStyles.Any, culture, out double lat)
                   && lat >= -90.0 && lat <= 90.0;
        }
        private void CommitLatitudeMatch()
        {
            if (latitudeBuffer.Length == 0) return;

            var value = latitudeBuffer.ToString();
            if (IsValidLatitude(value))
            {
                latitudeMatches.Add(Tuple.Create(value, matchStartIndex));
            }
            ResetLatitudeState();
        }
      

        private void ResetLatitudeState()
        {
            currentLatState = LatitudeState.Start;
            isNegative = false;
            latitudeBuffer.Clear();
        }

        private void HighlightMatch(int position, int length, Color color)
        {
            richTextBox1.SelectionStart = position;
            richTextBox1.SelectionLength = length;
            richTextBox1.SelectionColor = color;
        }
    }
}

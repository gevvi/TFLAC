using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;

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
            CheckSaveChanges();

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
                    текстToolStripMenuItem.Text = "Text";
                    постановкаЗадачиToolStripMenuItem.Text = "Task";
                    грамматикаToolStripMenuItem.Text = "Grammar";
                    классификацияГрамматикиToolStripMenuItem.Text = "Grammar classification";
                    методАнализаToolStripMenuItem.Text = "Aalysis method";
                    диагностикаИНейтрализацияToolStripMenuItem.Text = "Debag";
                    текстовыйПримерToolStripMenuItem.Text = "Text example";
                    списокЛитературыToolStripMenuItem.Text = "Bibliography";
                    исходныйКодПрограммыToolStripMenuItem.Text = "Source code";
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
                    текстToolStripMenuItem.Text = "Текст";
                    постановкаЗадачиToolStripMenuItem.Text = "Постановка задачи";
                    грамматикаToolStripMenuItem.Text = "Грамматика";
                    классификацияГрамматикиToolStripMenuItem.Text = "Классификация грамматики";
                    методАнализаToolStripMenuItem.Text = "Метод анализа";
                    диагностикаИНейтрализацияToolStripMenuItem.Text = "Диагностика и нейтрализация ошибок";
                    текстовыйПримерToolStripMenuItem.Text = "Текстовый пример";
                    списокЛитературыToolStripMenuItem.Text = "Список литературы";
                    исходныйКодПрограммыToolStripMenuItem.Text = "Исходный код программы";
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
                "\r\n\r\nВыполнены 3 доп.Задания: Изменение размеров текста в окне редактирования и окне вывода результатов."+
                "\r\n\r\nВыбор языка интерфейса приложения (интернационализация)."+
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
        #region Парсер
        //Парсер
        //private void Start_Click(object sender, EventArgs e)
        //{
        //    dataGridView1.Rows.Clear();

        //    try
        //    {
        //        string code = RemoveInsignificantSpaces(richTextBox1.Text);
        //        List<Token> tokens = Scan(code);

        //        foreach (Token token in tokens)
        //        {
        //            dataGridView1.Rows.Add(
        //                token.Code,
        //                token.Type,
        //                token.Value,
        //                $"с {token.StartPosition} по {token.EndPosition}"
        //            );
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Ошибка: {ex.Message}");
        //    }
        //}

        //private string RemoveInsignificantSpaces(string input)
        //{
        //    StringBuilder result = new StringBuilder();
        //    bool inStringLiteral = false;
        //    bool spaceAdded = true; // Начинаем с true, чтобы не добавлять пробелы в начале

        //    for (int i = 0; i < input.Length; i++)
        //    {
        //        char c = input[i];

        //        if (c == '\'')
        //        {
        //            inStringLiteral = !inStringLiteral;
        //            result.Append(c);
        //            spaceAdded = false;
        //        }
        //        else if (inStringLiteral)
        //        {
        //            result.Append(c);
        //            spaceAdded = false;
        //        }
        //        else if (char.IsWhiteSpace(c))
        //        {
        //            if (!spaceAdded && (i + 1 < input.Length) && !IsOperatorChar(input[i + 1]))
        //            {
        //                result.Append(' ');
        //                spaceAdded = true;
        //            }
        //        }
        //        else
        //        {
        //            result.Append(c);
        //            spaceAdded = false;
        //        }
        //    }

        //    return result.ToString().Trim();
        //}

        //private bool IsOperatorChar(char c)
        //{
        //    return c == ':' || c == '=' || c == '+' || c == '-' || c == '*' || c == '/' || c == ';';
        //}
        //private List<Token> Scan(string code)
        //{
        //    List<Token> tokens = new List<Token>();
        //    int position = 0;
        //    int line = 1;
        //    int lineStartPosition = 0;

        //    while (position < code.Length)
        //    {
        //        char current = code[position];
        //        int charPositionInLine = position - lineStartPosition + 1;

        //        // Пропускаем пробелы
        //        if (char.IsWhiteSpace(current))
        //        {

        //            tokens.Add(new Token(
        //                5, "Пробел", " ",
        //                line, charPositionInLine, charPositionInLine
        //            ));

        //            position++;
        //            continue;
        //        }

        //        // Обработка букв (идентификаторы и ключевые слова)
        //        if (char.IsLetter(current))
        //        {
        //            int start = position;
        //            int startCharPos = charPositionInLine;

        //            while (position < code.Length && (char.IsLetterOrDigit(code[position]) || code[position] == '_'))
        //            {
        //                position++;
        //            }

        //            string value = code.Substring(start, position - start);
        //            int endCharPos = startCharPos + (position - start) - 1;

        //            if (keyword.ContainsKey(value))
        //            {
        //                tokens.Add(new Token(
        //                    keyword[value], "Ключевое слово", value,
        //                    line, startCharPos, endCharPos
        //                ));
        //            }
        //            else
        //            {
        //                tokens.Add(new Token(
        //                    4, "Идентификатор", value,
        //                    line, startCharPos, endCharPos
        //                ));
        //            }
        //            continue;
        //        }

        //        // Обработка чисел (целые и дробные)
        //        if (char.IsDigit(current))
        //        {
        //            int start = position;
        //            int startCharPos = charPositionInLine;
        //            bool hasDecimalPoint = false;

        //            while (position < code.Length && (char.IsDigit(code[position]) || (code[position] == '.' && !hasDecimalPoint)))
        //            {
        //                if (code[position] == '.') hasDecimalPoint = true;
        //                position++;
        //            }

        //            string value = code.Substring(start, position - start);
        //            int endCharPos = startCharPos + (position - start) - 1;

        //            tokens.Add(new Token(
        //                hasDecimalPoint ? 9 : 8,
        //                hasDecimalPoint ? "Дробное число" : "Целое число без знака",
        //                value,
        //                line, startCharPos, endCharPos
        //            ));
        //            continue;
        //        }

        //        // Обработка операторов
        //        if (current == ':' && position + 1 < code.Length && code[position + 1] == '=')
        //        {
        //            tokens.Add(new Token(
        //                7, "Оператор присваивания", ":=",
        //                line, charPositionInLine, charPositionInLine + 1
        //            ));
        //            position += 2;
        //            continue;
        //        }

        //        // Обработка точки с запятой
        //        if (current == ';')
        //        {
        //            tokens.Add(new Token(
        //                10, "Конец оператора", ";",
        //                line, charPositionInLine, charPositionInLine
        //            ));
        //            position++;
        //            continue;
        //        }

        //        // Обработка недопустимых символов
        //        tokens.Add(new Token(
        //            11, "Недопустимый символ", current.ToString(),
        //            line, charPositionInLine, charPositionInLine
        //        ));
        //        position++;
        //    }

        //    return tokens;
        //}

        //private class Token
        //{
        //    public int Code { get; }
        //    public string Type { get; }
        //    public string Value { get; }
        //    public int Line { get; }
        //    public int StartPosition { get; }
        //    public int EndPosition { get; }

        //    public Token(int code, string type, string value, int line, int startPos, int endPos)
        //    {
        //        Code = code;
        //        Type = type;
        //        Value = value;
        //        Line = line;
        //        StartPosition = startPos;
        //        EndPosition = endPos;
        //    }
        //}
        #endregion
        #region Сканер

        #endregion
    }
}
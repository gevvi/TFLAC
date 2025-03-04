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
        }

        // Метод для проверки необходимости сохранения изменений
        private DialogResult CheckSaveChanges()
        {
            if (isTextChanged)
            {
                // Показываем диалоговое окно с предложением сохранить изменения
                DialogResult result = MessageBox.Show("Сохранить изменения в файле?", "Сохранение", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Save_button(null, null); // Сохраняем файл
                }

                return result;
            }

            return DialogResult.No; // Если изменений нет, возвращаем "Нет"
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
                "\r\n\r\nМеню «Текст» будет реализовано в последующих лабораторных работах и курсовой работе. При вызове команд этого меню должны открываться окна с соответствующей информацией." +
                "\r\n\r\nКоманда «Пуск» предназначена для запуска анализатора текста. Она также будет реализована в последующих лабораторных работах.";
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

            private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
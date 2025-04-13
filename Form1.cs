using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.DataFormats;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;

namespace Compiler
{
    public partial class Form1 : Form
    {
        //���������� ��� ���������� ���� �����
        private string filePath = null;

        //���������� ��� ����� �����
        int i = 1;

        //����������� ����� textbox
        private const int MaxStackSize = 100;

        // ���� ��� �������� ������� ���������
        private Stack<string> undoStack;

        // ���� ��� �������� ���������� �������� (����������)
        private Stack<string> redoStack;

        // ���� ��� ������������ ���������
        private bool isTextChanged = false;

        // ��������� ������ ������
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

        private int lineNumberOffset = 1; // ������ ��� ������� �����

        public Form1()
        {
            InitializeComponent();
            Tool_tips();
            UpdateTextBoxFont(); // ������������� ��������� �����
            //AddNewTab(); // ��������� ��������� �������AddNewTab(); // ��������� ��������� �������
            undoStack = new Stack<string>();//���� ��� ������
            redoStack = new Stack<string>();//���� ��� ����������
            openFileDialog1.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
            saveFileDialog1.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";
            // ��������� �������������� �� �����
            richTextBox1.AllowDrop = true;
            richTextBox1.DragEnter += MainForm_DragEnter;
            richTextBox1.DragDrop += MainForm_DragDrop;
            richTextBox1.TextChanged += RichTextBox_TextChanged;
            richTextBox1.KeyDown += RichTextBox_KeyDown; // ������������� �� ������� ������� ������
            richTextBox1.VScroll += RichTextBox_VScroll;
            //richTextBox1.Resize += RichTextBox_Resize;
        }
        private void RichTextBox_TextChanged(object sender, EventArgs e)
        {

            // ��������� ������ � �������� ����� ��� ��������� ������
            lineNumberPanel.Invalidate();
            // ��������� ������� ������� ������� � ���������
            int originalPosition = richTextBox1.SelectionStart;
            int originalLength = richTextBox1.SelectionLength;
            Color originalColor = richTextBox1.SelectionColor;

            // ��������� ���������� RichTextBox ��� �������������� ��������
            richTextBox1.SuspendLayout();

            // ���������� ���� ����� ������ �� �����������
            richTextBox1.SelectAll();
            richTextBox1.SelectionColor = richTextBox1.ForeColor;

            // �������� �� ������� ��������� �����
            foreach (var keyword in keywords)
            {
                int startIndex = 0;
                while (startIndex < richTextBox1.TextLength)
                {
                    // ���� �������� ����� ��� ����� ��������
                    int wordStartIndex = richTextBox1.Find(keyword.Key, startIndex, RichTextBoxFinds.WholeWord | RichTextBoxFinds.None);
                    if (wordStartIndex == -1) break;

                    // �������� ��������� �����
                    richTextBox1.SelectionStart = wordStartIndex;
                    richTextBox1.SelectionLength = keyword.Key.Length;

                    // ������ ���� ����������� ������ �� ��������� � �������
                    richTextBox1.SelectionColor = keyword.Value;

                    // �������� ��������� ������ ��� ������ ���������� ���������
                    startIndex = wordStartIndex + keyword.Key.Length;
                }
            }

            // ��������������� ������� ������� � ���������
            richTextBox1.SelectionStart = originalPosition;
            richTextBox1.SelectionLength = originalLength;
            richTextBox1.SelectionColor = originalColor;

            // ������������ ���������� RichTextBox
            richTextBox1.ResumeLayout();
        }
        private void RichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // ���� ������ ������� Enter
            if (e.KeyCode == Keys.Enter)
            {
                // ������������� ��������� ������ � �������� �����
                lineNumberPanel.Invalidate();
            }
            // ���������, ������ �� ���������� Ctrl + C (�����������)
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopyText();
                e.SuppressKeyPress = true; // ������������� ����������� ���������
            }

            // ���������, ������ �� ���������� Ctrl + V (�������)
            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteText();
                e.SuppressKeyPress = true; // ������������� ����������� ���������
            }

            // ���������, ������ �� ���������� Ctrl + V (�������)
            if (e.Control && e.KeyCode == Keys.F)
            {
                SelectAll();
                e.SuppressKeyPress = true; // ������������� ����������� ���������
            }
        }
        private void RichTextBox_VScroll(object sender, EventArgs e)
        {
            // ��������� ������ � �������� ����� ��� ���������
            lineNumberPanel.Invalidate();
        }

        private void RichTextBox_Resize(object sender, EventArgs e)
        {
            // ��������� ������ � �������� ����� ��� ��������� ������� RichTextBox
            lineNumberPanel.Invalidate();
        }

        private void LineNumberPanel_Paint(object sender, PaintEventArgs e)
        {
            // �������� ����������� �������� ��� ���������
            Graphics g = e.Graphics;
            g.Clear(lineNumberPanel.BackColor);

            // �������� ������� ����� � ���� ��� ������� �����
            using (Font font = new Font(richTextBox1.Font.FontFamily, richTextBox1.Font.Size))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                // �������� ������ ������� ������
                int firstVisibleLine = richTextBox1.GetCharIndexFromPosition(new Point(0, 0));
                firstVisibleLine = richTextBox1.GetLineFromCharIndex(firstVisibleLine);

                // �������� ����� ���������� ����� � RichTextBox
                int totalLines = richTextBox1.GetLineFromCharIndex(richTextBox1.TextLength) + 1;

                // �������� ���������� ������� �����
                int visibleLineCount = (int)Math.Ceiling((double)richTextBox1.Height / richTextBox1.Font.Height);

                // ������������ ���������� ������������ ����� �� ������ ���������� �����
                if (firstVisibleLine + visibleLineCount > totalLines)
                {
                    visibleLineCount = totalLines - firstVisibleLine;
                }

                // ������������ ������ ����� ������ ��� ��������������� �����
                for (int i = 0; i < visibleLineCount; i++)
                {
                    int lineNumber = firstVisibleLine + i + 1; // ��������� � 1
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
                // �������� ���������� ����� � ����� ������
                Clipboard.SetText(richTextBox1.SelectedText);
            }
        }

        private void PasteText()
        {
            if (Clipboard.ContainsText())
            {
                // ��������� ����� �� ������ ������
                richTextBox1.Paste();
            }
        }

        private void SelectAll()
        {
            richTextBox1.SelectAll();
        }

        // ����� ��� ���������� ����� �������
        //private void AddNewTab()
        //{
        //    // ������� ����� �������
        //    TabPage newTabPage = new TabPage();
        //    newTabPage.Text = "����� �����"; // �������� �������

        //    // ��������� TextBox �� �������
        //    RichTextBox textBox = new RichTextBox();
        //    textBox.Multiline = true;
        //    textBox.Dock = DockStyle.Fill;
        //    textBox.ScrollBars = ScrollBars.Vertical;
        //    newTabPage.Controls.Add(textBox);

        //    // ��������� ������� � TabControl
        //    tabControl.TabPages.Add(newTabPage);
        //    tabControl.SelectedTab = newTabPage; // ������������� �� ����� �������
        //}

        // ����� ��� �������� ������� �������
        //private void CloseCurrentTab()
        //{
        //    if (tabControl.TabPages.Count > 0)
        //    {
        //        TabPage currentTab = tabControl.SelectedTab;

        //        // ���������, ���� �� ������������� ���������
        //        TextBox textBox = (TextBox)currentTab.Controls[0];
        //        if (!string.IsNullOrEmpty(textBox.Text))
        //        {
        //            DialogResult result = MessageBox.Show("��������� ��������� ����� ���������?", "����������", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        //            if (result == DialogResult.Yes)
        //            {
        //                SaveTabContent(currentTab);
        //            }
        //            else if (result == DialogResult.Cancel)
        //            {
        //                return; // �������� �������� �������
        //            }
        //        }

        //        // ������� �������
        //        tabControl.TabPages.Remove(currentTab);
        //    }
        //}
        
        private void Tool_tips()
        {
            toolTip1.SetToolTip(this.Create, "�������");
            toolTip1.SetToolTip(this.Open, "�������");
            toolTip1.SetToolTip(this.Save, "���������");
            toolTip1.SetToolTip(this.Cansel, "��������");
            toolTip1.SetToolTip(this.Repeat, "���������");
            toolTip1.SetToolTip(this.Copy, "����������");
            toolTip1.SetToolTip(this.Cut, "��������");
            toolTip1.SetToolTip(this.Paste, "��������");
            toolTip1.SetToolTip(this.Start, "����");
            toolTip1.SetToolTip(this.Help, "�������");
            toolTip1.SetToolTip(this.About, "� ���������");
            toolTip1.SetToolTip(this.About, "� ���������");
        }

        private void �������������������������ToolStripMenuItem_Click(object sender, EventArgs e)
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
            // �������� ��������� ����
            string filename = openFileDialog1.FileName;
            // ������ ���� � ������
            string fileText = System.IO.File.ReadAllText(filename);
            richTextBox1.Text = fileText;
            MessageBox.Show("���� ������");
            dataGridView1.Rows[0].Cells[0].Value = filename;
        }

        private void Save_as_button(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                return;
            // �������� ��������� ����
            string filename = saveFileDialog1.FileName;
            // ��������� ����� � ����
            System.IO.File.WriteAllText(filename, richTextBox1.Text);
            MessageBox.Show("���� ��������");
            dataGridView1.Rows[0].Cells[0].Value = filePath;
        }

        // ����� ��� �������� ������������� ���������� ���������
        private void CheckSaveChanges()
        {
            if (isTextChanged)
            {
                // ���������� ���������� ���� � ������������ ��������� ���������
                DialogResult result = MessageBox.Show("��������� ��������� � �����?", "����������", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Save_button(null, null); // ��������� ����
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
                    ����ToolStripMenuItem.Text = "File";
                    �������ToolStripMenuItem.Text = "Create";
                    �������ToolStripMenuItem.Text = "Open";
                    ���������ToolStripMenuItem.Text = "Save";
                    ������������ToolStripMenuItem.Text = "Save as";
                    �����ToolStripMenuItem.Text = "Exit";
                    ������ToolStripMenuItem.Text = "Edit";
                    ��������ToolStripMenuItem.Text = "Undo";
                    ���������ToolStripMenuItem.Text = "Redo";
                    ��������ToolStripMenuItem.Text = "Cut";
                    ����������ToolStripMenuItem.Text = "Copy";
                    ��������ToolStripMenuItem.Text = "Paste";
                    �������ToolStripMenuItem.Text = "Delete";
                    �����������ToolStripMenuItem.Text = "Select all";
                    �����ToolStripMenuItem.Text = "Text";
                    ����������������ToolStripMenuItem.Text = "Task";
                    ����������ToolStripMenuItem.Text = "Grammar";
                    �����������������������ToolStripMenuItem.Text = "Grammar classification";
                    ������������ToolStripMenuItem.Text = "Aalysis method";
                    �������������������������ToolStripMenuItem.Text = "Debag";
                    ���������������ToolStripMenuItem.Text = "Text example";
                    ����������������ToolStripMenuItem.Text = "Bibliography";
                    ��������������������ToolStripMenuItem.Text = "Source code";
                    ����ToolStripMenuItem.Text = "Run";
                    �������ToolStripMenuItem.Text = "Help";
                    ������������ToolStripMenuItem.Text = "Help";
                    ����������ToolStripMenuItem.Text = "About programm";

                    break;

                case 1:
                    ����ToolStripMenuItem.Text = "����";
                    �������ToolStripMenuItem.Text = "�������";
                    �������ToolStripMenuItem.Text = "�������";
                    ���������ToolStripMenuItem.Text = "���������";
                    ������������ToolStripMenuItem.Text = "��������� ���";
                    �����ToolStripMenuItem.Text = "�����";
                    ������ToolStripMenuItem.Text = "������";
                    ��������ToolStripMenuItem.Text = "��������";
                    ���������ToolStripMenuItem.Text = "���������";
                    ��������ToolStripMenuItem.Text = "��������";
                    ����������ToolStripMenuItem.Text = "����������";
                    ��������ToolStripMenuItem.Text = "��������";
                    �������ToolStripMenuItem.Text = "�������";
                    �����������ToolStripMenuItem.Text = "�������� ���";
                    �����ToolStripMenuItem.Text = "�����";
                    ����������������ToolStripMenuItem.Text = "���������� ������";
                    ����������ToolStripMenuItem.Text = "����������";
                    �����������������������ToolStripMenuItem.Text = "������������� ����������";
                    ������������ToolStripMenuItem.Text = "����� �������";
                    �������������������������ToolStripMenuItem.Text = "����������� � ������������� ������";
                    ���������������ToolStripMenuItem.Text = "��������� ������";
                    ����������������ToolStripMenuItem.Text = "������ ����������";
                    ��������������������ToolStripMenuItem.Text = "�������� ��� ���������";
                    ����ToolStripMenuItem.Text = "����";
                    �������ToolStripMenuItem.Text = "�������";
                    ������������ToolStripMenuItem.Text = "����� �������";
                    ����������ToolStripMenuItem.Text = "� ���������";

                    break;
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void �����������������������ToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void Help_button(object sender, EventArgs e)
        {
            string helpText = "� ������ ������������ ������ ����������, " +
                "����� �������� ��� ������� �� ���� �����, ������� � ��������." +
                "\r\n\r\n��������� 3 ���.�������: ��������� �������� ������ � ���� �������������� � ���� ������ �����������."+
                "\r\n\r\n����� ����� ���������� ���������� (�������������������)."+
                "\r\n\r\n�������� ����� ��� �������������� ������ � ���� ���������.";
            MessageBox.Show(helpText);
        }

        private void Save_button(object sender, EventArgs e)
        {
            // ���������, ������ �� ����
            if (string.IsNullOrEmpty(filePath))
            {
                DialogResult result = MessageBox.Show("���� ��� �� ������. ������� �������� ����.", "������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                    Create_button(null, null);
                return;
            }

            // �������� ����� �� TextBox
            string textToSave = richTextBox1.Text;

            try
            {
                // ��������� ����� � ����
                File.WriteAllText(filePath, textToSave);

                // �������� ������������ �� �������� ����������
                MessageBox.Show($"����� ������� �������� � ����: {filePath}", "�����", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // ������������ ������
                MessageBox.Show($"������ ��� ���������� �����: {ex.Message}", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            dataGridView1.Rows[0].Cells[0].Value = filePath;
        }


        private void Create_button(object sender, EventArgs e)
        {
            // ���������� SaveFileDialog ��� ������ ����� ���������� �����
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "������� ����";
            saveFileDialog.Filter = "Text files(*.txt)|*.txt|All files(*.*)|*.*";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // ��������� ���� � �����
                filePath = saveFileDialog.FileName;

                // ������� ������ ����
                File.WriteAllText(filePath, string.Empty);

                // �������� ������������ �� �������� �������� �����
                MessageBox.Show($"���� ������� ������: {filePath}", "�����", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            dataGridView1.Rows[0].Cells[0].Value = filePath;
        }

        private void richTextBox1_TextChanged_1(object sender, EventArgs e)
        {
            isTextChanged = true; // ������������� ���� ���������

            if (undoStack.Count >= MaxStackSize)
            {
                // ������� ����� ������ ���������
                var tempStack = new Stack<string>(undoStack.Reverse().Skip(1).Reverse());
                undoStack = tempStack;
            }

            // ��������� ������� ����� � ����
            undoStack.Push(richTextBox1.Text);
        }


        //������� ������
        private void Cansel_button(object sender, EventArgs e)
        {
            if (undoStack.Count > 1)
            {
                // ��������� ���������� TextChanged
                richTextBox1.TextChanged -= richTextBox1_TextChanged_1;

                // ���������� ������� ��������� � ���� ����������
                redoStack.Push(undoStack.Pop());

                // ������� ������� ��������� �� �����
                undoStack.Pop();

                // ��������������� ���������� ���������
                richTextBox1.Text = undoStack.Peek();

                // �������� ���������� TextChanged
                richTextBox1.TextChanged += richTextBox1_TextChanged_1;
            }
            else
            {
                MessageBox.Show("������ ��������.", "����������", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //������ ��������
        private void Redo(object sender, EventArgs e)
        {
            if (redoStack.Count > 0) // ���������, ���� �� ��� ���������
            {
                // ��������������� ��������� �� ����� ����������
                richTextBox1.Text = redoStack.Peek();

                // ���������� ��������� ������� � ���� ������
                undoStack.Push(redoStack.Pop());

            }
            else
            {
                MessageBox.Show("������ ���������.", "����������", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ���������� ������ "��������"
        private void Cut_button(object sender, EventArgs e)
        {

            // ���������, ���� �� ���������� �����
            if (richTextBox1.SelectionLength > 0)
            {
                // �������� ���������� �����
                richTextBox1.Cut();
            }
            else
            {
                MessageBox.Show("�������� ����� ��� ���������.", "����������", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        //�����������
        private void Copy_button(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionLength > 0)
            {
                richTextBox1.Copy();
            }
            else
            {
                MessageBox.Show("�������� ����� ��� �����������.", "����������", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        //�������
        private void Paste_button(object sender, EventArgs e)
        {
            richTextBox1.Paste();
        }

        //������ �������� ����� ������
        private void Delete_button(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        //������ ��������� ����� ������
        private void Select_all_button(object sender, EventArgs e)
        {
            richTextBox1.SelectAll();
        }

        //��������� ������
        private void UpdateTextBoxFont()
        {
            richTextBox1.Font = new Font(richTextBox1.Font.FontFamily, currentFontSize);
            FontSize.Text = $"������ ������: {currentFontSize}"; // ��������� Label
        }


        private void SizeUP_button(object sender, EventArgs e)
        {
            currentFontSize += 2; // ����������� ������ ������ �� 2 ������
            UpdateTextBoxFont();
        }

        private void SizeDown_button(object sender, EventArgs e)
        {
            if (currentFontSize > 6) // ����������� ������ ������
            {
                currentFontSize -= 2; // ��������� ������ ������ �� 2 ������
                UpdateTextBoxFont();
            }
            else
            {
                MessageBox.Show("����������� ������ ������ ���������.", "����������", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        // ���������� ������� DragEnter
        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            // ���������, ��� ��������������� ������ �������� ������
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // ��������� �����������
            }
            else
            {
                e.Effect = DragDropEffects.None; // ��������� ��������������
            }
        }

        // ���������� ������� DragDrop
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            // �������� ������ ��������������� ������
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // ���������, ��� ��������� ���� �� ���� ����
            if (files != null && files.Length > 0)
            {
                string filePath = files[0]; // ����� ������ ����

                // ���������, ��� ���� ����� ���������� .txt
                if (Path.GetExtension(filePath).ToLower() == ".txt")
                {
                    try
                    {
                        // ������ ���������� ����� � ���������� ��� � RichTextBox
                        richTextBox1.Text = File.ReadAllText(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"������ ��� �������� �����: {ex.Message}", "������", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("�������������� ������ ��������� ����� (.txt).", "������", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void About_button(object sender, EventArgs e)
        {
            string helpText = "��������� 1 ������������ ������";
            MessageBox.Show(helpText);
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        #region ������
        //������
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
        //                $"� {token.StartPosition} �� {token.EndPosition}"
        //            );
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"������: {ex.Message}");
        //    }
        //}

        //private string RemoveInsignificantSpaces(string input)
        //{
        //    StringBuilder result = new StringBuilder();
        //    bool inStringLiteral = false;
        //    bool spaceAdded = true; // �������� � true, ����� �� ��������� ������� � ������

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

        //        // ���������� �������
        //        if (char.IsWhiteSpace(current))
        //        {

        //            tokens.Add(new Token(
        //                5, "������", " ",
        //                line, charPositionInLine, charPositionInLine
        //            ));

        //            position++;
        //            continue;
        //        }

        //        // ��������� ���� (�������������� � �������� �����)
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
        //                    keyword[value], "�������� �����", value,
        //                    line, startCharPos, endCharPos
        //                ));
        //            }
        //            else
        //            {
        //                tokens.Add(new Token(
        //                    4, "�������������", value,
        //                    line, startCharPos, endCharPos
        //                ));
        //            }
        //            continue;
        //        }

        //        // ��������� ����� (����� � �������)
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
        //                hasDecimalPoint ? "������� �����" : "����� ����� ��� �����",
        //                value,
        //                line, startCharPos, endCharPos
        //            ));
        //            continue;
        //        }

        //        // ��������� ����������
        //        if (current == ':' && position + 1 < code.Length && code[position + 1] == '=')
        //        {
        //            tokens.Add(new Token(
        //                7, "�������� ������������", ":=",
        //                line, charPositionInLine, charPositionInLine + 1
        //            ));
        //            position += 2;
        //            continue;
        //        }

        //        // ��������� ����� � �������
        //        if (current == ';')
        //        {
        //            tokens.Add(new Token(
        //                10, "����� ���������", ";",
        //                line, charPositionInLine, charPositionInLine
        //            ));
        //            position++;
        //            continue;
        //        }

        //        // ��������� ������������ ��������
        //        tokens.Add(new Token(
        //            11, "������������ ������", current.ToString(),
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
        #region ������

        #endregion
    }
}
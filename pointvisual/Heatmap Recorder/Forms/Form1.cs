using System;
using System.Windows.Forms;

namespace Heatmap_Recorder
{
    public partial class Form1 : Form
    {        
        public Form1()
        {
            InitializeComponent();

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
        }

        private string SelectFolder()
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog1.SelectedPath))
                return folderBrowserDialog1.SelectedPath;
            else
                return String.Empty;
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            char number = e.KeyChar;

            if (!Char.IsDigit(number) && number != 8)
            {
                e.Handled = true;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //if (textBox2.Text.Length > 0)
            //    if(textBox3.Text.Length > 0)
            //        if (textBox4.Text.Length > 0)
            //            if (textBox1.Text.Length > 0)
            //            {
            //                Data.user = Users.Add(new Users(0, textBox3.Text, comboBox1.SelectedIndex, Int32.Parse(textBox4.Text), richTextBox1.Text), textBox2.Text);

            //                Viewer viewer = new Viewer();
            //                viewer.Show();
            //            }
            //            else
            //                MessageBox.Show("Не указана папка с изображениями для исследования!", "Heatmap Recorder");
            //        else
            //            MessageBox.Show("Не указан возраст респондента!", "Heatmap Recorder");
            //    else
            //        MessageBox.Show("Не указано ФИО респондента!", "Heatmap Recorder");
            //else
            //    MessageBox.Show("Не указана папка для результатов исследования!", "Heatmap Recorder");

            Data.user = new Users(1, "test", 0, 0); //тестовая фигная, убрать

            Viewer viewer = new Viewer(textBox1.Text, @"c:\Users\User\Desktop\Исследование\", Int32.Parse(textBox5.Text), Int32.Parse(textBox6.Text), comboBox2.SelectedIndex);
            viewer.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = SelectFolder();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.Text = SelectFolder();
        }
    }
}

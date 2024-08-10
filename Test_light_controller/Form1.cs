using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Test_light_controller.Light;

namespace Test_light_controller
{
    public partial class Form1 : Form
    {
        public List<UserControl1> UserControls = new List<UserControl1>();
        public UserControl1 selectedUserControl;
        public static event EventHandler<int> ValueC;

        public Form1()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            numericUpDown1.ValueChanged += NumericUpDown1_ValueChanged;
            numericUpDown1.KeyDown += NumericUpDown1_KeyDown;
        }

        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            this.trackBar1.Value = (int)this.numericUpDown1.Value;
        }

        private void NumericUpDown1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                selectedUserControl?.light.SetValue((int)this.numericUpDown1.Value);
                foreach (var item in UserControls)
                {
                    if (item == selectedUserControl)
                    {
                        item.light.Brightness = (int)this.numericUpDown1.Value;
                        ValueC?.Invoke(this, (int)this.numericUpDown1.Value);
                        return;
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.FormClosed += Form2_FormClosed;
            form2.ShowDialog();
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            Form2 form2 = sender as Form2;
            if (form2 != null && form2.IsCancelled)
            {
                return;
            }

            foreach (UserControl1 item in UserControls)
            {
                if (form2.serialStructure.SerialNumber == item.serialStructure.SerialNumber && form2.serialStructure.ChannelValue == item.serialStructure.ChannelValue)
                {
                    MessageBox.Show("重复！");
                    return;
                }
            }

            UserControl1 myControl = new UserControl1(form2.serialStructure);
            myControl.Click += MyControl_Click;

            myControl.Serialportstatuschange += MyControl_Serialportstatuschange;
            myControl.light.PortName = form2.serialStructure.SerialNumber;
            myControl.Delect += MyControl_Delect;

            foreach (UserControl1 item in UserControls)
            {
                if (item.serialStructure.SerialNumber == myControl.serialStructure.SerialNumber)
                {
                    FieldInfo fieldInfo = typeof(LightPPX).GetField("_serialPort", BindingFlags.NonPublic | BindingFlags.Instance);
                    SerialPort value = (SerialPort)fieldInfo.GetValue(item.light);
                    fieldInfo.SetValue(myControl.light, value);
                    if (item.isSerialPortOpen == true)
                    {
                        myControl.isSerialPortOpen = true;
                        myControl.Text = "关闭串口";
                    }
                }
            }
            UserControls.Add(myControl);

            this.panel1.Controls.Add(myControl);
            panel1.Controls.SetChildIndex(myControl, 0);
        }

        private void MyControl_Delect(object sender, EventArgs e)
        {
            UserControl1 userControl1 = (UserControl1)sender;
            for (int i = 0; i < UserControls.Count; i++)
            {
                var item = UserControls[i];
                if (item.serialStructure.SerialNumber == userControl1.serialStructure.SerialNumber && item.serialStructure.ChannelValue == userControl1.serialStructure.ChannelValue)
                {
                    UserControls.RemoveAt(i);
                    panel1.Controls.Remove(userControl1);
                    break;
                }
            }
        }

        private void MyControl_Click(object sender, bool e)
        {
            this.selectedUserControl = (UserControl1)sender;
            this.label8.Text = selectedUserControl.serialStructure.name;
            this.label4.Text = selectedUserControl.serialStructure.ChannelValue.ToString();
            if (e == false)
            {
                return;
            }
            foreach (var item in UserControls)
            {
                if (this.selectedUserControl.serialStructure.SerialNumber == item.serialStructure.SerialNumber)
                {
                    item.isSerialPortOpen = true;
                }
            }
            UpdateMainFormUI();
            SetActiveControl(this.selectedUserControl);
            this.label8.Text = selectedUserControl.serialStructure.name;
        }

        private void MyControl_Serialportstatuschange(object sender, EventArgs e)
        {
            this.numericUpDown1.Value = 0;
            this.trackBar1.Value = 0;
            foreach (var item in UserControls)
            {
                if (this.selectedUserControl.serialStructure.SerialNumber == item.serialStructure.SerialNumber)
                {
                    item.isSerialPortOpen = false;
                }
            }
        }

        public void SetActiveControl(UserControl1 activeControl)
        {
            foreach (var control in UserControls)
            {
                if (control == activeControl)
                {
                    control.BackColor = Color.FromArgb(211, 211, 211);
                    UpdateMainFormUI();
                }
                else
                {
                    control.BackColor = Color.AliceBlue;
                }
            }
        }

        private void UpdateMainFormUI()
        {
            if (selectedUserControl != null)
            {
                if (selectedUserControl.isSerialPortOpen)
                {
                    this.numericUpDown1.Value = selectedUserControl.light.Brightness;
                    this.trackBar1.Value = (int)this.numericUpDown1.Value;
                }
                else
                {
                    this.numericUpDown1.Value = 0;
                    this.trackBar1.Value = 0;
                }
                this.label4.Text = selectedUserControl.light.Sn;
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            this.numericUpDown1.Value = (int)this.trackBar1.Value;
            selectedUserControl?.light.SetValue((int)this.trackBar1.Value);
            foreach (var item in UserControls)
            {
                if (item == selectedUserControl)
                {
                    item.light.Brightness = this.trackBar1.Value;
                    ValueC?.Invoke(this, this.trackBar1.Value);
                    return;
                }
            }
        }
    }
}

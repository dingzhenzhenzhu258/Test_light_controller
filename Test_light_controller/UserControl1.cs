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
    public partial class UserControl1 : UserControl
    {
        public bool isSerialPortOpen = false;
        public LightPPX light = new LightPPX();
        public SerialStructure serialStructure = new SerialStructure();
        public event EventHandler<bool> Click;
        public event EventHandler Serialportstatuschange;
        public event EventHandler NotificationSettings;
        public event EventHandler Delect;
        public static Dictionary<string, UserControl1> openWith = new Dictionary<string, UserControl1>();

        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                this.button1.Text = text;
            }
        }

        public UserControl1()
        {
            InitializeComponent();
        }

        public UserControl1(SerialStructure serialStructure)
        {
            InitializeComponent();
            this.Dock = DockStyle.Top;

            this.label3.Text = serialStructure.ChannelValue.ToString();
            this.label4.Text = serialStructure.name;
            this.serialStructure = serialStructure;
            this.light.Sn = serialStructure.ChannelValue.ToString();
            this.MouseDown += Control_MouseDown;
            this.MouseDoubleClick += UserControl1_MouseDoubleClick1;
            this.BackColor = Color.AliceBlue;
            this.label7.Text = serialStructure.SerialNumber.ToString();
            Form1.ValueC += Form1_ValueC;
        }

        private void UserControl1_MouseDoubleClick1(object sender, MouseEventArgs e)
        {
            UserControl1_MouseDoubleClick(sender, e);
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                UserControl1_Click(sender, e);
            }
        }

        private void Form1_ValueC(object sender, int e)
        {
            Form1 form1 = (Form1)sender;
            if (form1.selectedUserControl == this)
            {
                this.label6.Text = e.ToString();
            }
        }

        private void UserControl1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (button1.Text == "关闭串口" || isSerialPortOpen == true)
            {
                MessageBox.Show("请关闭串口再修改");
                return;
            }
            Form2 form2 = new Form2(this);
            form2.Settings += Form2_Settings;
            NotificationSettings?.Invoke(this, e);
            form2.ShowDialog();
        }

        private void Form2_Settings(object sender, SerialStructure e)
        {
            Form1 parentForm = (Form1)this.FindForm();
            foreach (var item in parentForm.UserControls)
            {
                if (item.serialStructure.SerialNumber == e.SerialNumber && item.serialStructure.ChannelValue == e.ChannelValue)
                {
                    MessageBox.Show("重复的！");
                    return;
                }
            }
            this.label3.Text = e.ChannelValue.ToString();
            this.label4.Text = e.name;
            this.serialStructure = e;
            this.light.Sn = e.ChannelValue.ToString();
            this.label7.Text = e.SerialNumber.ToString();

            Click?.Invoke(this, isSerialPortOpen);   
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!this.IsSelected())
            {
                MessageBox.Show("请先选中控件");
                return;
            }
            if (isSerialPortOpen == false)  // 如果当前串口设备是关闭状态
            {
                OpenSerialPort();
            }
            else
            {
                CloseSerialPort();
            }          
        }

        private void OpenSerialPort()
        {
            try
            {
                light.Connenct(serialStructure.SerialNumber, serialStructure.Baudrate, serialStructure.dataBits, serialStructure.stopBits, serialStructure.parity);
                light.Brightness = light.ReadValue();
                this.label6.Text = light.Brightness.ToString();
                isSerialPortOpen = true;

                Click?.Invoke(this, isSerialPortOpen);
                button1.Text = "关闭串口";
                Form1 parentForm = (Form1)this.FindForm();
                foreach (UserControl1 item in parentForm.UserControls)
                {
                    if (item.serialStructure.SerialNumber == this.serialStructure.SerialNumber)
                    {
                        item.button1.Text = "关闭串口";
                        item.isSerialPortOpen = true;
                        item.light.IsOpen = true;
                    }
                }
                light.IsOpen = true;
                if (openWith.ContainsKey(this.serialStructure.SerialNumber))
                    return;
                openWith.Add(this.serialStructure.SerialNumber,this);
            }
            catch
            {
                MessageBox.Show("打开串口失败，请检查串口", "错误");
            }
        }

        private void CloseSerialPort()
        {
            try
            {
                // 仅允许当前控件关闭串口，确保相同串口号但不同通道值的控件不能关闭已打开的串口
                if (this.ParentForm is Form1 form1)
                {
                    bool canClosePort = true;

                    string sp = this.serialStructure.SerialNumber.ToString();
                    // 如果没有其他控件使用该串口，则执行关闭操作
                    if (this.serialStructure.SerialNumber == openWith[sp].serialStructure.SerialNumber)
                    {
                        Form1 parentForm = (Form1)this.FindForm();
                        foreach (UserControl1 item in parentForm.UserControls)
                        {
                            if (item.serialStructure.SerialNumber == this.serialStructure.SerialNumber)
                            {
                                item.button1.Text = "打开串口";
                                item.isSerialPortOpen = false;
                                item.light.IsOpen = false;
                            }
                        }

                        light.Disconnect();
                        isSerialPortOpen = false;
                        Serialportstatuschange?.Invoke(this, EventArgs.Empty);
                        button1.Text = "打开串口";
                        light.IsOpen = false;

                        // 从 OpenSerialPorts 中移除对应的串口记录
                        openWith.Remove(sp);
                    }
                    else
                    {
                        MessageBox.Show("当前串口已被其他通道打开，请选中对应的通道进行关闭", "提示");
                    }
                }
            }
            catch
            {
                MessageBox.Show("关闭串口失败，请检查串口", "错误");
            }
        }

        private void UserControl1_Click(object sender, EventArgs e)
        {
            Form1 parentForm = (Form1)this.FindForm();
            foreach (UserControl1 item in parentForm.UserControls)
            {

                if (item.serialStructure.SerialNumber == this.serialStructure.SerialNumber && item.serialStructure.ChannelValue != this.serialStructure.ChannelValue && item.light.IsOpen == true)
                {
                    light.Brightness = this.light.ReadValue();
                    this.label6.Text = light.Brightness.ToString();
                    Click?.Invoke(this, isSerialPortOpen);
                    parentForm.SetActiveControl(this);
                    return;
                }
                if (item.serialStructure.SerialNumber == this.serialStructure.SerialNumber && item.serialStructure.ChannelValue == this.serialStructure.ChannelValue && item.light.IsOpen == true)
                {
                    if (isSerialPortOpen == true)
                    {
                        light.Brightness = this.light.ReadValue();
                        this.label6.Text = light.Brightness.ToString();
                        Click?.Invoke(this, isSerialPortOpen);
                        return;
                    }
                }

            }
            Click?.Invoke(this, isSerialPortOpen);
            parentForm.SetActiveControl(this);
        }

        public bool IsSelected()
        {
            return this.BackColor == Color.FromArgb(211, 211, 211);
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (button1.Text == "关闭串口" || isSerialPortOpen == true)
            {
                MessageBox.Show("请关闭串口再删除");
                return;
            }
            Delect?.Invoke(this,EventArgs.Empty);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            light.TurnOff();
            light.Brightness = 0;
            Click?.Invoke(this, isSerialPortOpen);

        }
    }
}

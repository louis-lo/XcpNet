using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MoneyClient
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private static string GetServicePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MoneyService.exe");
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            
        }
    }
}

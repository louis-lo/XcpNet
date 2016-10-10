using XcpClient.Controls;

namespace XcpClient
{
    partial class ClientForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (_hook13 != null)
            {
                _hook13.Dispose();
                _hook13 = null;
            }
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ads = new XcpClient.Controls.MediaControl();
            this.browser = new XcpClient.Controls.WebControl();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // ads
            // 
            this.ads.BackColor = System.Drawing.Color.Black;
            this.ads.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.ads.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ads.Location = new System.Drawing.Point(0, 0);
            this.ads.Name = "ads";
            this.ads.Size = new System.Drawing.Size(800, 500);
            this.ads.TabIndex = 0;
            this.ads.Text = "imgView";
            // 
            // browser
            // 
            this.browser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.browser.IsWebBrowserContextMenuEnabled = false;
            this.browser.Location = new System.Drawing.Point(0, 0);
            this.browser.MinimumSize = new System.Drawing.Size(20, 20);
            this.browser.Name = "browser";
            this.browser.Size = new System.Drawing.Size(800, 500);
            this.browser.TabIndex = 0;
            this.browser.Visible = false;
            this.browser.WebBrowserShortcutsEnabled = false;
            this.browser.BeforeNewWindow += new System.Windows.Forms.WebBrowserNavigatingEventHandler(this.browser_BeforeNewWindow);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // ClientForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 500);
            this.ControlBox = false;
            this.Controls.Add(this.ads);
            this.Controls.Add(this.browser);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ClientForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.ClientForm_Load);
            this.ResumeLayout(false);

        }

        #endregion
        private WebControl browser;
        private MediaControl ads;
        private System.Windows.Forms.Timer timer;
    }
}


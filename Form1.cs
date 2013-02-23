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
using System.Diagnostics;

namespace EverBoxTool
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public partial class Form1 : Form
    {
        private static int Concurrency = 5;
        private System.Threading.Semaphore semaphore = new System.Threading.Semaphore(Concurrency, Concurrency);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.webBrowser1.ScriptErrorsSuppressed = true;
            this.webBrowser1.ObjectForScripting = this;
            this.webBrowser1.Navigate(@"http://www.everbox.com/f/" + this.linkID);
        }

        private string linkID = "mKqJ211fRAFY183t9RYOEXMzDp";
        private HtmlElement fileViewer;
        private Timer scanTimer;
        private string outputPath = @"G:\outputLinks.txt";
        private string downloadPath = @"G:\ClassicMusic\";
        private int ProcessCount = 0;

        private void button1_Click(object sender, EventArgs e)
        {
            this.treeView1.Nodes.Clear();
            this.fileViewer = this.webBrowser1.Document.GetElementById("fileviewer");
            TreeNode tn = new TreeNode(this.getCurrentPath());
            tn.Name = tn.Text;
            tn.Nodes.Add("Loading...");
            this.treeView1.Nodes.Add(tn);
            //if (File.Exists(this.outputPath)) File.Delete(this.outputPath);
        }

        public void newFileLink(string fileName, string fileLink)
        {
            //MessageBox.Show(fileName + "||||" + fileLink);
            string extension = Path.GetExtension(Path.GetFileName(fileName));

            this.textBox2.AppendText(fileName + "\r\n");
            this.textBox2.AppendText(fileLink + "\r\n");
            
            FileStream fs = new FileStream(this.outputPath, FileMode.Append);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            sw.WriteLine(fileName);
            sw.WriteLine(fileLink);
            sw.Flush();
            sw.Close();
            fs.Close();

            return;
            
            fileName = fileName.Replace(" ", "");
            string directory = fileName.Substring(0, fileName.LastIndexOf(@"/"));
            if (!Directory.Exists(downloadPath + directory))
            {
                Directory.CreateDirectory(downloadPath + directory);
            }
            if (File.Exists(downloadPath + fileName))
            {
                return;
            }
            semaphore.WaitOne();
            Process p = new Process();
            Console.WriteLine(fileName);
            p.StartInfo.WorkingDirectory = downloadPath;
            p.StartInfo.FileName = @"C:\Users\Ruby\Desktop\wget-1.11.4-1-bin\bin\wget.exe";
            p.StartInfo.Arguments = string.Format("{0} -O {1}", fileLink, downloadPath + fileName);
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = false;
            p.EnableRaisingEvents = true;
            p.Start();
            p.Exited += (a, b) => { semaphore.Release(1); };
                
        }

        private string getCurrentPath()
        {
            try
            {
                string result = this.webBrowser1.Document.GetElementById("back2_btn").GetAttribute("data-folder");
                if (result == "") return @"/" + this.webBrowser1.Document.GetElementById("folder_title").InnerText;
                else return result;
            }
            catch { return ""; }
        }

        private void goUpFolder()
        {
            this.webBrowser1.Document.GetElementById("back2_btn").InvokeMember("click");
        }

        private void treeView1_AfterExpand(object sender, TreeViewEventArgs e)
        {
            if (e.Node.Name == this.getCurrentPath())
            {
                //MessageBox.Show(e.Node.FullPath);
                if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Loading...")
                {
                    e.Node.Nodes.Clear();
                    foreach (HtmlElement trnode in
                        this.webBrowser1.Document.GetElementById("filelinker").Children[0].Children[0].Children)
                    {
                        TreeNode tn = new TreeNode(trnode.Id);
                        tn.Name = tn.Text;
                        if (this.webBrowser1.Document.GetElementById(tn.Name).GetElementsByTagName("span")[0].Children[0].GetAttribute("className").Contains("folder"))
                        {
                            tn.Nodes.Add("Loading...");
                        }
                        else
                        {
                            string path = tn.Name;
                            path = path.Substring(path.IndexOf("/") + 1);
                            path = path.Substring(path.IndexOf("/") + 1);
                            object[] args = { path };
                            this.webBrowser1.Document.InvokeScript("myfunc2", args);
                        }
                        if ((tn.Name != e.Node.Name) && (tn.Name.StartsWith(e.Node.Name)))
                        {
                            e.Node.Nodes.Add(tn);
                        }
                        else
                        {
                            e.Node.Nodes.Add("Loading...");
                            e.Node.Collapse();
                            return;
                        }
                    }
                }
            }
            else
            {
                /*
                while (!e.Node.Name.StartsWith(this.getCurrentPath()))
                {
                    this.goUpFolder();
                }
                if (e.Node.Name != this.getCurrentPath())
                {
                    string subname = e.Node.Name.Substring(this.getCurrentPath().Length + 1);
                    if (subname.Contains("/"))
                    {
                        subname = subname.Substring(0, subname.IndexOf("/"));
                    }
                    subname = this.getCurrentPath() + "/" + subname;
                    object[] args = { subname };
                */
                object[] args = { e.Node.Name };
                this.webBrowser1.Document.InvokeScript("myfunc", args);
                e.Node.Collapse();
                /*}*/
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.textBox1.Text = this.getCurrentPath();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.scanTimer = new Timer();
            this.scanTimer.Interval = 500;
            this.scanTimer.Tick += scanTimer_Tick;
            this.scanTimer.Start();
        }

        string lastExpand = "";
        int failCount = 0;

        bool expandFirstNode(TreeNode tn)
        {
            if (tn.IsExpanded == false && tn.Nodes.Count > 0)
            {
                tn.Expand();
                if (lastExpand == tn.Name)
                {
                    failCount++;
                    if (failCount > 15)
                    {

                    }
                }
                else
                {
                    lastExpand = tn.Name;
                    failCount = 0;
                }
                return true;
            }
            for (int i = 0; i < tn.Nodes.Count; i++)
            {
                if (expandFirstNode(tn.Nodes[i])) return true;
            }
            return false;
        }

        bool canScan()
        {
            HtmlElement flash = this.webBrowser1.Document.GetElementById("flash");
            if (flash.Style == null) return true;
            if (flash.Style.Contains("none")) return true;
            if (flash.InnerText.Contains("加载中")) return false;
            else return true;
        }

        void scanTimer_Tick(object sender, EventArgs e)
        {
            if (!canScan()) return;
            if (!expandFirstNode(this.treeView1.Nodes[0])) this.scanTimer.Stop();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.goUpFolder();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            HtmlElement he = this.webBrowser1.Document.CreateElement("script");
            he.SetAttribute("type", "text/javascript");
            he.SetAttribute("text", @"function myfunc(arg){everbox.linkViewer.setPath(arg);}");
            this.webBrowser1.Document.Body.AppendChild(he);

            HtmlElement he2 = this.webBrowser1.Document.CreateElement("script");
            he2.SetAttribute("type", "text/javascript");
            he2.SetAttribute("text", @"function myfunc2(arg){
                var n = {};
                n[everbox.csrfKey] = everbox.csrfVal;
                n.path = arg;
                $.ajax({
                    type: ""POST"",
                    url: ""/f/download/"" + everbox.linkViewer.linkId,
                    data: n,
                    dataType: ""json"",
                    success: function (c) {
                        var a = c.code;
                        LinkViewer_alertErrMsg(a);
                        if (a == 200) {
                            //window.location.href = c.data.dataurl
                            window.external.newFileLink(n.path, c.data.dataurl);
                        } else {
                            myfunc2(arg);
                        }
                    }
                })
            }");
            this.webBrowser1.Document.Body.AppendChild(he2);

            //this.webBrowser1.Navigating += webBrowser1_Navigating;
        }

        void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            //MessageBox.Show(e.Url.ToString());
            //e.Cancel = true;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.webBrowser1.Navigate(@"http://www.everbox.com/f/" + this.linkID);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (this.scanTimer != null) this.scanTimer.Stop();
        }

    }
}

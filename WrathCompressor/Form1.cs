using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Compression;

namespace WrathCompressor
{
    public partial class Form1 : Form
    {

        List<CompressedFile> FilesOpen = new List<CompressedFile>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        void ReloadTree()
        {
            ChangeStatus("Reloading Tree...", 0);
            treeView1.Nodes.Clear();

            foreach (CompressedFile f in FilesOpen)
            {
                Invoke((Action)(() => { treeView1.Nodes.Add(f.Name.Remove(0, f.Name.LastIndexOf('\\') + 1)); }));
            }
            ChangeStatus("Ready", 0);
        }

        void ChangeStatus(string status, int progress = 0)
        {
            Invoke((Action)(() => { toolStripStatusLabel1.Text = status; }));

            Invoke((Action)(() => { toolStripProgressBar1.Value = progress; }));
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.Multiselect = true;
            of.ShowDialog();
            foreach (string f in of.FileNames)
            {
                FilesOpen.Add(new CompressedFile() { Name = f });
            }
            ReloadTree();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }
        public static byte[] Compress(byte[] raw)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory,
                    CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }
        public static byte[] Decompress(byte[] gzip)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip),
            CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog();

            Invoke((Action)(() => { sf.ShowDialog(); }));

            using (BinaryWriter bw = new BinaryWriter(new FileStream(sf.FileName, FileMode.Create)))
            {
                ChangeStatus("Saving file...", 0);
                byte[] magic = Encoding.ASCII.GetBytes("7PAK");
                byte[] count_b = BitConverter.GetBytes(FilesOpen.Count);
                bw.Write(magic);
                bw.Write(count_b);
                bw.Write((byte)01); // Not compressed
                int i = 0;
                foreach (CompressedFile f in FilesOpen)
                {
                    try
                    {
                        ChangeStatus("Saving file " + f.Name, (int)((float)(FilesOpen.Count / 100) * i));
                    }
                    catch
                    {
                        ChangeStatus("Saving file " + f.Name, 0);
                    }
                    byte[] filenamelen = BitConverter.GetBytes(f.Name.Remove(0, f.Name.LastIndexOf('\\') + 1).Length);
                    byte[] filename = Encoding.ASCII.GetBytes(f.Name.Remove(0, f.Name.LastIndexOf('\\') + 1));
                    byte[] fileContent;
                    using (var br = new BinaryReader(new FileStream(f.Name, FileMode.Open)))
                    {
                        var data = br.ReadBytes((int)br.BaseStream.Length);
                        fileContent = Compress(data);
                    }
                    byte[] fileContentLen = BitConverter.GetBytes(fileContent.Length);
                    bw.Write(filenamelen);
                    bw.Write(filename);
                    bw.Write(fileContentLen);
                    bw.Write(fileContent);
                    i++;
                }
                ChangeStatus("Ready", 0);
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!backgroundWorker2.IsBusy)
            {
                backgroundWorker2.RunWorkerAsync();
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            Invoke((Action)(() => { of.ShowDialog(); }));

            using (var br = new BinaryReader(new FileStream(of.FileName, FileMode.Open)))
            {
                ChangeStatus("Reading files...", 0);
                FilesOpen.Clear();
                if (Encoding.Default.GetString(br.ReadBytes(4)) != "7PAK")
                {
                    throw new Exception("Magic key did not match");
                }
                var filesOpen = br.ReadInt32();
                var compressed = br.ReadByte();
                int i = 0;
                while (i < filesOpen)
                {
                    i++;
                    // Read file
                    var fileNameLen = br.ReadInt32();
                    var fileName = br.ReadBytes(fileNameLen);
                    var fileContentLen = br.ReadInt32();
                    var fileContent = br.ReadBytes(fileContentLen);
                    if (compressed == (byte)01)
                        fileContent = Decompress(fileContent);
                    FilesOpen.Add(new CompressedFile() { Name = Encoding.ASCII.GetString(fileName), InArchive = true });
                    using (var bw = new BinaryWriter(new FileStream(of.FileName.Remove(of.FileName.LastIndexOf('\\') + 1) + Encoding.Default.GetString(fileName) + "2", FileMode.Create)))
                    {
                        bw.Write(fileContent);
                    }
                }
            }
            ReloadTree();
            ChangeStatus("Ready", 0);
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                var m = FilesOpen.Where(p => p.Name.Remove(0, p.Name.LastIndexOf('\\') + 1) == treeView1.SelectedNode.Text);
                FilesOpen.Remove(m.ToArray()[0]);
                ReloadTree();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            openToolStripMenuItem_Click(this, null);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            saveToolStripMenuItem_Click(this, null);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            removeToolStripMenuItem_Click(this, null);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            addToolStripMenuItem_Click(this, null);
        }

        private void toolStripSeparator3_Click(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }


    public class CompressedFile
    {
        public string Name;
        public bool InArchive;
    }

}

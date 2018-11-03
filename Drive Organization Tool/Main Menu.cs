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
using System.Xml;
using System.Text.RegularExpressions;

namespace Drive_Organization_Tool
{
    public partial class Main_Menu : Form
    {
        public int count = 0;
        private string path_to_cut_from;
        private string path_to_copy_from;
        public Main_Menu()
        {
            InitializeComponent();
            folderview.AllowDrop = true;
            path_to_copy_from = "";
            path_to_cut_from = "";
        }
        private void Main_Menu_Load(object sender, EventArgs e)
        {
            folderview.Nodes.Clear();
            string[] drives = Environment.GetLogicalDrives();
            foreach (string drive in drives)
            {
                DriveInfo temp = new DriveInfo(drive);
                TreeNode node = new TreeNode(drive.Substring(0, 1));
                node.Tag = drive;
                node.Name = temp.Name;
                if (temp.IsReady == true)
                {
                    node.Nodes.Add("...");
                    folderview.Nodes.Add(node);
                }
            }

        }
        private void folderview_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                if (e.Node.Nodes.Count > 0)
                {
                    e.Node.Nodes.Clear();
                    string[] arr = Directory.GetDirectories(e.Node.Tag.ToString());
                    for (int i = 0; i < arr.Length; i++)
                    {
                        DirectoryInfo info = new DirectoryInfo(arr[i]);
                        TreeNode node = new TreeNode(info.Name);
                        if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) continue;
                        node.Tag = arr[i];
                        node.Name = info.Name;
                        e.Node.Nodes.Add(node);
                        node.Nodes.Add("...");
                    }
                }
                string[] arr2 = Directory.GetFiles(e.Node.Tag.ToString());
                foreach (string x in arr2)
                {
                    DirectoryInfo info = new DirectoryInfo(x);
                    TreeNode node = new TreeNode(info.Name);
                    if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) continue;
                    node.Name = info.Name;
                    node.Tag = x;
                    e.Node.Nodes.Add(node);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void folderview_MouseDown(object sender, MouseEventArgs e)
        {
            TreeNode node = folderview.GetNodeAt(e.X, e.Y);
            folderview.SelectedNode = node;
            if (e.Button == MouseButtons.Right)
            {
                if (folderview.SelectedNode == null) return;
                bool isDrive = false;
                string[] drives = Environment.GetLogicalDrives();
                foreach (string drive in drives)
                    if (drive == folderview.SelectedNode.Tag.ToString()) isDrive = true;
                if (isDrive) return;
                if (path_to_copy_from.Length > 0 || path_to_cut_from.Length > 0)
                    pop_up.Items[3].Enabled = true;
                else
                    pop_up.Items[3].Enabled = false;
                if (File.Exists(folderview.SelectedNode.Tag.ToString()) == true) pop_up.Items[3].Enabled = false;
                if (isDrive) pop_up.Items[2].Enabled = false;
                pop_up.Show(MousePosition);
                return;
            }
            else
            {
                if (node == null) return;
                string[] drives = Environment.GetLogicalDrives();
                foreach (string drive in drives)
                {
                    if (node.Tag.ToString() == drive) return;
                }
                if (node != null) folderview.DoDragDrop(node, DragDropEffects.Copy);
            }
        }
        private void folderview_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.None;
            TreeNode nodeSource = (TreeNode)e.Data.GetData(typeof(TreeNode));
            if (nodeSource != null)
            {
                Point pt = new Point(e.X, e.Y);
                pt = folderview.PointToClient(pt);
                TreeNode nodeTarget = folderview.GetNodeAt(pt);
                if (nodeTarget != null)
                {
                    if (Directory.Exists(nodeTarget.Tag.ToString()) == false) return;
                    e.Effect = DragDropEffects.Copy;
                    folderview.SelectedNode = nodeTarget;
                }
            }
        }
        private void folderview_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                TreeView tree = (TreeView)sender;
                Point pt = new Point(e.X, e.Y);
                pt = tree.PointToClient(pt);
                TreeNode nodeTarget = tree.GetNodeAt(pt);
                TreeNode nodeSource = (TreeNode)e.Data.GetData(typeof(TreeNode));
                nodeTarget.Expand();
                string f = @"\";
                f += nodeSource.Text;
                bool done = Move_Dragged(nodeSource.Tag.ToString(), nodeTarget.Tag.ToString(), f);
                if (done)
                    nodeTarget.Nodes.Add((TreeNode)nodeSource.Clone());
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private bool Move_Dragged(string path_source, string path_target, string name)
        {
            bool isFile = File.Exists(path_source);
            try
            {
                if (isFile)
                    File.Move(path_source, path_target + name);
                else
                    Directory.Move(path_source, path_target + name);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                return false;
            }
        }
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string[] drives = Environment.GetLogicalDrives();
                foreach (string drive in drives)
                    if (drive == folderview.SelectedNode.Tag.ToString()) return;
                path_to_copy_from = "";
                path_to_cut_from = folderview.SelectedNode.Tag.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                string[] drives = Environment.GetLogicalDrives();
                foreach (string drive in drives)
                    if (drive == folderview.SelectedNode.Tag.ToString()) return;
                path_to_cut_from = "";
                path_to_copy_from = folderview.SelectedNode.Tag.ToString();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string name = @"\";
            string target = folderview.SelectedNode.Tag.ToString();
            if (path_to_copy_from.Length > 0)
            {
                try
                {
                    bool isFile = File.Exists(path_to_copy_from);
                    if (!isFile)
                    {
                        DirectoryInfo info = new DirectoryInfo(path_to_copy_from);
                        name += info.Name;
                        DirectoryCopy(path_to_copy_from, target + name, true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
            else if (path_to_cut_from.Length > 0)
            {
                try
                {
                    bool isFile = File.Exists(path_to_cut_from);
                    if (!isFile)
                    {
                        DirectoryInfo info = new DirectoryInfo(path_to_cut_from);
                        name += info.Name;
                        Directory.Move(path_to_cut_from, target + name);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message.ToString());
                }
            }
        }
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] drives = Environment.GetLogicalDrives();
            foreach (string drive in drives)
                if (drive == folderview.SelectedNode.Tag.ToString()) return;
            try
            {
                bool isFile = File.Exists(folderview.SelectedNode.Tag.ToString());
                if (isFile)
                {
                    File.Delete(folderview.SelectedNode.Tag.ToString());
                }
                else
                {
                    Directory.Delete(folderview.SelectedNode.Tag.ToString(), true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
            }
        }
        private void print_Click(object sender, EventArgs e)
        {
            if (folderview.SelectedNode == null)
            {
                MessageBox.Show("Select something");
                return;
            }
            string path = folderview.SelectedNode.Tag.ToString();
            Make_XML(path);
        }
        private void Make_XML(string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.CheckCharacters = false;
            settings.NewLineHandling = NewLineHandling.None;
            DirectoryInfo info = new DirectoryInfo(path);
            XmlWriter textWriter = XmlWriter.Create("OrganizerLastReport.Xml", settings);
            textWriter.WriteStartDocument();
            textWriter.WriteStartElement("Root_Node");
            if (info.FullName.Length == 3)
            {
                textWriter.WriteStartAttribute("Drive");
                textWriter.WriteAttributeString("Name", info.Name.Substring(0, 1));
                Explore(info, textWriter);
            }
            else if (File.Exists(info.FullName))
            {
                textWriter.WriteStartAttribute("File");
                textWriter.WriteAttributeString("Name", info.Name.ToString());
            }
            else
            {
                textWriter.WriteStartAttribute("Folder");
                textWriter.WriteAttributeString("Name", info.Name.ToString());
                Explore(info,textWriter);
            }
            textWriter.WriteEndDocument();
            textWriter.Close();
            this.Close();
        }
        private void Explore(DirectoryInfo node , XmlWriter writer)
        {
            try
            {
                foreach (DirectoryInfo i in node.GetDirectories())
                {
                    if( (i.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) continue;
                    writer.WriteStartElement("Directory");
                    writer.WriteAttributeString("Name", i.Name.ToString());
                    foreach (FileInfo j in i.GetFiles())
                    {
                        writer.WriteStartElement("File");
                        writer.WriteAttributeString("Name", j.Name.ToString());
                        writer.WriteEndElement();
                    }
                    Explore(i, writer);
                    writer.WriteEndElement();
                }
            }
            catch(UnauthorizedAccessException)
            {

            }
        }

        private void pop_up_Opening(object sender, CancelEventArgs e)
        {

        }
    }

}

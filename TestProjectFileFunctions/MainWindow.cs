﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using DotNetSiemensPLCToolBoxLibrary;
using DotNetSiemensPLCToolBoxLibrary.Communication;
using DotNetSiemensPLCToolBoxLibrary.DataTypes;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.AWL.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Blocks.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders;
using DotNetSiemensPLCToolBoxLibrary.DataTypes.Projectfolders.Step7V5;
using DotNetSiemensPLCToolBoxLibrary.Projectfiles;
using TestProjectFileFunctions;
using Application = System.Windows.Forms.Application;
using Color = System.Drawing.Color;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.Forms.MessageBox;

namespace JFK_VarTab
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Step7ProjectV5 tmp;

        private void Form1_Load(object sender, EventArgs e)
        {
            tableLayoutPanelVisu.ColumnStyles[1].Width = 0;

            lstConnections.Items.Clear();
            lstConnections.Items.AddRange(PLCConnectionConfiguration.GetConfigurationNames());

            if (lstConnections.Items.Count > 0)
                lstConnections.SelectedItem = lstConnections.Items[0];


            try
            {
                if (Settings.Default.OpenedProjects != null)
                    foreach (string prj in Settings.Default.OpenedProjects)
                    {
                        loadPrj(prj);
                    }
            }
            catch (Exception ex)
            {
                lblStatus.Text = ex.Message;
            }


            if (!string.IsNullOrEmpty(Settings.Default.ProjectsPath))
            {
                lstProjects.Items.Clear();
                lstProjects.Items.AddRange(Projects.GetStep7ProjectsFromDirectory(Settings.Default.ProjectsPath));
            }

        }

        class myTreeNode : TreeNode
        {
            public object myObject;
        }

        public void AddNodes(TreeNode nd, List<ProjectFolder> lst)
        {
            foreach (var subitem in lst)
            {
                myTreeNode tmpNode = new myTreeNode();
                tmpNode.Text = subitem.Name;
                tmpNode.myObject = subitem;
                tmpNode.ImageIndex = 0;
                //nd.ImageKey
                //Set the Image for the Folders...
                if (subitem.GetType() == typeof(StationConfigurationFolder))
                {
                    if (((StationConfigurationFolder)subitem).StationType == PLCType.Simatic300)
                        tmpNode.ImageIndex = 4;
                    else if (((StationConfigurationFolder)subitem).StationType == PLCType.Simatic400 || ((StationConfigurationFolder)subitem).StationType == PLCType.Simatic400H)
                        tmpNode.ImageIndex = 5;
                }
                else if (subitem.GetType() == typeof(CPUFolder))
                {
                    if (((CPUFolder)subitem).CpuType == PLCType.Simatic300)
                        tmpNode.ImageIndex = 2;
                    else if (((CPUFolder)subitem).CpuType == PLCType.Simatic400 || ((CPUFolder)subitem).CpuType == PLCType.Simatic400H)
                        tmpNode.ImageIndex = 3;
                }

                nd.Nodes.Add(tmpNode);

                if (subitem.SubItems != null)
                    AddNodes(tmpNode, subitem.SubItems);
            }
        }       

      

        void loadPrj(string fnm)
        {
            if (fnm != "")
            {
                /*
                dtaSymbolTable.Visible = false;
                lstListBox.Visible = false;
                txtTextBox.Visible = false;
                cmdSetKnowHow.Visible = false;
                cmdRemoveKnowHow.Visible = false;
                cmdUndeleteBlock.Visible = false;
                txtUndeleteName.Visible = false;
                */
                //treeStep7Project.Nodes.Clear();
                Project tmp = Projects.LoadProject(fnm, chkShowDeleted.Checked);
                //tmp = new Step7ProjectV5(fnm, chkShowDeleted.Checked);

                //listBox1.Items.AddRange(tmp.PrgProjectFolders.ToArray());
                //lblProjectName.Text = tmp.ProjectName;
                //lblProjectInfo.Text = tmp.ProjectDescription;

                if (tmp != null)
                {
                    myTreeNode trnd = new myTreeNode();
                    trnd.myObject = tmp.ProjectStructure;
                    trnd.Text = tmp.ToString();
                    if (chkShowDeleted.Checked)
                        trnd.Text += "(show deleted)";
                    AddNodes(trnd, tmp.ProjectStructure.SubItems);
                    treeStep7Project.Nodes.Add(trnd);
                }


            }
        }

        private IBlocksFolder blkFld;
        private SourceFolder src;

        private TreeNode oldNode;
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (myConn != null)
                myConn.Disconnect();

            viewBlockList.Visible = false;            
           
            dtaSymbolTable.Visible = false;
                        
            txtTextBox.Visible = false;
            lblToolStripFileSystemFolder.Text = "";

            lblStatus.Text = "";            

            tableLayoutPanelVisu.ColumnStyles[1].Width = 0;

            datablockView.Visible = false;

            lblToolStripFileSystemFolder.Text = "";

            blkFld = null;


            if (treeStep7Project.SelectedNode != null)
            {
                ProjectFolder fld = (ProjectFolder)((myTreeNode)treeStep7Project.SelectedNode).myObject;
                lblProjectName.Text = fld.Project.ProjectName;
                lblProjectInfo.Text = fld.Project.ProjectDescription;


                var tmp = (myTreeNode)treeStep7Project.SelectedNode;
                if (tmp.myObject is IBlocksFolder)
                    blkFld = (IBlocksFolder)tmp.myObject;

                if (tmp.myObject.GetType() == typeof(SymbolTable))
                {
                    var tmp2 = (SymbolTable)tmp.myObject;

                    if (oldNode != treeStep7Project.SelectedNode)
                    {

                        dtaSymbolTable.Rows.Clear();
                        foreach (var step7SymbolTableEntry in tmp2.SymbolTableEntrys)
                        {
                            dtaSymbolTable.Rows.Add(new object[]
                                                        {
                                                            step7SymbolTableEntry.Symbol, step7SymbolTableEntry.DataType,
                                                            step7SymbolTableEntry.Operand, step7SymbolTableEntry.Comment
                                                        });

                        }
                    }
                    dtaSymbolTable.Visible = true;
                    lblToolStripFileSystemFolder.Text = tmp2.Folder;
                }
                else if (blkFld != null)
                {
                    if (oldNode != treeStep7Project.SelectedNode)
                    {
                        lstListBox.Items.Clear();
                        //ProjectBlockInfo[] arr = 
                        //NumericComparer nc = new NumericComparer();
                        //Array.Sort(arr, nc);
                        lstListBox.Items.AddRange(blkFld.readPlcBlocksList().ToArray());
                    }
                    viewBlockList.Visible = true;

                    if (tmp.myObject.GetType() == typeof(BlocksOfflineFolder))
                        lblToolStripFileSystemFolder.Text = ((BlocksOfflineFolder)blkFld).Folder;
                }
                else if (tmp.myObject.GetType() == typeof(SourceFolder))
                {
                    src = (SourceFolder)tmp.myObject;
                    if (oldNode != treeStep7Project.SelectedNode)
                    {
                        lstListBox.Items.Clear();
                        lstListBox.Items.AddRange(src.readPlcBlocksList().ToArray());
                    }
                    viewBlockList.Visible = true;

                    lblToolStripFileSystemFolder.Text = src.Folder;
                }
            }
            oldNode = treeStep7Project.SelectedNode;
        }

        private IDataBlock myBlk = null;
        private S7DataRow expRow = null;

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            viewBlockList.Visible = false;
            
            if (lstListBox.SelectedItem is ProjectBlockInfo)
            {
                viewBlockList.Visible = false;

                lblStatus.Text = ((ProjectBlockInfo)lstListBox.SelectedItem).ToString();

                Block tmp;
                if (blkFld is BlocksOfflineFolder)
                    tmp = ((BlocksOfflineFolder)blkFld).GetBlock((ProjectBlockInfo)lstListBox.SelectedItem, new S7ConvertingOptions() { GenerateCallsfromUCs = convertCallsToolStripMenuItem.Checked });
                else
                    tmp = blkFld.GetBlock((ProjectBlockInfo) lstListBox.SelectedItem);

                if (tmp != null)
                {
                    if (tmp.BlockType == PLCBlockType.UDT || tmp.BlockType == PLCBlockType.DB || tmp.BlockType == PLCBlockType.S5_DV || tmp.BlockType == PLCBlockType.S5_DB)
                    {
                        //dataBlockViewControl.DataBlockRows = ((PLCDataBlock) tmp).Structure;
                        myBlk = (IDataBlock)tmp;
                        expRow = myBlk.Structure;
                        if (mnuExpandDatablockArrays.Checked)
                            expRow = myBlk.GetArrayExpandedStructure(new S7DataBlockExpandOptions() { ExpandCharArrays = false });
                        dataBlockViewControl.DataBlockRows = expRow;
                        
                        datablockView.Visible = true;
                       
                    }
                    else
                    {
                        txtTextBox.Text = tmp.ToString();
                        txtTextBox.Visible = true;
                    }
                }

            }
            else if (lstListBox.SelectedItem.GetType() == typeof(S7ProjectSourceInfo))
            {
                var tmp = (S7ProjectSourceInfo)lstListBox.SelectedItem;

                if (tmp != null)
                {
                    string fnm = tmp.Filename;

                    if (fnm != null && fnm != "")
                        if (System.IO.File.Exists(fnm))
                            txtTextBox.Text = new System.IO.StreamReader(tmp.Filename).ReadToEnd();
                }

                txtTextBox.Visible = true;
                //lstListBox.Visible = false;
            }

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstListBox.SelectedItem != null)
                if (lstListBox.SelectedItem.GetType() == typeof(S7ProjectBlockInfo))
                {
                    var tmp = (S7ProjectBlockInfo)lstListBox.SelectedItem;
                    if (tmp.BlockType == PLCBlockType.DB)
                        tableLayoutPanelVisu.ColumnStyles[1].Width = 255;
                    //grpVisu.Visible = true;
                    else
                        //grpVisu.Visible = false;
                        tableLayoutPanelVisu.ColumnStyles[1].Width = 0;
                    if (!tmp.Deleted)
                    {
                        cmdUndeleteBlock.Visible = false;
                        txtUndeleteName.Visible = false;
                    }
                    else
                    {
                        //cmdUndeleteBlock.Visible = true;
                        //txtUndeleteName.Visible = true;
                    }
                }
        }

        /*
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            loadPrj();
        }
        */

        private void button2_Click(object sender, EventArgs e)
        {
            if (lstListBox.SelectedItem != null)
                if (lstListBox.SelectedItem.GetType() == typeof(S7ProjectBlockInfo))
                {
                    ((BlocksOfflineFolder) blkFld).ChangeKnowHowProtection((S7ProjectBlockInfo)lstListBox.SelectedItem, true);
                    lstListBox.Items.Clear();
                    lstListBox.Items.AddRange(blkFld.readPlcBlocksList().ToArray());
                }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (lstListBox.SelectedItem != null)
                if (lstListBox.SelectedItem.GetType() == typeof(S7ProjectBlockInfo))
                {
                    ((BlocksOfflineFolder)blkFld).ChangeKnowHowProtection((S7ProjectBlockInfo)lstListBox.SelectedItem, false);
                    lstListBox.Items.Clear();
                    lstListBox.Items.AddRange(blkFld.readPlcBlocksList().ToArray());
                }


        }

        private void button4_Click(object sender, EventArgs e)
        {
            //This Button is not showed, because it does not work.
            //After undeleteion of a Block you have to recreate the mdx File of the database, and this needs to be implemented
            if (lstListBox.SelectedItem != null)
                if (lstListBox.SelectedItem.GetType() == typeof(S7ProjectBlockInfo))
                {
                    ((BlocksOfflineFolder)blkFld).UndeleteBlock((S7ProjectBlockInfo)lstListBox.SelectedItem, Convert.ToInt32(txtUndeleteName.Text));
                    treeView1_AfterSelect(sender, null);
                }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void treeView1_Click(object sender, EventArgs e)
        {
            if (treeStep7Project.SelectedNode != null)
                treeView1_AfterSelect(sender, null);
        }

        private void cmdCreateFlexibleErrorMessages_Click(object sender, EventArgs e)
        {
            S7DataBlock myDB = (S7DataBlock)((BlocksOfflineFolder)blkFld).GetBlock((S7ProjectBlockInfo)lstListBox.SelectedItem);

            int cnt = 0;

            if (myDB.Structure != null && myDB.Structure.Children != null)
                cnt = myDB.Structure.Children[myDB.Structure.Children.Count - 1].NextBlockAddress.ByteAddress;

            string varname = "STOERUNGEN_DB" + myDB.BlockNumber;

            string tags = varname + ";" + txtConnectionName.Text + ";DB " + myDB.BlockNumber + " DBW 0;Int;;" + ((cnt - 2) / 2).ToString() + ";2;1 s;;;;;0;10;0;100;0;;0;\r\n";

            string errors = "";

            int errNr = Convert.ToInt32(txtStartErrorNumber.Text);

            foreach (S7DataRow plcDataRow in S7DataRow.GetChildrowsAsList(myDB.Structure)) // myDB.GetRowsAsList())
            {
                if (plcDataRow.DataType == S7DataRowType.BOOL)
                {
                    ByteBitAddress akAddr = plcDataRow.BlockAddress;

                    int bitnr = (akAddr.ByteAddress / 2) * 16 + akAddr.BitAddress;//akAddr.BitAddress;
                    if (akAddr.ByteAddress % 2 == 0)
                        bitnr += 8;

                    string stoeTxt = "";
                    string stoeTxtEn = "";

                    stoeTxt = plcDataRow.Comment;
                    if (stoeTxt.Contains(";"))
                    {
                        stoeTxt = "Störort: " + stoeTxt.Split(';')[0] + ", " + stoeTxt.Split(';')[1];
                    }

                    if (chkFixedErrorNumber.Checked)
                        errNr = Convert.ToInt32(txtStartErrorNumber.Text) + akAddr.ByteAddress * 8 + akAddr.BitAddress;
                    errors += "\"D\"\t\"" + errNr.ToString() + "\"\t\"Alarms\"\t\"" + varname + "\"\t\"" + bitnr.ToString() + "\"\t\t\t\t\t\t\"0\"\t\"de-DE=" + stoeTxt + "\"\t\"en-US=" + stoeTxtEn + "\"\t\"de-DE=\"" + "\r\n";
                    if (!chkFixedErrorNumber.Checked)
                        errNr++;
                }
            }

            FolderBrowserDialog fldDlg = null;

            fldDlg = new FolderBrowserDialog();
            fldDlg.Description = "Destination Diretory for Alarms.csv and Tags.csv!";
            if (fldDlg.ShowDialog() == DialogResult.OK)
            {
                System.IO.StreamWriter swr;

                swr = new System.IO.StreamWriter(fldDlg.SelectedPath + "\\Alarms.csv");
                swr.Write(errors);
                swr.Close();

                swr = new System.IO.StreamWriter(fldDlg.SelectedPath + "\\Tags.csv");
                swr.Write(tags.Replace(";", "\t"));
                swr.Close();
            }
        }

        private void treeStep7Project_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.ImageIndex == 1)
                e.Node.ImageIndex = 0;
        }

        private void treeStep7Project_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.ImageIndex == 0)
                e.Node.ImageIndex = 1;
        }

        private void cmdCreateWinCCErrorMessages_Click(object sender, EventArgs e)
        {
            HMIGENOBJECTSLib.HMIGO HMIGOObject = null;
            try
            {
                HMIGOObject = new HMIGENOBJECTSLib.HMIGO();
            }
            catch (Exception ex)
            {
                MessageBox.Show("The WinCC Object could not be created!\n\n Error:" + ex.Message);
            }

            try
            {
                S7DataBlock myDB = (S7DataBlock)((BlocksOfflineFolder)blkFld).GetBlock((S7ProjectBlockInfo)lstListBox.SelectedItem);

                int cnt = 0;

                if (myDB.Structure != null && myDB.Structure.Children != null)
                    cnt = myDB.Structure.Children[myDB.Structure.Children.Count - 1].NextBlockAddress.ByteAddress;

                string varname = "STOERUNGEN_DB" + myDB.BlockNumber;

                for (int n = 0; n < cnt / 2; n++)
                {
                    try
                    {
                        HMIGOObject.CreateTag(varname + "_" + (n + 1).ToString(), HMIGENOBJECTSLib.HMIGO_TAG_TYPE.TAG_UNSIGNED_16BIT_VALUE, txtConnectionName.Text, "DB" + myDB.BlockNumber + ",DD" + (n*2).ToString(), "Stoerungen");
                    }
                    catch (COMException ex)
                    {
                        if (ex.ErrorCode != -2147196408)
                            throw ex;
                    }
                    
                }

                string errors = "";

                int errNr = Convert.ToInt32(txtStartErrorNumber.Text);

                foreach (S7DataRow plcDataRow in S7DataRow.GetChildrowsAsList(myDB.Structure)) // myDB.GetRowsAsList())
                {
                    if (plcDataRow.DataType == S7DataRowType.BOOL)
                    {
                        ByteBitAddress akAddr = plcDataRow.BlockAddress;
                        int varnr = (akAddr.ByteAddress / 2) + 1;

                        int bitnr = akAddr.BitAddress;
                        if (akAddr.ByteAddress % 2 == 0)
                            bitnr += 8;

                        string stoeTxt = "";
                        string stoeOrt = "";
                        string stoeTxtEn = "";

                        stoeTxt = plcDataRow.Comment;
                        if (stoeTxt.Contains(";"))
                        {
                            stoeTxt = stoeTxt.Split(';')[1];
                            stoeOrt = stoeTxt.Split(';')[0];
                        }

                        if (chkFixedErrorNumber.Checked)
                            errNr = Convert.ToInt32(txtStartErrorNumber.Text) + akAddr.ByteAddress * 8 + akAddr.BitAddress;

                        try
                        {
                            HMIGOObject.CreateSingleAlarm(errNr, HMIGENOBJECTSLib.HMIGO_SINGLE_ALARM_CLASS_ID.SINGLE_ALARM_ERROR, 1, stoeTxt, varname + "_" + varnr.ToString(), bitnr);
                            HMIGOObject.SingleAlarmInfoText = stoeTxt;
                            HMIGOObject.SingleAlarmText2ID = stoeOrt;
                            HMIGOObject.CommitSingleAlarm();
                        }
                        catch (System.Runtime.InteropServices.COMException ex)
                        {
                            if (ex.ErrorCode != -2147467259)
                                throw ex;
                        }

                        //errors += "\"D\"\t\"" + errNr.ToString() + "\"\t\"Alarms\"\t\"" + varname + "\"\t\"" + bitnr.ToString() + "\"\t\t\t\t\t\t\"0\"\t\"de-DE=" + stoeTxt + "\"\t\"en-US=" + stoeTxtEn + "\"\t\"de-DE=\"" + "\r\n";
                        if (!chkFixedErrorNumber.Checked)
                            errNr++;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                if (ex.ErrorCode == -2147195889)
                    MessageBox.Show("Error: The Connection Name you specified does not exist!");                               
                else
                    MessageBox.Show("Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void cmdCreateWinCCTags_Click(object sender, EventArgs e)
        {
            HMIGENOBJECTSLib.HMIGO HMIGOObject = null;
            try
            {
                HMIGOObject = new HMIGENOBJECTSLib.HMIGO();
            }
            catch (Exception ex)
            {
                MessageBox.Show("The WinCC Object could not be created!\n\n Error:" + ex.Message);
            }
            S7DataBlock myDB = (S7DataBlock)((BlocksOfflineFolder)blkFld).GetBlock((S7ProjectBlockInfo)lstListBox.SelectedItem);

            List<S7DataRow> myLst = null;
            if (chkExpandArrays.Checked)
                myLst = S7DataRow.GetChildrowsAsList(myDB.GetArrayExpandedStructure(new S7DataBlockExpandOptions())); // ) myDB.GetRowsAsArrayExpandedList(ne);
            else
                myLst = S7DataRow.GetChildrowsAsList(myDB.Structure); // myDB.GetRowsAsList();

            int cnt = 0;

            try
            {
                foreach (S7DataRow plcDataRow in myLst)
                {
                    string tagName = txtTagsPrefix.Text + plcDataRow.StructuredName.Replace(".", "_").Replace("[", "_").Replace("]", "").Replace(" ", "").Replace(",", "_");
                    try
                    {
                        switch (plcDataRow.DataType)
                        {
                            case S7DataRowType.BOOL:
                                HMIGOObject.CreateTag(tagName, HMIGENOBJECTSLib.HMIGO_TAG_TYPE.TAG_BINARY_TAG, txtConnectionName.Text, "DB" + myDB.BlockNumber + ",D" + plcDataRow.BlockAddress.ToString(), "TAGS_DB" + myDB.BlockNumber);
                                break;
                            case S7DataRowType.INT:
                                HMIGOObject.CreateTag(tagName, HMIGENOBJECTSLib.HMIGO_TAG_TYPE.TAG_SIGNED_16BIT_VALUE, txtConnectionName.Text, "DB" + myDB.BlockNumber + ",DW" + plcDataRow.BlockAddress.ByteAddress.ToString(), "TAGS_DB" + myDB.BlockNumber);
                                break;
                            case S7DataRowType.DINT:
                                HMIGOObject.CreateTag(tagName, HMIGENOBJECTSLib.HMIGO_TAG_TYPE.TAG_SIGNED_32BIT_VALUE, txtConnectionName.Text, "DB" + myDB.BlockNumber + ",DD" + plcDataRow.BlockAddress.ByteAddress.ToString(), "TAGS_DB" + myDB.BlockNumber);
                                break;
                            case S7DataRowType.WORD:
                                HMIGOObject.CreateTag(tagName, HMIGENOBJECTSLib.HMIGO_TAG_TYPE.TAG_UNSIGNED_16BIT_VALUE, txtConnectionName.Text, "DB" + myDB.BlockNumber + ",DW" + plcDataRow.BlockAddress.ByteAddress.ToString(), "TAGS_DB" + myDB.BlockNumber);
                                break;
                            case S7DataRowType.DWORD:
                                HMIGOObject.CreateTag(tagName, HMIGENOBJECTSLib.HMIGO_TAG_TYPE.TAG_UNSIGNED_32BIT_VALUE, txtConnectionName.Text, "DB" + myDB.BlockNumber + ",DD" + plcDataRow.BlockAddress.ByteAddress.ToString(), "TAGS_DB" + myDB.BlockNumber);
                                break;
                            case S7DataRowType.BYTE:
                                HMIGOObject.CreateTag(tagName, HMIGENOBJECTSLib.HMIGO_TAG_TYPE.TAG_UNSIGNED_8BIT_VALUE, txtConnectionName.Text, "DB" + myDB.BlockNumber + ",DBB" + plcDataRow.BlockAddress.ByteAddress.ToString(), "TAGS_DB" + myDB.BlockNumber);
                                break;
                            case S7DataRowType.REAL:
                                HMIGOObject.CreateTag(tagName, HMIGENOBJECTSLib.HMIGO_TAG_TYPE.TAG_FLOATINGPOINT_NUMBER_32BIT_IEEE_754, txtConnectionName.Text, "DB" + myDB.BlockNumber + ",DD" + plcDataRow.BlockAddress.ByteAddress.ToString(), "TAGS_DB" + myDB.BlockNumber);
                                break;
                        }
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        if (ex.ErrorCode != -2147196408)
                            throw ex;
                        //Tag existiert schoin                            
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                if (ex.ErrorCode == -2147195889)
                    MessageBox.Show("Error: The Connection Name you specified does not exist!");
                else
                    MessageBox.Show("Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }

        }

        private void elementHost1_ChildChanged(object sender, System.Windows.Forms.Integration.ChildChangedEventArgs e)
        {

        }

        PLCConnection myConn;
       

        private List<PLCTag> valueList = null;
        private ScrollViewer myScrollViewer = null;
        private int oldPos = 0;
        private void fetchPLCData_Tick(object sender, EventArgs e)
        {
            if (lblConnected.BackColor == Color.LightGreen)
                lblConnected.BackColor = Color.DarkGray;
            else
                lblConnected.BackColor = Color.LightGreen;

            try
            {
                if (myConn.Connected)
                {


                    /* Get the ScrollViewer des XAML Trees (um scrollposition zu lesen!)... */ 
                    if ( myScrollViewer == null)
                    {
                        DependencyObject tst = VisualTreeHelper.GetChild(dataBlockViewControl.MyTree, 0);
                        while (tst != null && tst.GetType() != typeof (ScrollViewer))
                        {
                            tst = VisualTreeHelper.GetChild(tst, 0);
                        }
                        if (tst != null)
                        {
                            myScrollViewer = (ScrollViewer) tst;
                            
                        }
                    }

                    //nur die angezeigten Values von der SPS lesen...
                    int start = (int) myScrollViewer.VerticalOffset/20;
                    if (valueList == null || start!=oldPos)
                    {
                        List<S7DataRow> tmpLst = S7DataRow.GetChildrowsAsList(expRow);
                        List<S7DataRow> askLst=new List<S7DataRow>();
                        for (int n = 0; n < tmpLst.Count;n++ )
                        {
                            if (n>=start&&n<start+24)
                            {
                                askLst.Add(tmpLst[n]);
                            }
                        }
                        valueList = S7DataRow.GetLibnoDaveValues(askLst);
                        oldPos = start;
                    }
                    myConn.ReadValues(valueList);
                }
                else
                {
                    oldPos = 0;
                    myScrollViewer = null;
                    fetchPLCData.Enabled = false;
                    valueList = null;
                    lblConnected.BackColor = Color.DarkGray;                    
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = ex.Message;
            }
        }

       
        private void lstListBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                listBox1_DoubleClick(sender, null);
        }

       

        private void cmdCloseProject_Click(object sender, EventArgs e)
        {
            if (treeStep7Project.SelectedNode != null)
            {
                myTreeNode nd = (myTreeNode) treeStep7Project.SelectedNode;                                
                while (nd.Parent!=null)
                {
                    nd = (myTreeNode)nd.Parent;
                }
                treeStep7Project.Nodes.Remove(nd);
            }

            List<string> projects=new List<string>();
            foreach (myTreeNode myTreeNode in treeStep7Project.Nodes)
            {
                projects.Add(((ProjectFolder) myTreeNode.myObject).Project.ProjectFile);
            }
            Settings.Default.OpenedProjects = projects.ToArray();
            Settings.Default.Save();

        }

        private void chkShowDeleted_CheckedChanged(object sender, EventArgs e)
        {
            if (chkShowDeleted.Checked)
                MessageBox.Show("After checking this checkbox please reopen the project to show deleted blocks!");
        }

        private void treeStep7Project_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button==MouseButtons.Right)
            {
                treeStep7Project.ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("Close", delegate { cmdCloseProject_Click(sender, null); }), });
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            FolderBrowserDialog fldDlg=new FolderBrowserDialog();
            if (!string.IsNullOrEmpty(Settings.Default.ProjectsPath))
                fldDlg.SelectedPath = Settings.Default.ProjectsPath;
            fldDlg.ShowNewFolderButton = false;
            DialogResult ret = fldDlg.ShowDialog();

            
            if (ret == DialogResult.OK)
            {
                lstProjects.Items.Clear();
                Settings.Default.ProjectsPath = fldDlg.SelectedPath;
                Settings.Default.Save();
                lstProjects.Items.AddRange(Projects.GetStep7ProjectsFromDirectory(fldDlg.SelectedPath));                
            }
        }

        private void lstProjects_DoubleClick(object sender, EventArgs e)
        {
            if (lstProjects.SelectedItem!=null)
            {
                Project tmp = (Project) lstProjects.SelectedItem;
                loadPrj(tmp.ProjectFile);
            }

            List<string> projects = new List<string>();
            foreach (myTreeNode myTreeNode in treeStep7Project.Nodes)
            {
                projects.Add(((ProjectFolder)myTreeNode.myObject).Project.ProjectFile);
            }
            Settings.Default.OpenedProjects = projects.ToArray();
            Settings.Default.Save();
        }            
       
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void featuresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            (new Features()).ShowDialog();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "All supported types (*.zip, *.s7p, *.s5d)|*.s7p;*.zip;*.s5d|Step5 Project|*.s5d|Step7 V5.5 Project|*.s7p|Zipped Step5/Step7 Project|*.zip";

            var ret = op.ShowDialog();
            if (ret == DialogResult.OK)
            {
                loadPrj(op.FileName);
            }

            List<string> projects = new List<string>();
            foreach (myTreeNode myTreeNode in treeStep7Project.Nodes)
            {
                projects.Add(((ProjectFolder)myTreeNode.myObject).Project.ProjectFile);
            }
            Settings.Default.OpenedProjects = projects.ToArray();
            Settings.Default.Save();
        }

        private void configConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            myConn = null;
            var tmp = lstConnections.SelectedItem;

            Configuration.ShowConfiguration("Verbindung_1", false);

            lstConnections.Items.Clear();
            lstConnections.Items.AddRange(PLCConnectionConfiguration.GetConfigurationNames());

            if (tmp != null && lstConnections.Items.Contains(tmp))
                lstConnections.SelectedItem = tmp;
        }

        private void lstConnections_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            myConn = new PLCConnection((string)lstConnections.SelectedItem);
            if (lstConnections.SelectedItem != null)
                lblConnInfo.Text = new PLCConnectionConfiguration((string)lstConnections.SelectedItem).ToString();
        }

        private void watchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "";
            try
            {
                if (myConn != null)
                    myConn.Dispose();
                myConn = new PLCConnection((string)lstConnections.SelectedItem);
                myConn.Connect();
                fetchPLCData.Enabled = true;
            }
            catch (Exception ex)
            {
                lblStatus.Text = ex.Message;
            }
            //myConn.Disconnect();
        }

        private void unwatchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lblConnected.BackColor = Color.DarkGray;
            if (myConn != null && myConn.Connected)
                myConn.Disconnect();
        }

        private void mnuExpandDatablockArrays_Click(object sender, EventArgs e)
        {
            if (mnuExpandDatablockArrays.Checked)
                mnuExpandDatablockArrays.Checked = false;
            else
                mnuExpandDatablockArrays.Checked = true;
            unwatchToolStripMenuItem_Click(sender, e);
            expRow = myBlk.Structure;
            if (mnuExpandDatablockArrays.Checked)
                expRow = myBlk.GetArrayExpandedStructure(new S7DataBlockExpandOptions() { ExpandCharArrays = false });
            dataBlockViewControl.DataBlockRows = expRow;
        }

        private void treeStep7Project_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeStep7Project.SelectedNode = e.Node;
                treeView1_AfterSelect(sender, null);
            }
        }

        private void downloadOnlineBlockToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DownloadBlock myDown = new DownloadBlock((string)lstConnections.SelectedItem);
            myDown.ShowDialog();
            
        }

        private void convertCallsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            convertCallsToolStripMenuItem.Checked = !convertCallsToolStripMenuItem.Checked;
        }

        private void dBStructResizerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DBStructresizer stRz = new DBStructresizer();
            stRz.ShowDialog();
        }




    }
}

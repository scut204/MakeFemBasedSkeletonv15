namespace IsolineEditing
{
    partial class FormMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            MyGeometry.Matrix4d matrix4d1 = new MyGeometry.Matrix4d();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSplitButtonOpen = new System.Windows.Forms.ToolStripSplitButton();
            this.openMeshFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openSelectionFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openCameraFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSplitButtonSaveFile = new System.Windows.Forms.ToolStripSplitButton();
            this.saveMeshFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSelectionFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveCameraFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonOpenSkeleton = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSaveSkeleton = new System.Windows.Forms.ToolStripButton();
            this.toolStripGetSplitSlicerButton = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonViewingTool = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonSelectionTool = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonMovingTool = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonAutoUpdate = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonAbout = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonExit = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.labelVertexCount = new System.Windows.Forms.Label();
            this.buttonCloseTab = new System.Windows.Forms.Button();
            this.buttonShowHideProperty = new System.Windows.Forms.Button();
            this.buttonShowHideOutput = new System.Windows.Forms.Button();
            this.tabControlModelList = new System.Windows.Forms.TabControl();
            this.meshView1 = new IsolineEditing.MeshView();
            this.buttonClearOutputText = new System.Windows.Forms.Button();
            this.textBoxOutput = new System.Windows.Forms.TextBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageModel = new System.Windows.Forms.TabPage();
            this.propertyGridModel = new System.Windows.Forms.PropertyGrid();
            this.tabPageDisplay = new System.Windows.Forms.TabPage();
            this.propertyGridDisplay = new System.Windows.Forms.PropertyGrid();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageModel.SuspendLayout();
            this.tabPageDisplay.SuspendLayout();
            this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
            this.toolStripContainer1.ContentPanel.SuspendLayout();
            this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
            this.toolStripContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1,
            this.toolStripSplitButtonOpen,
            this.toolStripSplitButtonSaveFile,
            this.toolStripSeparator1,
            this.toolStripButton2,
            this.toolStripButtonOpenSkeleton,
            this.toolStripButtonSaveSkeleton,
            this.toolStripGetSplitSlicerButton,
            this.toolStripButtonViewingTool,
            this.toolStripButtonSelectionTool,
            this.toolStripButtonMovingTool,
            this.toolStripSeparator3,
            this.toolStripButtonAutoUpdate,
            this.toolStripSeparator2,
            this.toolStripSeparator4,
            this.toolStripButtonAbout,
            this.toolStripButtonExit});
            this.toolStrip1.Location = new System.Drawing.Point(3, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(930, 27);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(146, 24);
            this.toolStripButton1.Text = "Open Mesh File";
            this.toolStripButton1.Click += new System.EventHandler(this.toolStripSplitButtonOpen_ButtonClick);
            // 
            // toolStripSplitButtonOpen
            // 
            this.toolStripSplitButtonOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSplitButtonOpen.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openMeshFileToolStripMenuItem,
            this.openSelectionFileToolStripMenuItem,
            this.openCameraFileToolStripMenuItem});
            this.toolStripSplitButtonOpen.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButtonOpen.Image")));
            this.toolStripSplitButtonOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSplitButtonOpen.Name = "toolStripSplitButtonOpen";
            this.toolStripSplitButtonOpen.Size = new System.Drawing.Size(39, 24);
            this.toolStripSplitButtonOpen.Text = "Open File";
            this.toolStripSplitButtonOpen.Visible = false;
            this.toolStripSplitButtonOpen.ButtonClick += new System.EventHandler(this.toolStripSplitButtonOpen_ButtonClick);
            // 
            // openMeshFileToolStripMenuItem
            // 
            this.openMeshFileToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.openMeshFileToolStripMenuItem.Name = "openMeshFileToolStripMenuItem";
            this.openMeshFileToolStripMenuItem.Size = new System.Drawing.Size(193, 26);
            this.openMeshFileToolStripMenuItem.Text = "Mesh File...";
            this.openMeshFileToolStripMenuItem.Click += new System.EventHandler(this.openMeshFileToolStripMenuItem_Click);
            // 
            // openSelectionFileToolStripMenuItem
            // 
            this.openSelectionFileToolStripMenuItem.Name = "openSelectionFileToolStripMenuItem";
            this.openSelectionFileToolStripMenuItem.Size = new System.Drawing.Size(193, 26);
            this.openSelectionFileToolStripMenuItem.Text = "Selection File...";
            this.openSelectionFileToolStripMenuItem.Click += new System.EventHandler(this.openSelectionFileToolStripMenuItem_Click);
            // 
            // openCameraFileToolStripMenuItem
            // 
            this.openCameraFileToolStripMenuItem.Name = "openCameraFileToolStripMenuItem";
            this.openCameraFileToolStripMenuItem.Size = new System.Drawing.Size(193, 26);
            this.openCameraFileToolStripMenuItem.Text = "Camera File...";
            this.openCameraFileToolStripMenuItem.Click += new System.EventHandler(this.openCameraFileToolStripMenuItem_Click);
            // 
            // toolStripSplitButtonSaveFile
            // 
            this.toolStripSplitButtonSaveFile.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSplitButtonSaveFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveMeshFileToolStripMenuItem,
            this.saveSelectionFileToolStripMenuItem,
            this.saveCameraFileToolStripMenuItem});
            this.toolStripSplitButtonSaveFile.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButtonSaveFile.Image")));
            this.toolStripSplitButtonSaveFile.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSplitButtonSaveFile.Name = "toolStripSplitButtonSaveFile";
            this.toolStripSplitButtonSaveFile.Size = new System.Drawing.Size(39, 24);
            this.toolStripSplitButtonSaveFile.Text = "Save File";
            this.toolStripSplitButtonSaveFile.Visible = false;
            // 
            // saveMeshFileToolStripMenuItem
            // 
            this.saveMeshFileToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveMeshFileToolStripMenuItem.Image")));
            this.saveMeshFileToolStripMenuItem.Name = "saveMeshFileToolStripMenuItem";
            this.saveMeshFileToolStripMenuItem.Size = new System.Drawing.Size(193, 26);
            this.saveMeshFileToolStripMenuItem.Text = "Mesh File...";
            this.saveMeshFileToolStripMenuItem.Click += new System.EventHandler(this.saveMeshFileToolStripMenuItem_Click);
            // 
            // saveSelectionFileToolStripMenuItem
            // 
            this.saveSelectionFileToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveSelectionFileToolStripMenuItem.Image")));
            this.saveSelectionFileToolStripMenuItem.Name = "saveSelectionFileToolStripMenuItem";
            this.saveSelectionFileToolStripMenuItem.Size = new System.Drawing.Size(193, 26);
            this.saveSelectionFileToolStripMenuItem.Text = "Selection File...";
            this.saveSelectionFileToolStripMenuItem.Click += new System.EventHandler(this.saveSelectionFileToolStripMenuItem_Click);
            // 
            // saveCameraFileToolStripMenuItem
            // 
            this.saveCameraFileToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("saveCameraFileToolStripMenuItem.Image")));
            this.saveCameraFileToolStripMenuItem.Name = "saveCameraFileToolStripMenuItem";
            this.saveCameraFileToolStripMenuItem.Size = new System.Drawing.Size(193, 26);
            this.saveCameraFileToolStripMenuItem.Text = "Camera File...";
            this.saveCameraFileToolStripMenuItem.Click += new System.EventHandler(this.saveCameraFileToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(151, 24);
            this.toolStripButton2.Text = "Extract Skeleton";
            this.toolStripButton2.Click += new System.EventHandler(this.toolStripSplitButtonSkeletonization_ButtonClick);
            // 
            // toolStripButtonOpenSkeleton
            // 
            this.toolStripButtonOpenSkeleton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonOpenSkeleton.Image")));
            this.toolStripButtonOpenSkeleton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonOpenSkeleton.Name = "toolStripButtonOpenSkeleton";
            this.toolStripButtonOpenSkeleton.Size = new System.Drawing.Size(137, 24);
            this.toolStripButtonOpenSkeleton.Text = "OpenSkeleton";
            this.toolStripButtonOpenSkeleton.Click += new System.EventHandler(this.toolStripButtonOpenSkeleton_Click);
            // 
            // toolStripButtonSaveSkeleton
            // 
            this.toolStripButtonSaveSkeleton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSaveSkeleton.Image")));
            this.toolStripButtonSaveSkeleton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSaveSkeleton.Name = "toolStripButtonSaveSkeleton";
            this.toolStripButtonSaveSkeleton.Size = new System.Drawing.Size(135, 24);
            this.toolStripButtonSaveSkeleton.Text = "Save Skeleton";
            this.toolStripButtonSaveSkeleton.Click += new System.EventHandler(this.toolStripButtonSaveSkeleton_Click);
            // 
            // toolStripGetSplitSlicerButton
            // 
            this.toolStripGetSplitSlicerButton.Image = ((System.Drawing.Image)(resources.GetObject("toolStripGetSplitSlicerButton.Image")));
            this.toolStripGetSplitSlicerButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripGetSplitSlicerButton.Name = "toolStripGetSplitSlicerButton";
            this.toolStripGetSplitSlicerButton.Size = new System.Drawing.Size(66, 24);
            this.toolStripGetSplitSlicerButton.Text = "Split";
            this.toolStripGetSplitSlicerButton.ToolTipText = "toolStripGetSplitSlicerButton";
            this.toolStripGetSplitSlicerButton.Click += new System.EventHandler(this.toolStripGetSplitSlicerButton_Click);
            // 
            // toolStripButtonViewingTool
            // 
            this.toolStripButtonViewingTool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonViewingTool.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonViewingTool.Image")));
            this.toolStripButtonViewingTool.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonViewingTool.Name = "toolStripButtonViewingTool";
            this.toolStripButtonViewingTool.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonViewingTool.Text = "Viewing Tool";
            this.toolStripButtonViewingTool.Visible = false;
            this.toolStripButtonViewingTool.Click += new System.EventHandler(this.toolStripButtonViewingTool_Click);
            // 
            // toolStripButtonSelectionTool
            // 
            this.toolStripButtonSelectionTool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonSelectionTool.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSelectionTool.Image")));
            this.toolStripButtonSelectionTool.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonSelectionTool.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonSelectionTool.Name = "toolStripButtonSelectionTool";
            this.toolStripButtonSelectionTool.Size = new System.Drawing.Size(23, 24);
            this.toolStripButtonSelectionTool.Text = "Selection Tool";
            this.toolStripButtonSelectionTool.Visible = false;
            this.toolStripButtonSelectionTool.Click += new System.EventHandler(this.toolStripButtonSelectionTool_Click);
            // 
            // toolStripButtonMovingTool
            // 
            this.toolStripButtonMovingTool.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonMovingTool.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonMovingTool.Image")));
            this.toolStripButtonMovingTool.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripButtonMovingTool.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonMovingTool.Name = "toolStripButtonMovingTool";
            this.toolStripButtonMovingTool.Size = new System.Drawing.Size(23, 24);
            this.toolStripButtonMovingTool.Text = "Moving Tool";
            this.toolStripButtonMovingTool.Visible = false;
            this.toolStripButtonMovingTool.Click += new System.EventHandler(this.toolStripButtonMovingTool_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 27);
            this.toolStripSeparator3.Visible = false;
            // 
            // toolStripButtonAutoUpdate
            // 
            this.toolStripButtonAutoUpdate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonAutoUpdate.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonAutoUpdate.Image")));
            this.toolStripButtonAutoUpdate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonAutoUpdate.Name = "toolStripButtonAutoUpdate";
            this.toolStripButtonAutoUpdate.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonAutoUpdate.Text = "Auto Update";
            this.toolStripButtonAutoUpdate.Visible = false;
            this.toolStripButtonAutoUpdate.Click += new System.EventHandler(this.toolStripButtonAutoUpdate_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 27);
            this.toolStripSeparator2.Visible = false;
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 27);
            // 
            // toolStripButtonAbout
            // 
            this.toolStripButtonAbout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonAbout.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonAbout.Image")));
            this.toolStripButtonAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonAbout.Name = "toolStripButtonAbout";
            this.toolStripButtonAbout.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonAbout.Text = "About";
            this.toolStripButtonAbout.Visible = false;
            this.toolStripButtonAbout.Click += new System.EventHandler(this.toolStripButtonAbout_Click);
            // 
            // toolStripButtonExit
            // 
            this.toolStripButtonExit.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonExit.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonExit.Image")));
            this.toolStripButtonExit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonExit.Name = "toolStripButtonExit";
            this.toolStripButtonExit.Size = new System.Drawing.Size(24, 24);
            this.toolStripButtonExit.Text = "Exit";
            this.toolStripButtonExit.Click += new System.EventHandler(this.toolStripButtonExit_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer1.Size = new System.Drawing.Size(1093, 534);
            this.splitContainer1.SplitterDistance = 716;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 3;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(4);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.labelVertexCount);
            this.splitContainer2.Panel1.Controls.Add(this.buttonCloseTab);
            this.splitContainer2.Panel1.Controls.Add(this.buttonShowHideProperty);
            this.splitContainer2.Panel1.Controls.Add(this.buttonShowHideOutput);
            this.splitContainer2.Panel1.Controls.Add(this.tabControlModelList);
            this.splitContainer2.Panel1.Controls.Add(this.meshView1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.buttonClearOutputText);
            this.splitContainer2.Panel2.Controls.Add(this.textBoxOutput);
            this.splitContainer2.Panel2Collapsed = true;
            this.splitContainer2.Size = new System.Drawing.Size(716, 534);
            this.splitContainer2.SplitterDistance = 338;
            this.splitContainer2.SplitterWidth = 5;
            this.splitContainer2.TabIndex = 0;
            // 
            // labelVertexCount
            // 
            this.labelVertexCount.AutoSize = true;
            this.labelVertexCount.BackColor = System.Drawing.SystemColors.Control;
            this.labelVertexCount.Font = new System.Drawing.Font("Arial", 27.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelVertexCount.Location = new System.Drawing.Point(16, 41);
            this.labelVertexCount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelVertexCount.Name = "labelVertexCount";
            this.labelVertexCount.Size = new System.Drawing.Size(208, 53);
            this.labelVertexCount.TabIndex = 5;
            this.labelVertexCount.Text = "Vertex #:";
            // 
            // buttonCloseTab
            // 
            this.buttonCloseTab.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCloseTab.Image = ((System.Drawing.Image)(resources.GetObject("buttonCloseTab.Image")));
            this.buttonCloseTab.Location = new System.Drawing.Point(664, 29);
            this.buttonCloseTab.Margin = new System.Windows.Forms.Padding(4);
            this.buttonCloseTab.Name = "buttonCloseTab";
            this.buttonCloseTab.Size = new System.Drawing.Size(48, 28);
            this.buttonCloseTab.TabIndex = 4;
            this.buttonCloseTab.UseVisualStyleBackColor = true;
            this.buttonCloseTab.Click += new System.EventHandler(this.buttonCloseTab_Click);
            // 
            // buttonShowHideProperty
            // 
            this.buttonShowHideProperty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonShowHideProperty.Image = ((System.Drawing.Image)(resources.GetObject("buttonShowHideProperty.Image")));
            this.buttonShowHideProperty.Location = new System.Drawing.Point(664, 504);
            this.buttonShowHideProperty.Margin = new System.Windows.Forms.Padding(4);
            this.buttonShowHideProperty.Name = "buttonShowHideProperty";
            this.buttonShowHideProperty.Size = new System.Drawing.Size(48, 28);
            this.buttonShowHideProperty.TabIndex = 0;
            this.buttonShowHideProperty.UseVisualStyleBackColor = true;
            this.buttonShowHideProperty.Visible = false;
            this.buttonShowHideProperty.Click += new System.EventHandler(this.buttonShowHideProperty_Click);
            // 
            // buttonShowHideOutput
            // 
            this.buttonShowHideOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonShowHideOutput.Image = ((System.Drawing.Image)(resources.GetObject("buttonShowHideOutput.Image")));
            this.buttonShowHideOutput.Location = new System.Drawing.Point(608, 504);
            this.buttonShowHideOutput.Margin = new System.Windows.Forms.Padding(4);
            this.buttonShowHideOutput.Name = "buttonShowHideOutput";
            this.buttonShowHideOutput.Size = new System.Drawing.Size(48, 28);
            this.buttonShowHideOutput.TabIndex = 1;
            this.buttonShowHideOutput.UseVisualStyleBackColor = true;
            this.buttonShowHideOutput.Visible = false;
            this.buttonShowHideOutput.Click += new System.EventHandler(this.buttonShowHideOutput_Click);
            // 
            // tabControlModelList
            // 
            this.tabControlModelList.Dock = System.Windows.Forms.DockStyle.Top;
            this.tabControlModelList.Location = new System.Drawing.Point(0, 0);
            this.tabControlModelList.Margin = new System.Windows.Forms.Padding(4);
            this.tabControlModelList.Name = "tabControlModelList";
            this.tabControlModelList.SelectedIndex = 0;
            this.tabControlModelList.Size = new System.Drawing.Size(716, 26);
            this.tabControlModelList.TabIndex = 2;
            this.tabControlModelList.Selected += new System.Windows.Forms.TabControlEventHandler(this.tabControlModelList_Selected);
            // 
            // meshView1
            // 
            matrix4d1.Element = new double[] {
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D,
        0D};
            this.meshView1.CurrTransformation = matrix4d1;
            this.meshView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.meshView1.Location = new System.Drawing.Point(0, 0);
            this.meshView1.Margin = new System.Windows.Forms.Padding(4);
            this.meshView1.Name = "meshView1";
            this.meshView1.Size = new System.Drawing.Size(716, 534);
            this.meshView1.TabIndex = 3;
            this.meshView1.Text = "meshView1";
            // 
            // buttonClearOutputText
            // 
            this.buttonClearOutputText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonClearOutputText.Image = ((System.Drawing.Image)(resources.GetObject("buttonClearOutputText.Image")));
            this.buttonClearOutputText.Location = new System.Drawing.Point(1591, 71);
            this.buttonClearOutputText.Margin = new System.Windows.Forms.Padding(4);
            this.buttonClearOutputText.Name = "buttonClearOutputText";
            this.buttonClearOutputText.Size = new System.Drawing.Size(48, 28);
            this.buttonClearOutputText.TabIndex = 1;
            this.buttonClearOutputText.UseVisualStyleBackColor = true;
            this.buttonClearOutputText.Click += new System.EventHandler(this.buttonClearOutputText_Click);
            // 
            // textBoxOutput
            // 
            this.textBoxOutput.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxOutput.Location = new System.Drawing.Point(0, 0);
            this.textBoxOutput.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxOutput.Multiline = true;
            this.textBoxOutput.Name = "textBoxOutput";
            this.textBoxOutput.ReadOnly = true;
            this.textBoxOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxOutput.Size = new System.Drawing.Size(150, 46);
            this.textBoxOutput.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageModel);
            this.tabControl1.Controls.Add(this.tabPageDisplay);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(372, 534);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPageModel
            // 
            this.tabPageModel.Controls.Add(this.propertyGridModel);
            this.tabPageModel.Location = new System.Drawing.Point(4, 25);
            this.tabPageModel.Margin = new System.Windows.Forms.Padding(4);
            this.tabPageModel.Name = "tabPageModel";
            this.tabPageModel.Padding = new System.Windows.Forms.Padding(4);
            this.tabPageModel.Size = new System.Drawing.Size(364, 505);
            this.tabPageModel.TabIndex = 0;
            this.tabPageModel.Text = "Model";
            this.tabPageModel.UseVisualStyleBackColor = true;
            // 
            // propertyGridModel
            // 
            this.propertyGridModel.CommandsDisabledLinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.propertyGridModel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridModel.HelpVisible = false;
            this.propertyGridModel.Location = new System.Drawing.Point(4, 4);
            this.propertyGridModel.Margin = new System.Windows.Forms.Padding(4);
            this.propertyGridModel.Name = "propertyGridModel";
            this.propertyGridModel.Size = new System.Drawing.Size(356, 497);
            this.propertyGridModel.TabIndex = 0;
            // 
            // tabPageDisplay
            // 
            this.tabPageDisplay.Controls.Add(this.propertyGridDisplay);
            this.tabPageDisplay.Location = new System.Drawing.Point(4, 25);
            this.tabPageDisplay.Margin = new System.Windows.Forms.Padding(4);
            this.tabPageDisplay.Name = "tabPageDisplay";
            this.tabPageDisplay.Padding = new System.Windows.Forms.Padding(4);
            this.tabPageDisplay.Size = new System.Drawing.Size(355, 491);
            this.tabPageDisplay.TabIndex = 1;
            this.tabPageDisplay.Text = "Display";
            this.tabPageDisplay.UseVisualStyleBackColor = true;
            // 
            // propertyGridDisplay
            // 
            this.propertyGridDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridDisplay.HelpVisible = false;
            this.propertyGridDisplay.Location = new System.Drawing.Point(4, 4);
            this.propertyGridDisplay.Margin = new System.Windows.Forms.Padding(4);
            this.propertyGridDisplay.Name = "propertyGridDisplay";
            this.propertyGridDisplay.Size = new System.Drawing.Size(347, 483);
            this.propertyGridDisplay.TabIndex = 0;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // toolStripContainer1
            // 
            // 
            // toolStripContainer1.BottomToolStripPanel
            // 
            this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.statusStrip1);
            // 
            // toolStripContainer1.ContentPanel
            // 
            this.toolStripContainer1.ContentPanel.Controls.Add(this.splitContainer1);
            this.toolStripContainer1.ContentPanel.Margin = new System.Windows.Forms.Padding(4);
            this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(1093, 534);
            this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolStripContainer1.LeftToolStripPanelVisible = false;
            this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
            this.toolStripContainer1.Margin = new System.Windows.Forms.Padding(4);
            this.toolStripContainer1.Name = "toolStripContainer1";
            this.toolStripContainer1.RightToolStripPanelVisible = false;
            this.toolStripContainer1.Size = new System.Drawing.Size(1093, 586);
            this.toolStripContainer1.TabIndex = 4;
            this.toolStripContainer1.Text = "toolStripContainer1";
            // 
            // toolStripContainer1.TopToolStripPanel
            // 
            this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Dock = System.Windows.Forms.DockStyle.None;
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 0);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1093, 25);
            this.statusStrip1.TabIndex = 5;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(64, 20);
            this.toolStripStatusLabel1.Text = "[Ready]";
            // 
            // timer1
            // 
            this.timer1.Interval = 20;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // timer2
            // 
            this.timer2.Interval = 200;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1093, 586);
            this.Controls.Add(this.toolStripContainer1);
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormMain";
            this.Text = "Skeleton Extraction Demo";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormMain_FormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.FormMain_KeyDown);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPageModel.ResumeLayout(false);
            this.tabPageDisplay.ResumeLayout(false);
            this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
            this.toolStripContainer1.ContentPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
            this.toolStripContainer1.TopToolStripPanel.PerformLayout();
            this.toolStripContainer1.ResumeLayout(false);
            this.toolStripContainer1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageModel;
		private System.Windows.Forms.TabPage tabPageDisplay;
        private System.Windows.Forms.PropertyGrid propertyGridModel;
		private System.Windows.Forms.PropertyGrid propertyGridDisplay;
        private System.Windows.Forms.TextBox textBoxOutput;
        private System.Windows.Forms.Button buttonShowHideOutput;
		private System.Windows.Forms.Button buttonShowHideProperty;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton toolStripButtonViewingTool;
		private System.Windows.Forms.ToolStripButton toolStripButtonSelectionTool;
		private System.Windows.Forms.ToolStripButton toolStripButtonMovingTool;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Button buttonClearOutputText;
		private System.Windows.Forms.TabControl tabControlModelList;
		private MeshView meshView1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.Button buttonCloseTab;
		private System.Windows.Forms.Timer timer1;
		private System.Windows.Forms.ToolStripButton toolStripButtonAutoUpdate;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton toolStripButtonAbout;
		private System.Windows.Forms.ToolStripButton toolStripButtonExit;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.SaveFileDialog saveFileDialog1;
		private System.Windows.Forms.Label labelVertexCount;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripSplitButton toolStripSplitButtonOpen;
		private System.Windows.Forms.ToolStripMenuItem openMeshFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openSelectionFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openCameraFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripSplitButton toolStripSplitButtonSaveFile;
		private System.Windows.Forms.ToolStripMenuItem saveMeshFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveSelectionFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveCameraFileToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton toolStripButton2;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.Timer timer2;
		private System.Windows.Forms.ToolStripButton toolStripButtonSaveSkeleton;
        private System.Windows.Forms.ToolStripButton toolStripGetSplitSlicerButton;
        private System.Windows.Forms.ToolStripButton toolStripButtonOpenSkeleton;
    }
}


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Threading;
using CsGL.OpenGL;
using MyGeometry;

namespace IsolineEditing
{
    public partial class FormMain : Form
    {
		List<MeshRecord> meshes = new List<MeshRecord>();
		MeshRecord currentMeshRecord = null;
		Thread bgThread = null;
		delegate void SetTextCallback(string text);

        public FormMain()
        {
            InitializeComponent();

			this.propertyGridDisplay.SelectedObject = Program.displayProperty;
			//this.propertyGridTools.SelectedObject = Program.toolsProperty;
			toolStripButtonViewingTool.Checked = true;
			toolStripButtonSelectionTool.Checked = false;
			toolStripButtonMovingTool.Checked = false;
			this.labelVertexCount.Text = "";

        }
		public void PrintText(string s)
		{
			if (this.textBoxOutput.InvokeRequired)
			{
				SetTextCallback d = new SetTextCallback(PrintText);
				this.Invoke(d, new object[] { s });
			}
			else
			{
				this.textBoxOutput.AppendText(s + Environment.NewLine);
				this.toolStripStatusLabel1.Text = s;
			}
		}
		public void Print3DText(Vector3d pos, string s)
		{
			GL.glRasterPos3d(pos.x, pos.y, pos.z);
			GL.glPushAttrib(GL.GL_LIST_BIT);					// Pushes The Display List Bits
			GL.glListBase(this.meshView1.fontBase);							// Sets The Base Character to 32
			GL.glCallLists(s.Length, GL.GL_UNSIGNED_SHORT, s);	// Draws The Display List Text
			GL.glPopAttrib();									// Pops The Display List Bits
		}
		public void OpenMeshFile()
		{
			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "Mesh files (*.obj)|*.obj";
			openFileDialog1.CheckFileExists = true;

			DialogResult ret = openFileDialog1.ShowDialog(this);

			if (ret == DialogResult.OK)
			{
				StreamReader sr = new StreamReader(openFileDialog1.FileName);
				Mesh m = new Mesh(sr);
				sr.Close();
				MeshRecord rec = new MeshRecord(openFileDialog1.FileName, m);

				meshes.Add(rec);
				currentMeshRecord = rec;
				TabPage page = new TabPage(rec.ToString());
				page.Tag = rec;
				tabControlModelList.TabPages.Add(page);
				tabControlModelList.SelectedTab = page;
				meshView1.SetModel(rec);
				propertyGridModel.SelectedObject = rec;
				PrintText("Loaded mesh " + openFileDialog1.FileName);
			}
		}
		public void SaveMeshFile()
		{
			if (currentMeshRecord == null || currentMeshRecord.Mesh == null)
				return;

			saveFileDialog1.FileName = "";
			saveFileDialog1.Filter = "Mesh files (*.obj)|*.obj";
			saveFileDialog1.OverwritePrompt = true;

			DialogResult ret = saveFileDialog1.ShowDialog(this);

			if (ret == DialogResult.OK)
			{
				StreamWriter sw = new StreamWriter(saveFileDialog1.FileName);
				currentMeshRecord.Mesh.Write(sw);
				sw.Close();
				PrintText("Saved mesh " + saveFileDialog1.FileName + "\n");
			}
		}
		public void OpenCameraFile()
		{
			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "Camera files (*.camera)|*.camera";
			openFileDialog1.CheckFileExists = true;

			DialogResult ret = openFileDialog1.ShowDialog(this);
			if (ret == DialogResult.OK)
			{
				StreamReader sr = new StreamReader(openFileDialog1.FileName);
				XmlSerializer xs = new XmlSerializer(typeof(Matrix4d));
				meshView1.CurrTransformation = (Matrix4d)xs.Deserialize(sr);
				sr.Close();
				PrintText("Loaded camera " + openFileDialog1.FileName + "\n");
			}
		}
		public void SaveCameraFile()
		{
			saveFileDialog1.FileName = "";
			saveFileDialog1.Filter = "Camera files (*.camera)|*.camera";
			saveFileDialog1.OverwritePrompt = true;

			DialogResult ret = saveFileDialog1.ShowDialog(this);
			if (ret == DialogResult.OK)
			{
				StreamWriter sw = new StreamWriter(saveFileDialog1.FileName);
				XmlSerializer xs = new XmlSerializer(typeof(Matrix4d));
				xs.Serialize(sw, meshView1.CurrTransformation);
				sw.Close();
				PrintText("Saved camera " + saveFileDialog1.FileName + "\n");
			}
		}
		public void OpenSelectionFile()
		{
			if (currentMeshRecord == null || currentMeshRecord.Mesh == null)
				return;

			openFileDialog1.FileName = "";
			openFileDialog1.Filter = "Selection files (*.sel)|*.sel";
			openFileDialog1.CheckFileExists = true;

			DialogResult ret = openFileDialog1.ShowDialog(this);
			if (ret == DialogResult.OK)
			{
				StreamReader sr = new StreamReader(openFileDialog1.FileName);
				XmlSerializer xs = new XmlSerializer(typeof(byte[]));
				currentMeshRecord.Mesh.Flag = (byte[])xs.Deserialize(sr);
				sr.Close();
				PrintText("Loaded selection " + openFileDialog1.FileName + "\n");
			}
		}
		public void SaveSelectionFile()
		{
			if (currentMeshRecord == null || currentMeshRecord.Mesh == null)
				return;

			saveFileDialog1.FileName = "";
			saveFileDialog1.Filter = "Selection files (*.sel)|*.sel";
			saveFileDialog1.OverwritePrompt = true;

			DialogResult ret = saveFileDialog1.ShowDialog(this);
			if (ret == DialogResult.OK)
			{
				StreamWriter sw = new StreamWriter(saveFileDialog1.FileName);
				XmlSerializer xs = new XmlSerializer(typeof(byte[]));
				xs.Serialize(sw, currentMeshRecord.Mesh.Flag);
				sw.Close();
				PrintText("Saved selection " + saveFileDialog1.FileName + "\n");
			}
		}
		public void CloseTab()
		{
			if (tabControlModelList.SelectedTab != null)
			{
				tabControlModelList.TabPages.Remove(tabControlModelList.SelectedTab);
			}
		}
		public void FinishSkeletonization()
		{
			this.timer2.Enabled = false;
			Program.currentMode = Program.EnumOperationMode.Viewing;
			this.meshView1.Refresh();
		}

        public void RefreshMeshView()
        {
            this.meshView1.Refresh();
        }

		private void toolStripSplitButtonOpen_ButtonClick(object sender, EventArgs e)
		{
			OpenMeshFile();
		}
		private void toolStripButtonViewingTool_Click(object sender, EventArgs e)
		{
			toolStripButtonViewingTool.Checked = true;
			toolStripButtonSelectionTool.Checked = false;
			toolStripButtonMovingTool.Checked = false;
			Program.currentMode = Program.EnumOperationMode.Viewing;
		}
		private void toolStripButtonSelectionTool_Click(object sender, EventArgs e)
		{
			toolStripButtonViewingTool.Checked = false;
			toolStripButtonSelectionTool.Checked = true;
			toolStripButtonMovingTool.Checked = false;
			Program.currentMode = Program.EnumOperationMode.Selection;
		}
		private void toolStripButtonMovingTool_Click(object sender, EventArgs e)
		{
			toolStripButtonViewingTool.Checked = false;
			toolStripButtonSelectionTool.Checked = false;
			toolStripButtonMovingTool.Checked = true;
			Program.currentMode = Program.EnumOperationMode.Moving;
		}
		private void toolStripButtonAutoUpdate_Click(object sender, EventArgs e)
		{
			toolStripButtonAutoUpdate.Checked = !toolStripButtonAutoUpdate.Checked;
			this.timer1.Enabled = toolStripButtonAutoUpdate.Checked;
		}
		private void toolStripButtonAbout_Click(object sender, EventArgs e)
		{
			AboutBox1 box = new AboutBox1();
			box.ShowDialog(this);
		}
		private void toolStripButtonExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}
		private void toolStripSplitButtonSkeletonization_ButtonClick(object sender, EventArgs e)
		{
			if (currentMeshRecord != null)
			{
				Skeletonizer.Options opt = new Skeletonizer.Options();
				opt.LaplacianConstraintWeight = 1.0 / (10 * Math.Sqrt(currentMeshRecord.Mesh.AverageFaceArea()));
				FormInput f = new FormInput(opt);
				DialogResult ret = f.ShowDialog(this);
				if (ret == DialogResult.OK)
				{
					currentMeshRecord.Skeletonizer =
						new Skeletonizer(currentMeshRecord.Mesh, opt);
					ParameterizedThreadStart s = new ParameterizedThreadStart(ThreadStartSkeletonization);
					Thread newThread = new Thread(s);
					newThread.Priority = ThreadPriority.Lowest;    // 设置线程等级
					newThread.Start(currentMeshRecord.Skeletonizer);
					this.bgThread = newThread;

					Program.currentMode = Program.EnumOperationMode.None;
					this.timer2.Enabled = true;
				}
			}

		}
		private void ThreadStartSkeletonization(object data)
		{
			Skeletonizer s = data as Skeletonizer;
			if (s == null) return;
			s.Start();
		}
		private void openMeshFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenMeshFile();
		}
		private void openCameraFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenCameraFile();
		}
		private void openSelectionFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenSelectionFile();
		}
		private void saveMeshFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveMeshFile();
		}
		private void saveCameraFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveCameraFile();
		}
		private void saveSelectionFileToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveSelectionFile();
		}
		private void saveSegmentationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (currentMeshRecord == null ||
				currentMeshRecord.Mesh == null ||
				currentMeshRecord.Skeletonizer == null)
				return;

			saveFileDialog1.FileName = "";
			saveFileDialog1.Filter = "Mesh segmentation (*.seg)|*.seg";
			saveFileDialog1.OverwritePrompt = true;

			DialogResult ret = saveFileDialog1.ShowDialog(this);

			if (ret == DialogResult.OK)
			{
				StreamWriter sw = new StreamWriter(saveFileDialog1.FileName);
				currentMeshRecord.Skeletonizer.WriteSegmentation(sw);
				sw.Close();
				PrintText("Saved mesh segmentation " + saveFileDialog1.FileName + "\n");
			}

		}

		private void buttonShowHideOutput_Click(object sender, EventArgs e)
        {
			splitContainer2.Panel2Collapsed = !splitContainer2.Panel2Collapsed;
		}
        private void buttonShowHideProperty_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = ! splitContainer1.Panel2Collapsed;
        }
		private void buttonClearOutputText_Click(object sender, EventArgs e)
		{
			textBoxOutput.Clear();
		}
		private void buttonCloseTab_Click(object sender, EventArgs e)
		{
			CloseTab();
		}

		private void tabControlModelList_Selected(object sender, TabControlEventArgs e)
		{
			if (tabControlModelList.SelectedTab != null)
			{
				MeshRecord rec = (MeshRecord)tabControlModelList.SelectedTab.Tag;
				meshView1.SetModel(rec);
				propertyGridModel.SelectedObject = rec;
				currentMeshRecord = rec;
			}
			else
			{
				meshView1.SetModel(null);
				propertyGridModel.SelectedObject = null;
				currentMeshRecord = null;
			}
		}

		private void FormMain_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control)
			{
				switch (e.KeyCode)
				{
					case Keys.O: OpenMeshFile(); break;
				}
			}
			else
			{
				switch (e.KeyCode)
				{
					//case Keys.F1: OpenMeshFile(); break;
					case Keys.F2: Program.currentMode = Program.EnumOperationMode.Viewing; break;
					case Keys.F3: Program.currentMode = Program.EnumOperationMode.Selection; break;
					case Keys.F4: Program.currentMode = Program.EnumOperationMode.Moving; break;
                    case Keys.F8: Program.currentMode = Program.EnumOperationMode.PickSkeNode; break;
					case Keys.F12:
						if (currentMeshRecord != null &&
							currentMeshRecord.Deformer != null)
						{
							currentMeshRecord.Deformer.Deform();
							meshView1.Refresh();
						}
						break;
				}
			}
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			if (Program.currentMode == Program.EnumOperationMode.Moving &&
				toolStripButtonAutoUpdate.Checked &&
				currentMeshRecord != null && 
				currentMeshRecord.Deformer != null)
			{
				currentMeshRecord.Deformer.Deform();
				currentMeshRecord.Deformer.Update();
			}

			if (currentMeshRecord != null && currentMeshRecord.Skeletonizer != null &&
				currentMeshRecord.Skeletonizer.NodeCount != 0)
				this.labelVertexCount.Text = "Vertex #: " + currentMeshRecord.Skeletonizer.NodeCount;
			this.meshView1.Refresh();
			
		}


		private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
		{
			if (this.bgThread != null && this.bgThread.IsAlive)
				this.bgThread.Abort();

		}

		private void timer2_Tick(object sender, EventArgs e)
		{
			this.meshView1.Refresh();
		}

		private void toolStripButtonSaveSkeleton_Click(object sender, EventArgs e)
		{
			if (currentMeshRecord != null &&
				currentMeshRecord.Skeletonizer != null)
			{
				saveFileDialog1.FileName = "";
				saveFileDialog1.Filter = "Skeleton files (*.skeleton)|*.skeleton";
				saveFileDialog1.OverwritePrompt = true;

				DialogResult ret = saveFileDialog1.ShowDialog(this);

				if (ret == DialogResult.OK)
				{
					StreamWriter sw = new StreamWriter(saveFileDialog1.FileName);
					currentMeshRecord.Skeletonizer.WriteSkeleton(sw);
					sw.Close();
					PrintText("Saved skeleton " + saveFileDialog1.FileName + "\n");
				}
			}
		}

        private void toolStripGetSplitSlicerButton_Click(object sender, EventArgs e)
        {
           	if (currentMeshRecord != null &&
                currentMeshRecord.Skeletonizer != null)
            {
                currentMeshRecord.Segmentation = new Segmentation(currentMeshRecord.Skeletonizer, currentMeshRecord.Mesh);
                currentMeshRecord.Segmentation.SegPostProcess();
            }
            PrintText("Saved hexa ... " );
        }

        private void toolStripButtonOpenSkeleton_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = "";
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.Filter = "Skeleton files (*.skeleton)|*.skeleton";

            DialogResult ret = openFileDialog1.ShowDialog(this);

            if (ret == DialogResult.OK && currentMeshRecord !=null)
            {
                StreamReader sr = new StreamReader(openFileDialog1.FileName);
                if(currentMeshRecord.Skeletonizer == null)
                     currentMeshRecord.Skeletonizer = new Skeletonizer(sr, currentMeshRecord.Mesh);
                else currentMeshRecord.Skeletonizer.ReadSkeleton(sr, currentMeshRecord.Mesh);
                sr.Close();
                Program.displayProperty.MeshDisplayMode = DisplayProperty.EnumMeshDisplayMode.TransparentSmoothShaded;
                this.meshView1.Refresh();
                this.meshView1.skeHandleIndex = 1;
                PrintText("Read skeleton " + openFileDialog1.FileName + "\n");
            }
        }
    }
}
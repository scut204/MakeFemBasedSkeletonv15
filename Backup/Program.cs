using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading;
using MyGeometry;

namespace IsolineEditing
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

		public enum EnumOperationMode { Viewing, Selection, Moving, None }

		static public EnumOperationMode currentMode = EnumOperationMode.Viewing;
		static public DisplayProperty displayProperty = new DisplayProperty();
		static public ToolsProperty toolsProperty = new ToolsProperty();
		static public FormMain currentForm = null;
		
		[STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
			Program.currentForm = new FormMain();
			Application.Run(Program.currentForm);
        }

		static public void PrintText(string s)
		{
			FormMain f = FormMain.ActiveForm as FormMain;
			if (f != null)
				f.PrintText(s);
		}

		static public void Print3DText(Vector3d pos, string s)
		{
			FormMain f = FormMain.ActiveForm as FormMain;
			if (f != null)
				f.Print3DText(pos, s);
		}


    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace IsolineEditing
{
	public partial class FormInput : Form
	{
		public FormInput()
		{
			InitializeComponent();
		}
		public FormInput(object obj)
		{
			InitializeComponent();
			this.Text = obj.ToString();
			this.propertyGrid.SelectedObject = obj;
		}
	}
}
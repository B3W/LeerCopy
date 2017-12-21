using System;
using System.Drawing;
using System.Windows.Forms;

namespace Leer_Copy
{
    /// <summary>
    /// Author: Weston Berg (weberg@iastate.edu)
    /// Options form for Leer application
    /// </summary>
    public partial class FeaturesFrm : Form
    {
        // Properties to keep track of setting state
        public Double UserOpacity { get; private set; }
        public Color UserBackColor { get; private set; }
        public Brush UserBrushColor { get; private set; }
        public Color UserTxtColor { get; private set; }
        public Cursor UserCursor { get; private set; }
        public Boolean UserBorder { get; private set; }
        public Boolean UserTips { get; private set; }
        // Variables for main form to proccess
        public Boolean cancel { get; private set; }
        public Boolean isShown { get; private set; }
        public Boolean newLeer { get; private set; }

        private Label[] tipLabels;

        /// <summary>
        /// Constructor for the features window with initial values as parameters
        /// </summary>
        /// <param name="opacity"></param>
        /// <param name="backgroundColor"></param>
        /// <param name="brushColor"></param>
        /// <param name="cursorStyle"></param>
        /// <param name="addBorder"></param>
        /// <param name="showTips"></param>
        public FeaturesFrm(Double opacity, Color backgroundColor, Brush brushColor, Color txtColor,
                            Cursor cursorStyle, Boolean addBorder, Boolean showTips, Boolean showFeatureTips,
                            Size winSize)
        {
            InitializeComponent();
            this.KeyPreview = true;
            this.UserOpacity = opacity;
            this.UserBackColor = backgroundColor;
            this.UserBrushColor = brushColor;
            this.UserTxtColor = txtColor;
            this.UserCursor = cursorStyle;
            this.UserBorder = addBorder;
            this.UserTips = showTips;
            this.isShown = showFeatureTips;
            this.KeyUp += new KeyEventHandler(FeaturesFrm_KeyUp);
            this.SizeChanged += new EventHandler(FeaturesFrm_SizeChanged);
            this.tipLabels = new Label[] { fLbl, rLbl, cLbl, qLbl, tLbl};
            this.Size = winSize;
            // Configure control locations
            SetControlLocations();
            // Keep shown/hidden option from last opened session
            if (!isShown)
            {
                this.Size = new Size(this.Size.Width, 140);
                this.hideBtn.Location = new Point(hideBtn.Location.X, 50);
                this.hideBtn.Text = "SHOW TIPS";
                this.isShown = false;
                foreach (Label lbl in tipLabels)
                {
                    lbl.Visible = false;
                }
            }
        }
        // Event handlers for main form
        private void FeaturesFrm_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Q || e.KeyCode == Keys.Escape)
            {
                this.Hide();
            } else if(e.KeyCode == Keys.F)
            {
                this.featuresToolStripMenuItem.ShowDropDown();
            } else if(e.KeyCode == Keys.C)
            {
                DialogResult response = MessageBox.Show(this, "Are you sure you want to exit?", "Confirm", MessageBoxButtons.YesNo);
                if (response == DialogResult.Yes)
                {
                    this.cancel = true;
                    this.Hide();
                }
            } else if(e.KeyCode == Keys.R)
            {
                DialogResult response = MessageBox.Show(this, "Discard current Leer and create new?", "Confirm", MessageBoxButtons.YesNo);
                if (response == DialogResult.Yes)
                {
                    this.newLeer = true;
                    this.Hide();
                }
            } else if(e.KeyCode == Keys.T)
            {
                hideBtn.PerformClick();
            }
        } // FeaturesFrm_KeyUp

        private void FeaturesFrm_SizeChanged(object sender, EventArgs e)
        {
            SetControlLocations();
        } // FeaturesFrm_SizeChanged

        // Helper method to set the locations of controls centered horizontally
        private void SetControlLocations()
        {
            this.featuresMenu.Location = new Point((this.Width / 2) - ((featuresMenu.Width + 20) / 2), 0);
            int lblY = 60;
            foreach (Label lbl in tipLabels)
            {
                lbl.Location = new Point((this.Width / 2) - (lbl.Width / 2), lblY);
                lblY += 50;
            }
            this.hideBtn.Location = new Point((this.Width / 2) - (hideBtn.Width / 2), tLbl.Location.Y + 50);
        } // SetControlLocations

        // Opacity picker
        private void opacityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Boolean save = false;
            double selectedOpacity = this.UserOpacity;
            using (Form opacityPickerFrm = new Form
            { // Form properties
                Text = "Pick Opacity (" + Math.Round(this.UserOpacity, 2) + ")",
                StartPosition = FormStartPosition.CenterScreen,
                Size = new Size(400, 150),
                BackColor = this.BackColor,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                KeyPreview = true
            })
            { // Add TrackBar to form, anon event handlers, and show
                // Formatting
                TrackBar opacityTBar = new TrackBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    TickFrequency = 10,
                    Value = Convert.ToInt32(UserOpacity * 100),
                    BackColor = this.featuresToolStripMenuItem.BackColor,
                    Size = new Size(opacityPickerFrm.Size.Width, 60),
                    LargeChange = 0,
                    SmallChange = 0
                };
                Button saveBtn = new Button
                {
                    Text = "SAVE",
                    Size = new Size(100, 50),
                    Location = new Point((opacityPickerFrm.Size.Width / 2) - 50, opacityTBar.Size.Height),
                    BackColor = opacityTBar.BackColor,
                    FlatStyle = FlatStyle.Popup
                };
                // Anon event handlers
                // Form
                opacityPickerFrm.KeyUp += delegate (object frmSender, KeyEventArgs frmE)
                {
                    if (frmE.KeyCode == Keys.Enter || frmE.KeyCode == Keys.S)
                    {
                        save = true;
                        selectedOpacity = Convert.ToDouble(opacityTBar.Value / 100.0);
                        opacityPickerFrm.Dispose();
                    }
                    else if (frmE.KeyCode == Keys.Escape || frmE.KeyCode == Keys.Q)
                    {
                        save = false;
                        opacityPickerFrm.Dispose();
                    }
                };
                // TrackBar
                opacityTBar.Scroll += delegate (object tbarSender, EventArgs tbarE)
                {
                    opacityPickerFrm.Opacity = Convert.ToDouble(opacityTBar.Value / 100.0);
                    opacityPickerFrm.Text = "Pick Opacity (" + Convert.ToDouble(opacityTBar.Value / 100.0) + ")";
                };
                opacityTBar.MouseUp += delegate (object tbarSender2, MouseEventArgs tbarE2)
                {
                    opacityPickerFrm.Opacity = 1.0;
                };
                // Button
                saveBtn.Click += delegate (object btnSender, EventArgs btnE)
                {
                    save = true;
                    selectedOpacity = Convert.ToDouble(opacityTBar.Value / 100.0);
                    opacityPickerFrm.Dispose();
                };
                opacityPickerFrm.Controls.Add(opacityTBar);
                opacityPickerFrm.Controls.Add(saveBtn);
                opacityPickerFrm.ShowDialog();
            }
            if(save) { this.UserOpacity = selectedOpacity; }
        } // select opacity

        private void defaultOpacityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserOpacity = 0.35;
        } // default opacity

        // Cursor options
        private void crossToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserCursor = Cursors.Cross;
        } // cross

        private void arrowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserCursor = Cursors.Arrow;
        } // arrow

        private void handToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserCursor = Cursors.Hand;
        } // hand

        private void arrowCrossToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserCursor = Cursors.SizeAll;
        } // arrow cross

        private void iBeamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserCursor = Cursors.IBeam;
        } // ibeam

        private void defaultCursorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserCursor = Cursors.Cross;
        } // defaultCursor

        // Back Color and Brush Color pickers
        private void selectBackColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog backColDialog = new ColorDialog
            {
                AllowFullOpen = false,
                AnyColor = false,
                Color = Color.Gray
            };
            if(backColDialog.ShowDialog() == DialogResult.OK)
            {
                this.UserBackColor = Color.FromName(backColDialog.Color.Name);
            }
        } // selectBackColor

        private void defaultBackColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserBackColor = SystemColors.Control;
        } // defaultBackColor

        private void selectBrushColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog brushColDialog = new ColorDialog
            {
                AllowFullOpen = false,
                AnyColor = false,
                Color = Color.Red
            };
            if(brushColDialog.ShowDialog() == DialogResult.OK)
            {
                if(brushColDialog.Color.Name.Substring(0,2).ToLower().Equals("ff"))
                {
                    this.UserBrushColor = new SolidBrush(ColorTranslator.FromHtml("#" + brushColDialog.Color.Name));
                }
                else
                {
                    this.UserBrushColor = new SolidBrush(Color.FromName(brushColDialog.Color.Name));
                }
            }
        } // selectBrushColor

        private void defaultBrushColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserBrushColor = Brushes.Red;
        } // defaultBrushColor

        // Border and tip on/offs
        private void onBorderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserBorder = true;
        } // onBorder

        private void offBorderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserBorder = false;
        } // offBorder

        private void onTipsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserTips = true;
        } // onTips

        private void offTipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserTips = false;
        } // offTips

        private void selectTxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ColorDialog txtColDialog = new ColorDialog
            {
                AllowFullOpen = false,
                AnyColor = false,
                Color = Color.Black
            };
            if (txtColDialog.ShowDialog() == DialogResult.OK)
            {
                this.UserTxtColor = Color.FromName(txtColDialog.Color.Name);
            }
        } // select text color

        private void defaultTxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.UserTxtColor = Color.Black;
        } // default text color

        // New Leer
        private void newLeerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult response = MessageBox.Show(this, "Discard current Leer and create new?", "Confirm", MessageBoxButtons.YesNo);
            if (response == DialogResult.Yes)
            {
                this.newLeer = true;
                this.Hide();
            }
        }

        // Exits application
        private void cancelLeerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult response = MessageBox.Show(this, "Are you sure you want to exit?", "Confirm", MessageBoxButtons.YesNo);
            if (response == DialogResult.Yes)
            {
                this.cancel = true;
                this.Hide();
            }
        } // cancelLeer

        // Hides/shows tips
        private void hideBtn_Click(object sender, EventArgs e)
        {
            if (isShown)
            {
                this.Size = new Size(this.Size.Width, 140);
                this.hideBtn.Location = new Point(hideBtn.Location.X, 50);
                this.hideBtn.Text = "SHOW TIPS";
                this.isShown = false;
            } else
            {
                this.Size = new Size(this.Size.Width, 390);
                this.hideBtn.Location = new Point(hideBtn.Location.X, 300);
                this.hideBtn.Text = "HIDE TIPS";
                this.isShown = true;
            }
            foreach (Label lbl in this.tipLabels)
            {
                lbl.Visible = lbl.Visible ? false : true;
            }
        } // hideBtn_Click
    }
}

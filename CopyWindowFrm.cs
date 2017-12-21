using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Leer_Copy
{
    /// <summary>
    /// Author: Weston Berg (weberg@iastate.edu)
    /// Main form of Leer application
    /// </summary>
    public partial class CopyWindowFrm : Form
    {
        /// <summary>
        /// Points to keep track of where the mouse is on the screen
        /// </summary>
        private Point startPt;
        private Point currPt;
        /// <summary>
        /// Keeps track of wheter or not an area has been selected
        /// </summary>
        private Boolean isDrawn = false;
        private Boolean isDrawing = false;
        /// <summary>
        /// Indicates wheter to clear graphics on invalidate
        /// </summary>
        private Boolean clrGraphics = false;
        /// <summary>
        /// Bitmap of the screen
        /// </summary>
        private Bitmap screenIm;
        /// <summary>
        /// Displays area available for copying
        /// </summary>
        private PictureBox screenPicBox = new PictureBox();
        private PictureBox topPicLayer = new PictureBox();
        /// <summary>
        /// Transparent layer
        /// </summary>
        private Form transparentFrm;
        /// <summary>
        /// User selected background color
        /// </summary>
        private Color pbColor;
        /// <summary>
        /// User selected text color
        /// </summary>
        private Color txtColor;
        /// <summary>
        /// User selected 'transparency
        /// </summary>
        private Double userOpacity;
        /// <summary>
        /// User selects whether they would like a border or not
        /// </summary>
        private Boolean addBorder;
        /// <summary>
        /// User selected border color
        /// </summary>
        private Brush brushColor = Brushes.Red;
        /// <summary>
        /// User selected cursor style
        /// </summary>
        private Cursor userCursor;
        /// <summary>
        /// User toggles tip labels on or off. Originally set on startup.
        /// </summary>
        private Boolean showTips;
        /// <summary>
        /// User toggles tips on or off in features window
        /// </summary>
        private Boolean showFeatureTips;
        /// <summary>
        /// User defined size for the features window
        /// </summary>
        private Size featureWinSize;
        private Label[] tipLabels;
        /// <summary>
        /// Keeps track of last window state
        /// </summary>
        private FormWindowState lastWinState;
        /// <summary>
        /// Tells main program if restart is required
        /// </summary>
        public Boolean NeedsRestart { get; private set; }
        private Boolean showOnNewLeer;
        /// <summary>
        /// XML config file for saving user settings
        /// </summary>
        private static String configFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Leer Copy/leerConfig.xml";

        /// <summary>
        /// Constructor for main form of app
        /// </summary>
        public CopyWindowFrm()
        {
            InitializeComponent();
            Log("Entering constructor...");
            InitSettings();
            Log("Init settings completed.");
            lastWinState = FormWindowState.Maximized;
            // Display picture box
            screenPicBox.Location = new Point(0, 0);
            this.Controls.Add(screenPicBox);
            // Add transparent layer
            transparentFrm = new Form
            {
                Opacity = userOpacity,
                BackColor = Color.Wheat,
                Cursor = userCursor,
                KeyPreview = true,
                WindowState = FormWindowState.Maximized,
                FormBorderStyle = FormBorderStyle.None,
                TopMost = true,
                TransparencyKey = Color.Wheat
            };
            // Create picture box for drawing selected area
            topPicLayer.Size = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            topPicLayer.Location = new Point(0, 0);
            topPicLayer.BackColor = pbColor;
            AddContextMenu();
            transparentFrm.Controls.Add(topPicLayer);
            // Configure tip labels
            ConfigureTipLabels(showTips);
            // Set event handlers
            transparentFrm.KeyUp += new KeyEventHandler(TransparentFrm_KeyUp);
            transparentFrm.KeyDown += new KeyEventHandler(TransparentFrm_KeyDown);
            transparentFrm.Shown += new EventHandler(TransparentFrm_Shown);
            topPicLayer.MouseDown += new MouseEventHandler(TopPicLayer_MouseDown);
            topPicLayer.MouseUp += new MouseEventHandler(TopPicLayer_MouseUp);
            topPicLayer.MouseMove += new MouseEventHandler(TopPicLayer_MouseMove);
            topPicLayer.Paint += new PaintEventHandler(TopPicLayer_Paint);
            // Show transparent layer
            transparentFrm.Show(this);
            Log("Application starting...");
        } // CopyWindowFrm

        /// <summary>
        /// Processes key up events at the form level
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TransparentFrm_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Q || e.KeyCode == Keys.Escape)  // Exit application
            {
                this.Close();
            } else if(e.KeyCode == Keys.Z)  // Clear selection
            {
                clrGraphics = true;
                this.topPicLayer.Invalidate();
                this.isDrawn = false;
                this.isDrawing = false;
            } else if(e.KeyCode == Keys.T)  // Toggle label tips
            {
                foreach (Label lbl in this.tipLabels)
                {
                    lbl.Visible = lbl.Visible ? false : true;
                    this.showTips = lbl.Visible ? true : false;
                }
            } else if(e.KeyCode == Keys.R)  
            {
                this.transparentFrm.TopMost = false;
                DialogResult initResponse = MessageBox.Show(this, "Discard current Leer and create new?", "Confirm", MessageBoxButtons.YesNo);
                if(initResponse == DialogResult.Yes)
                {
                    this.NeedsRestart = true;
                    if (this.showOnNewLeer)
                    {
                        DialogResult response = MessageBox.Show(this, "Position window in new location and \nreopen application to start new Leer." +
                                                                "\n\nNever show message again?", "Reposition", MessageBoxButtons.YesNo);
                        if (response == DialogResult.Yes)
                        {
                            this.showOnNewLeer = false;
                        }
                    }
                    this.transparentFrm.Hide();
                    this.WindowState = FormWindowState.Minimized;
                    this.lastWinState = FormWindowState.Minimized;
                } else
                {
                    this.transparentFrm.TopMost = true;
                }
                
            } else if(e.KeyCode == Keys.F)  // Open features window
            {
                this.transparentFrm.TopMost = false;
                using (FeaturesFrm features = new FeaturesFrm(this.userOpacity, this.pbColor, this.brushColor, this.txtColor,
                                                                this.userCursor, this.addBorder, this.showTips, this.showFeatureTips,
                                                                this.featureWinSize))
                {
                    features.ShowDialog();
                    this.userOpacity = features.UserOpacity; // Opacity
                    this.transparentFrm.Opacity = this.userOpacity;
                    if(features.UserBackColor.Name.Substring(0,2).ToLower().Equals("ff")) // BackColor
                    {
                        this.pbColor = ColorTranslator.FromHtml("#" + features.UserBackColor.Name);
                    }
                    else
                    {
                        this.pbColor = features.UserBackColor;
                    }
                    this.topPicLayer.BackColor = this.pbColor;
                    this.txtColor = features.UserTxtColor; // Text Color
                    this.brushColor = features.UserBrushColor; // Brush Color
                    this.userCursor = features.UserCursor; // Cursor Style
                    this.transparentFrm.Cursor = this.userCursor;
                    this.addBorder = features.UserBorder; // Border
                    this.showTips = features.UserTips; // Tips
                    this.showFeatureTips = features.isShown; // Feature Tips
                    this.featureWinSize = features.Size; // Feature Size
                    if (features.cancel)
                    {
                        this.Close();
                    }
                    if(features.newLeer)
                    {
                        this.NeedsRestart = true;
                        if (this.showOnNewLeer)
                        {
                            DialogResult response = MessageBox.Show(this, "Position window in new location and \nreopen application to start new Leer." +
                                                                    "\n\nNever show message again?", "Reposition", MessageBoxButtons.YesNo);
                            if (response == DialogResult.Yes)
                            {
                                this.showOnNewLeer = false;
                            }
                        }
                        this.transparentFrm.Hide();
                        this.WindowState = FormWindowState.Minimized;
                        this.lastWinState = FormWindowState.Minimized;
                    }
                    foreach (Label lbl in this.tipLabels)
                    {
                        lbl.Visible = this.showTips ? true : false;
                        lbl.ForeColor = this.txtColor;
                    }
                    this.transparentFrm.TopMost = true;
                }
                this.topPicLayer.Invalidate();
            }
            else if(e.KeyCode == Keys.A)  // Select the entire screen
            {
                this.startPt = new Point(0, 0);
                this.currPt = new Point(Screen.PrimaryScreen.Bounds.Right, Screen.PrimaryScreen.Bounds.Bottom);
                this.isDrawing = false;
                this.isDrawn = true;
                this.topPicLayer.Invalidate();
            } else if(e.KeyCode == Keys.B)  // Toggle border on or off
            {
                this.addBorder = this.addBorder ? false : true;
                this.topPicLayer.Invalidate();
            } else {
                if (this.isDrawn == true && this.isDrawing == false)  // Make sure user has made a selection
                {
                    if (e.KeyCode == Keys.C)  // Copy selection to clipboard
                    {
                        Rectangle imagePortion = GetImagePortion(true);
                        using (Bitmap finalSelection = this.screenIm.Clone(imagePortion, screenIm.PixelFormat))
                        {
                            try
                            {
                                Clipboard.SetImage(finalSelection);
                            }
                            catch (ExternalException)
                            {
                                transparentFrm.TopMost = false;
                                MessageBox.Show(transparentFrm, "ERROR", "Image unable to be copied to clipboard." +
                                                                         "\nClipboard possibly being used by another process.");
                                transparentFrm.TopMost = true;
                            }
                        }
                    }
                    else if (e.KeyCode == Keys.S)  // Save selection as .png
                    {
                        Rectangle imagePortion = GetImagePortion(true);
                        using (Bitmap finalSelection = this.screenIm.Clone(imagePortion, screenIm.PixelFormat))
                        {
                            transparentFrm.TopMost = false;
                            SaveFileDialog saveFrm = new SaveFileDialog()
                            {
                                Title = "Save As",
                                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                                FileName = "Leer_" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Year,
                                Filter = "PNG (*.png)|*.png",
                                CheckPathExists = true
                            };
                            if (saveFrm.ShowDialog() == DialogResult.OK)
                            {
                                finalSelection.Save(saveFrm.FileName, System.Drawing.Imaging.ImageFormat.Png);
                            }
                            transparentFrm.TopMost = true;
                        }
                    }
                    else if (e.KeyCode == Keys.E)  // Send selection to image editor and exit
                    {
                        Rectangle imagePortion = GetImagePortion(true);
                        using (Bitmap finalSelection = this.screenIm.Clone(imagePortion, screenIm.PixelFormat))
                        {
                            transparentFrm.TopMost = false;
                            String file = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/leer_mspaint_temp_1592629.png";
                            finalSelection.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                            System.Diagnostics.ProcessStartInfo stInfo = new System.Diagnostics.ProcessStartInfo(file)
                            {
                                Verb = "edit"
                            };
                            try
                            {
                                System.Diagnostics.Process.Start(stInfo);
                                this.Dispose();
                            }
                            catch (Exception)
                            {
                                MessageBox.Show(transparentFrm, "ERROR", "Unable to start image editor. Please try again.");
                                transparentFrm.TopMost = true;
                            }
                        }
                    }
                    
                }
            }
        } // CopyWindowFrm_KeyUp

        /// <summary>
        /// Processes key down events at the form level
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TransparentFrm_KeyDown(object sender, KeyEventArgs e)
        {
            if(this.isDrawn == true && this.isDrawing == false)
            {  // Following are handlers for shifting selection via arrow keys
                int pixelShift = 1;
                if((ModifierKeys & Keys.Control) == Keys.Control)
                {
                    pixelShift = -1;
                }
                if((ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    pixelShift *= 3;
                }
                if (e.KeyCode == Keys.Up)
                {
                    if (startPt.Y < currPt.Y)
                    {
                        startPt.Y -= pixelShift;
                    }
                    else
                    {
                        currPt.Y -= pixelShift;
                    }
                    topPicLayer.Invalidate();
                }
                else if (e.KeyCode == Keys.Left)
                {
                    if (startPt.X < currPt.X)
                    {
                        startPt.X -= pixelShift;
                    }
                    else
                    {
                        currPt.X -= pixelShift;
                    }
                    topPicLayer.Invalidate();
                }
                else if (e.KeyCode == Keys.Down)
                {
                    if (startPt.Y < currPt.Y)
                    {
                        currPt.Y += pixelShift;
                    }
                    else
                    {
                        startPt.Y += pixelShift;
                    }
                    topPicLayer.Invalidate();
                }
                else if (e.KeyCode == Keys.Right)
                {
                    if (startPt.X > currPt.X)
                    {
                        startPt.X += pixelShift;
                    }
                    else
                    {
                        currPt.X += pixelShift;
                    }
                    topPicLayer.Invalidate();
                }
            }
        } // TransparentFrm_KeyDown

        /// <summary>
        /// Gives the transparent form focus to process key input
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TransparentFrm_Shown(object sender, EventArgs e)
        {
            this.transparentFrm.Activate();
        } // TransparentFrm_Shown

        /// <summary>
        /// Processes mouse clicks on the picture box. If area is already selected
        /// then the current position becomes the mouses location
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopPicLayer_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (this.isDrawn)
                {
                    this.currPt = e.Location;
                    this.topPicLayer.Invalidate();
                }
                else
                {
                    this.startPt = e.Location;
                    this.currPt = e.Location;
                }
                this.isDrawing = true;
            }
        } // CopyWindowFrm_MouseClick

        /// <summary>
        /// Handler for when the mouse is unclicked. If the mouse was not
        /// moved from when it was first clicked no image is copied.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopPicLayer_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!this.startPt.Equals(e.Location))
                {
                    this.isDrawn = true;
                }
                else
                {
                    this.isDrawn = false;
                }
                this.isDrawing = false;
            }
        } // CopyWindowFrm_MouseUp

        /// <summary>
        /// Shows the parts of the screen that have been selected as the mouse moves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopPicLayer_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.isDrawing)
            {
                this.currPt = e.Location;
                this.topPicLayer.Invalidate();
            }
        } // CopyWindowFrm_MouseMove

        /// <summary>
        /// Loads bitmap of the screen into memory before form is displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyWindowFrm_Load(object sender, EventArgs e)
        {
            Log("Application loading screen bitmap...");
            this.Hide();
            // Bitmap of screen
            screenIm = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                  Screen.PrimaryScreen.Bounds.Height,
                                  System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            // Create graphics object from bitmap
            Graphics bmpGraphics = Graphics.FromImage(screenIm as Image);
            // Screenshot the entire screen
            bmpGraphics.CopyFromScreen(0, 0, 0, 0, screenIm.Size, CopyPixelOperation.SourceCopy);
            // Save image into memory
            using (MemoryStream s = new MemoryStream())
            {
                screenIm.Save(s, System.Drawing.Imaging.ImageFormat.Bmp);
                screenPicBox.Size = new Size(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                screenPicBox.Image = Image.FromStream(s);
            }
            this.Show();
        } // CopyWindowFrm_Load

        /// <summary>
        /// Helps track when user reopens application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyWindowFrm_Resize(object sender, EventArgs e)
        {
            if(WindowState != this.lastWinState)
            {
                if(lastWinState == FormWindowState.Minimized && this.WindowState == FormWindowState.Maximized)
                {
                    this.Close();
                }
            }
        }

        /// <summary>
        /// Saves users settings to the config file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyWindowFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.transparentFrm.TopMost = false;
            Log("Application closing... saving settings to config.");
            XDocument configDoc = null;
            try
            {
                configDoc = XDocument.Load(configFile);
            }
            catch (FileNotFoundException)
            {
                // Create XML config document if none found
                XmlWriterSettings settings = new XmlWriterSettings()
                {
                    CloseOutput = true,
                    OmitXmlDeclaration = false,
                    Indent = true
                };
                XmlWriter writer = null;
                try
                {
                    writer = XmlWriter.Create(configFile, settings);
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Properties");
                    // Opacity
                    writer.WriteStartElement("Opacity");
                    writer.WriteEndElement();
                    // Background Color
                    writer.WriteStartElement("BackgroundColor");
                    writer.WriteEndElement();
                    // Text Color
                    writer.WriteStartElement("TextColor");
                    writer.WriteEndElement();
                    // Border
                    writer.WriteStartElement("Border");
                    writer.WriteEndElement();
                    // Brush color
                    writer.WriteStartElement("Brush");
                    writer.WriteEndElement();
                    // Cursor
                    writer.WriteStartElement("Cursor");
                    writer.WriteEndElement();
                    // Tips
                    writer.WriteStartElement("Tips");
                    writer.WriteEndElement();
                    // Feature Tips
                    writer.WriteStartElement("FeatureTips");
                    writer.WriteEndElement();
                    // Feature Window Width
                    writer.WriteStartElement("FeatureWidth");
                    writer.WriteEndElement();
                    // Feature Window Height
                    writer.WriteStartElement("FeatureHeight");
                    writer.WriteEndElement();
                    // Show message on new leer
                    writer.WriteStartElement("OnNewLeer");
                    writer.WriteEndElement();
                    // Close the document
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Fatal Error: " + ex.Message, "SEVERE");
                    this.Dispose();
                }
                finally
                {
                    // Close writer
                    writer.Flush();
                    writer.Close();
                }
                configDoc = XDocument.Load(configFile);
            }
            // Write new values to the config file with error checking
            // Opacity
            XElement opacNode = QueryConfig(configDoc, "Opacity");
            if(opacNode != null)
            {
                opacNode.SetValue(XmlConvert.ToString(this.userOpacity));
            } else
            {
                WriteNode(configDoc, "Opacity", XmlConvert.ToString(this.userOpacity));
            }
            // Background color
            XElement backColNode = QueryConfig(configDoc, "BackgroundColor");
            if (backColNode != null)
            {
                backColNode.SetValue(this.pbColor.Name);
            } else
            {
                WriteNode(configDoc, "BackgroundColor", this.pbColor.Name);
            }
            // Text Color
            XElement txtColNode = QueryConfig(configDoc, "TextColor");
            if (txtColNode != null)
            {
                txtColNode.SetValue(this.txtColor.Name);
            } else
            {
                WriteNode(configDoc, "TextColor", this.txtColor.Name);
            }
            // Border
            XElement borderNode = QueryConfig(configDoc, "Border");
            if (borderNode != null)
            {
                borderNode.SetValue(XmlConvert.ToString(this.addBorder));
            } else
            {
                WriteNode(configDoc, "Border", XmlConvert.ToString(this.addBorder));
            }
            // Border color
            XElement brushNode = QueryConfig(configDoc, "Brush");
            if (brushNode != null)
            {
                brushNode.SetValue(new Pen(this.brushColor).Color.Name);
            } else
            {
                WriteNode(configDoc, "Brush", new Pen(this.brushColor).Color.Name);
            }
            // Cursor style
            XElement cursorNode = QueryConfig(configDoc, "Cursor");
            string cursor = cursor = this.userCursor.ToString().Split(':')[1].Trim(); ;
            if (cursorNode != null)
            {
                cursorNode.SetValue(cursor.Substring(0, cursor.Length - 1));
            } else
            {
                WriteNode(configDoc, "Cursor", cursor);
            }
            // Show tips on start
            XElement tipNode = QueryConfig(configDoc, "Tips");
            if (tipNode != null)
            {
                tipNode.SetValue(XmlConvert.ToString(this.showTips));
            } else
            {
                WriteNode(configDoc, "Tips", XmlConvert.ToString(this.showTips));
            }
            // Show feature tips
            XElement featTipNode = QueryConfig(configDoc, "FeatureTips");
            if (featTipNode != null)
            {
                featTipNode.SetValue(XmlConvert.ToString(this.showFeatureTips));
            } else
            {
                WriteNode(configDoc, "FeatureTips", XmlConvert.ToString(this.showFeatureTips));
            }
            // Feature window width
            XElement featWidth = QueryConfig(configDoc, "FeatureWidth");
            if (featWidth != null)
            {
                featWidth.SetValue(XmlConvert.ToString(this.featureWinSize.Width));
            } else
            {
                WriteNode(configDoc, "FeatureWidth", XmlConvert.ToString(this.featureWinSize.Width));
            }
            // Feature window height
            XElement featHeight = QueryConfig(configDoc, "FeatureHeight");
            if(featHeight != null)
            {
                featHeight.SetValue(XmlConvert.ToString(this.featureWinSize.Height));
            } else
            {
                WriteNode(configDoc, "FeatureHeight", XmlConvert.ToString(this.featureWinSize.Height));
            }
            // Message on new leer
            XElement newLeerNode = QueryConfig(configDoc, "OnNewLeer");
            if (newLeerNode != null)
            {
                newLeerNode.SetValue(XmlConvert.ToString(this.showOnNewLeer));
            } else
            {
                WriteNode(configDoc, "OnNewLeer", XmlConvert.ToString(this.showOnNewLeer));
            }
            // Close document
            configDoc.Save(configFile);
            Log("Settings saved successfully. Exiting...");
            // Clear log file after 24 hours
            if (File.GetCreationTime(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Leer Copy/log.txt").Date.CompareTo(DateTime.Now.Date) < 0)
            {
                File.Create(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Leer Copy/log.txt").Close();
            }
            this.Dispose();
        }

        /// <summary>
        /// Paints portion of image that has been selected to the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TopPicLayer_Paint(object sender, PaintEventArgs e)
        {
            if (!clrGraphics)
            {
                // Draw to screen from bitmap image
                Rectangle imagePortion = GetImagePortion(false);
                e.Graphics.DrawImage(screenIm, startPt.X, startPt.Y,
                                    imagePortion, GraphicsUnit.Pixel);
                if (this.addBorder)
                {
                    int width = (currPt.X - startPt.X) + 4;
                    int height = (currPt.Y - startPt.Y) + 4;
                    Point loc = new Point();
                    int compareRes = ComparePoints(startPt, currPt);
                    if (compareRes == 4)
                    {
                        loc.X = startPt.X - 2;
                        loc.Y = startPt.Y - 2;
                    }
                    else if (compareRes == 1)
                    {
                        width = (startPt.X - currPt.X) + 4;
                        height = (startPt.Y - currPt.Y) + 4;
                        loc.X = currPt.X - 2;
                        loc.Y = currPt.Y - 2;
                    }
                    else if (compareRes == 2)
                    {
                        width = (currPt.X - startPt.X) + 4;
                        height = (startPt.Y - currPt.Y) + 4;
                        loc.X = startPt.X - 2;
                        loc.Y = currPt.Y - 2;
                    }
                    else if (compareRes == 3)
                    {
                        width = (startPt.X - currPt.X) + 4;
                        height = (currPt.Y - startPt.Y) + 4;
                        loc.X = currPt.X - 2;
                        loc.Y = startPt.Y - 2;
                    }
                    e.Graphics.DrawRectangle(new Pen(brushColor, 4), new Rectangle(loc, new Size(width, height)));
                }  // Add border around the selection
            } else
            {
                e.Graphics.Clear(pbColor);
                this.startPt = new Point();
                this.currPt = new Point();
                clrGraphics = false;
            }
            

        } // TopPicLayer_Paint

        /// <summary>
        /// Sets initial conditions for user defined variables using LINQ. If a node is not
        /// found variable will be set to default value
        /// </summary>
        private void InitSettings()
        {
            Log("Entering InitSettings function...");
            XDocument configDoc = null;
            try
            {
                configDoc = XDocument.Load(configFile);
                // Opacity
                XElement opacNode = QueryConfig(configDoc, "Opacity");
                if (opacNode != null)
                {
                    try
                    {
                        this.userOpacity = Convert.ToSingle(opacNode.Value);
                    }
                    catch (Exception)
                    {
                        this.userOpacity = 0.35;
                    }
                } else
                {
                    this.userOpacity = 0.35;
                }
                // Background color
                XElement backColNode = QueryConfig(configDoc, "BackgroundColor");
                if (backColNode != null)
                {
                    try
                    {
                        string tempBackColor = backColNode.Value;
                        if (!tempBackColor.Substring(0, 2).ToLower().Equals("ff"))
                        {
                            this.pbColor = Color.FromName(tempBackColor);
                        }
                        else
                        {
                            this.pbColor = ColorTranslator.FromHtml("#" + tempBackColor);
                        }
                    }
                    catch (Exception)
                    {
                        this.pbColor = SystemColors.Control;
                    }
                } else
                {
                    this.pbColor = SystemColors.Control;
                }
                // Text Color
                XElement txtColNode = QueryConfig(configDoc, "TextColor");
                if (txtColNode != null)
                {
                    try
                    {
                        string tempTxtColor = txtColNode.Value;
                        if (!tempTxtColor.Substring(0, 2).ToLower().Equals("ff"))
                        {
                            this.txtColor = Color.FromName(tempTxtColor);
                        }
                        else
                        {
                            this.txtColor = ColorTranslator.FromHtml("#" + tempTxtColor);
                        }
                    }
                    catch (Exception)
                    {
                        this.txtColor = Color.Black;
                    }
                } else
                {
                    this.txtColor = Color.Black;
                }
                // Border
                XElement borderNode = QueryConfig(configDoc, "Border");
                if (borderNode != null)
                {
                    if (borderNode.Value.ToLower().Equals("true")) this.addBorder = true;
                    else this.addBorder = false;
                } else
                {
                    this.addBorder = false;
                }
                // Brush color
                XElement brushNode = QueryConfig(configDoc, "Brush");
                if (brushNode != null)
                {
                    try
                    {
                        if (brushNode.Value.ToLower().Substring(0, 2).Equals("ff"))
                        {
                            this.brushColor = new SolidBrush(ColorTranslator.FromHtml("#" + brushNode.Value));
                        }
                        else
                        {
                            this.brushColor = new SolidBrush(Color.FromName(brushNode.Value));
                        }
                    }
                    catch (Exception)
                    {
                        this.brushColor = Brushes.Red;
                    }
                } else
                {
                    this.brushColor = Brushes.Red;
                }
                // Cursor style
                XElement cursorNode = QueryConfig(configDoc, "Cursor");
                if (cursorNode != null)
                {
                    try
                    {
                        this.userCursor = SelectCursor(cursorNode.Value);
                    }
                    catch (Exception)
                    {
                        this.userCursor = Cursors.Cross;
                    }
                } else
                {
                    this.userCursor = Cursors.Cross;
                }
                // Show tips on start
                XElement tipNode = QueryConfig(configDoc, "Tips");
                if(tipNode != null)
                {
                    if (tipNode.Value.ToLower().Equals("true")) this.showTips = true;
                    else this.showTips = false;
                } else
                {
                    this.showTips = true;
                }
                // Show feature window tips
                XElement featTipNode = QueryConfig(configDoc, "FeatureTips");
                if(featTipNode != null)
                {
                    if (featTipNode.Value.ToLower().Equals("true")) this.showFeatureTips = true;
                    else this.showFeatureTips = false;
                } else
                {
                    this.showFeatureTips = true;
                }
                // Feature window size
                XElement featWidth = QueryConfig(configDoc, "FeatureWidth");
                XElement featHeight = QueryConfig(configDoc, "FeatureHeight");
                if (featWidth != null && featHeight != null)
                {
                    try
                    {
                        this.featureWinSize = new Size(Convert.ToInt32(featWidth.Value), Convert.ToInt32(featHeight.Value));
                    }
                    catch (Exception)
                    {
                        this.featureWinSize = new Size(430, 390);
                    }
                } else
                {
                    this.featureWinSize = new Size(430, 390);
                }
                // Message on new leer
                XElement newLeerNode = QueryConfig(configDoc, "OnNewLeer");
                if(newLeerNode != null)
                {
                    if (newLeerNode.Value.ToLower().Equals("true")) this.showOnNewLeer = true;
                    else this.showOnNewLeer = false;
                } else
                {
                    this.showOnNewLeer = true;
                }
                // Close document
                configDoc.Save(configFile);
                Log("InitSettings function returning...");
            }
            catch (FileNotFoundException)
            {
                this.userOpacity = 0.35;
                this.pbColor = SystemColors.Control;
                this.brushColor = Brushes.Red;
                this.txtColor = Color.Black;
                this.addBorder = true;
                this.userCursor = Cursors.Cross;
                this.showTips = true;
                this.showFeatureTips = true;
                this.featureWinSize = new Size(430, 390);
                this.showOnNewLeer = true;
                Thread.Sleep(300);
                Log("InitSettings function returning with defaults...");
                return;
            }
        }

        // Helper method for determining selected area
        private Rectangle GetImagePortion(Boolean save)
        {
            int width = 0;
            int height = 0;
            Point loc = new Point();
            if (!save)
            {
                width = currPt.X - startPt.X;
                height = currPt.Y - startPt.Y;
                loc = startPt;
            }
            else
            {
                int compareRes = ComparePoints(startPt, currPt);
                if (compareRes == 4)
                {
                    width = currPt.X - startPt.X;
                    height = currPt.Y - startPt.Y;
                    loc = startPt;
                }
                else if (compareRes == 1)
                {
                    width = startPt.X - currPt.X;
                    height = startPt.Y - currPt.Y;
                    loc = new Point(currPt.X, currPt.Y);
                }
                else if (compareRes == 2)
                {
                    width = currPt.X - startPt.X;
                    height = startPt.Y - currPt.Y;
                    loc = new Point(startPt.X, currPt.Y);
                }
                else if (compareRes == 3)
                {
                    width = startPt.X - currPt.X;
                    height = currPt.Y - startPt.Y;
                    loc = new Point(currPt.X, startPt.Y);
                }
            }
            Size rectSize = new Size(width, height);
            return new Rectangle(loc, rectSize);
        } // GetImagePortion

        // Helper method for locating where points are in relation to each other
        private int ComparePoints(Point p1, Point p2)
        {
            if (p1.X > p2.X && p1.Y > p2.Y) // p2 to the upper left of p1
            {
                return 1;
            }
            else if (p1.X < p2.X && p1.Y > p2.Y) // p2 to the upper right of p1
            {
                return 2;
            }
            else if (p1.X > p2.X && p1.Y < p2.Y) // p2 to the lower left of p1
            {
                return 3;
            }
            else if (p1.X < p2.X && p1.Y < p2.Y) // p2 to the lower right of p1
            {
                return 4;
            }
            else
            {
                return -1;
            }
        } // ComparePoints

        // Copies the users selection to the clipboard in Context Menu
        private void CopySelection_Click(object sender, EventArgs e)
        {
            if (this.isDrawn == true && this.isDrawing == false && ComparePoints(this.startPt, this.currPt) != -1)  // Make sure user has made a selection
            {
                Rectangle imagePortion = GetImagePortion(true);
                using (Bitmap finalSelection = this.screenIm.Clone(imagePortion, screenIm.PixelFormat))
                {
                    try
                    {
                        Clipboard.SetImage(finalSelection);
                    }
                    catch (ExternalException)
                    {
                        transparentFrm.TopMost = false;
                        MessageBox.Show(transparentFrm, "ERROR", "Image unable to be copied to clipboard." +
                                                                 "\nClipboard possibly being used by another process.");
                        transparentFrm.TopMost = true;
                    }
                }
            }
        }

        // Saves users selection in Context Menu
        private void SaveSelection_Click(object sender, EventArgs e)
        {
            if (this.isDrawn == true && this.isDrawing == false && ComparePoints(this.startPt, this.currPt) != -1)  // Make sure user has made a selection
            {
                Rectangle imagePortion = GetImagePortion(true);
                using (Bitmap finalSelection = this.screenIm.Clone(imagePortion, screenIm.PixelFormat))
                {
                    transparentFrm.TopMost = false;
                    SaveFileDialog saveFrm = new SaveFileDialog()
                    {
                        Title = "Save As",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                        FileName = "Leer_" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Year,
                        Filter = "PNG (*.png)|*.png",
                        CheckPathExists = true
                    };
                    if (saveFrm.ShowDialog() == DialogResult.OK)
                    {
                        finalSelection.Save(saveFrm.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    transparentFrm.TopMost = true;
                }
            }
        }

        // Sends users selection to default editor in Context Menu
        private void EditSelection_Click(object sender, EventArgs e)
        {
            if (this.isDrawn == true && this.isDrawing == false && ComparePoints(this.startPt, this.currPt) != -1)  // Make sure user has made a selection
            {
                Rectangle imagePortion = GetImagePortion(true);
                using (Bitmap finalSelection = this.screenIm.Clone(imagePortion, screenIm.PixelFormat))
                {
                    transparentFrm.TopMost = false;
                    String file = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/leer_mspaint_temp_1592629.png";
                    finalSelection.Save(file, System.Drawing.Imaging.ImageFormat.Png);
                    System.Diagnostics.ProcessStartInfo stInfo = new System.Diagnostics.ProcessStartInfo(file)
                    {
                        Verb = "edit"
                    };
                    try
                    {
                        System.Diagnostics.Process.Start(stInfo);
                        this.Dispose();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(transparentFrm, "ERROR", "Unable to start image editor. Please try again.");
                        transparentFrm.TopMost = true;
                    }
                }
            }
        }
        
        // Sets the right click context menu for the top picture box
        private void AddContextMenu()
        {
            ContextMenu cmenu = new ContextMenu();
            cmenu.MenuItems.Add(new MenuItem("Copy", CopySelection_Click));
            cmenu.MenuItems.Add(new MenuItem("Edit", EditSelection_Click));
            cmenu.MenuItems.Add(new MenuItem("Save", SaveSelection_Click));
            MenuItem exit = new MenuItem("Exit");
            exit.Click += delegate (object sender, EventArgs e)
            {
                this.Close();
            };
            cmenu.MenuItems.Add(exit);
            topPicLayer.ContextMenu = cmenu;
        }

        // Helper method to enumerate over cursor options
        private Cursor SelectCursor(string cursor)
        {
            switch (cursor)
            {
                case "IBeam":
                    return Cursors.IBeam;
                case "Arrow":
                    return Cursors.Arrow;
                case "Cross":
                    return Cursors.Cross;
                case "SizeAll":
                    return Cursors.SizeAll;
                default:
                    return Cursors.Hand;
            }
        }

        // Configures tip labels on startup
        private void ConfigureTipLabels(Boolean show)
        {
            int topScreen = Screen.PrimaryScreen.Bounds.Top;
            int rightScreen = Screen.PrimaryScreen.Bounds.Right;
            Label Ebtn = new Label()
            {
                Text = "E - Edit with default editor",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 100)
            };
            Label Sbtn = new Label()
            {
                Text = "S - Save image to file",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 150)
            };
            Label Abtn = new Label()
            {
                Text = "A - Select entire screen",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 200)
            };
            Label Cbtn = new Label()
            {
                Text = "C - Copy to clipboard",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 250)
            };
            Label Zbtn = new Label()
            {
                Text = "Z - Clear selection",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 300)
            };
            Label Rbtn = new Label()
            {
                Text = "R - New Leer",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 350)
            };
            Label Tbtn = new Label()
            {
                Text = "T - Tips on/off",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 400)
            };
            Label Fbtn = new Label()
            {
                Text = "F - Features window",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 450)
            };
            Label Arrowkeys = new Label()
            {
                Text = "Arrow Keys resize selection",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 550)
            };
            Label Qbtn = new Label()
            {
                Text = "Q - Quit application",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 500)
            };
            this.tipLabels = new Label[] { Ebtn, Sbtn, Abtn, Cbtn, Zbtn, Rbtn, Tbtn, Fbtn, Arrowkeys, Qbtn};
            foreach (Label lbl in tipLabels)
            {
                this.topPicLayer.Controls.Add(lbl);
                if (!show)
                {
                    lbl.Visible = false;
                }
            }
        } // ConfigureTipLabels

        // Helper method for saving settings to and retrieving them from config file
        private XElement QueryConfig(XDocument doc, string query)
        {
            var genericQuery = from node in doc.Root.Descendants()
                               where node.Name.ToString().Equals(query)
                               select node;
            foreach (var item in genericQuery)
            {
                return item;
            }
            return null;
        }

        // Helper method for writing missing nodes to the config file
        private void WriteNode(XDocument doc, string node, string value)
        {
            XElement root = new XElement(node);
            root.SetValue(value);
            doc.Element("Properties").Add(root);
        }

        // Writes string to log file
        private void Log(string str)
        {
            // Add folder to AppData if not present
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Leer Copy"))
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Leer Copy");
            }
            using (StreamWriter w = File.AppendText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/Leer Copy/log.txt"))
            {
                try
                {
                    w.Write("\r\nLog Entry : ");
                    w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
                    w.WriteLine("  :");
                    w.WriteLine("  :{0}", str);
                    w.WriteLine("-------------------------------");
                }
                catch (Exception)
                {
                    // Continue execution of app even if logging does not work
                }
            }
        }
    }
}

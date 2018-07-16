// Copyright 2017 Weston Berg (westieberg@gmail.com)
//
// This file is part of Leer Copy.
//
// Leer Copy is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Leer Copy is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Leer Copy.  If not, see <http://www.gnu.org/licenses/>.

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
        /// Indicates whether to clear graphics on invalidate
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
        private Brush brushColor;
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
        /// <summary>
        /// Flag for determining if the user should be given instructions when starting new Leer
        /// </summary>
        private Boolean showOnNewLeer;
        // Fields containing user shortcut keys
        private Keys quitKey;
        private Keys clearKey;
        private Keys tipKey;
        private Keys newKey;
        private Keys featuresKey;
        private Keys selectAllKey;
        private Keys borderKey;
        private Keys copyKey;
        private Keys saveKey;
        private Keys printKey = Keys.P;
        private Keys editKey;
        private Keys viewKey;
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
            if(e.KeyCode == this.quitKey)  // Exit application
            {
                this.Close();
            } else if(e.KeyCode == this.clearKey)  // Clear selection
            {
                clrGraphics = true;
                this.topPicLayer.Invalidate();
                this.isDrawn = false;
                this.isDrawing = false;
            } else if(e.KeyCode == this.tipKey)  // Toggle label tips
            {
                foreach (Label lbl in this.tipLabels)
                {
                    lbl.Visible = lbl.Visible ? false : true;
                    this.showTips = lbl.Visible ? true : false;
                }
            } else if(e.KeyCode == this.newKey)  
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
                
            } else if(e.KeyCode == this.featuresKey)  // Open features window
            {
                OpenFeatures();
            }
            else if(e.KeyCode == this.selectAllKey)  // Select the entire screen
            {
                this.startPt = new Point(0, 0);
                this.currPt = new Point(Screen.PrimaryScreen.Bounds.Right, Screen.PrimaryScreen.Bounds.Bottom);
                this.isDrawing = false;
                this.isDrawn = true;
                this.topPicLayer.Invalidate();
            } else if(e.KeyCode == this.borderKey)  // Toggle border on or off
            {
                this.addBorder = this.addBorder ? false : true;
                this.topPicLayer.Invalidate();
            } else {
                if (this.isDrawn == true && this.isDrawing == false)  // Make sure user has made a selection
                {
                    if (e.KeyCode == this.copyKey)  // Copy selection to clipboard
                    {
                        CopySelection();
                    }
                    else if (e.KeyCode == this.saveKey)  // Save selection as .png
                    {
                        SaveSelection();
                    }
					else if (e.KeyCode == this.printKey)  // Print the selection
                    {
                        PrintSelection();
                    }
                    else if (e.KeyCode == this.editKey)  // Send selection to image editor and exit
                    {
                        EditSelection();
                    }
                    else if (e.KeyCode == this.viewKey) // View selection in a popup
                    {
                        ViewSelection();
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
        } // CopyWindowFrm_Resize

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
                    // Key Shortcuts
                    writer.WriteStartElement("QuitKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("ClearKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("TipKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("NewKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("FeaturesKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("SelectAllKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("BorderKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("CopyKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("SaveKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("EditKey");
                    writer.WriteEndElement();
                    writer.WriteStartElement("ViewKey");
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
            // Shortcut Keys
            XElement quitKeyNode = QueryConfig(configDoc, "QuitKey");
            if (quitKeyNode != null)
            {
                quitKeyNode.SetValue(XmlConvert.ToString((int)quitKey));
            } else
            {
                WriteNode(configDoc, "QuitKey", XmlConvert.ToString((int)quitKey));
            }
            XElement clearKeyNode = QueryConfig(configDoc, "ClearKey");
            if (clearKeyNode != null)
            {
                clearKeyNode.SetValue(XmlConvert.ToString((int)clearKey));
            }
            else
            {
                WriteNode(configDoc, "ClearKey", XmlConvert.ToString((int)clearKey));
            }
            XElement tipKeyNode = QueryConfig(configDoc, "TipKey");
            if (tipKeyNode != null)
            {
                tipKeyNode.SetValue(XmlConvert.ToString((int)tipKey));
            }
            else
            {
                WriteNode(configDoc, "TipKey", XmlConvert.ToString((int)tipKey));
            }
            XElement newKeyNode = QueryConfig(configDoc, "NewKey");
            if (newKeyNode != null)
            {
                newKeyNode.SetValue(XmlConvert.ToString((int)newKey));
            }
            else
            {
                WriteNode(configDoc, "NewKey", XmlConvert.ToString((int)newKey));
            }
            XElement featuresKeyNode = QueryConfig(configDoc, "FeaturesKey");
            if (featuresKeyNode != null)
            {
                featuresKeyNode.SetValue(XmlConvert.ToString((int)featuresKey));
            }
            else
            {
                WriteNode(configDoc, "FeaturesKey", XmlConvert.ToString((int)featuresKey));
            }
            XElement selectKeyNode = QueryConfig(configDoc, "SelectAllKey");
            if (selectKeyNode != null)
            {
                selectKeyNode.SetValue(XmlConvert.ToString((int)selectAllKey));
            }
            else
            {
                WriteNode(configDoc, "SelectAllKey", XmlConvert.ToString((int)selectAllKey));
            }
            XElement borderKeyNode = QueryConfig(configDoc, "BorderKey");
            if (borderKeyNode != null)
            {
                borderKeyNode.SetValue(XmlConvert.ToString((int)borderKey));
            }
            else
            {
                WriteNode(configDoc, "BorderKey", XmlConvert.ToString((int)borderKey));
            }
            XElement copyKeyNode = QueryConfig(configDoc, "CopyKey");
            if (copyKeyNode != null)
            {
                copyKeyNode.SetValue(XmlConvert.ToString((int)copyKey));
            }
            else
            {
                WriteNode(configDoc, "CopyKey", XmlConvert.ToString((int)copyKey));
            }
            XElement saveKeyNode = QueryConfig(configDoc, "SaveKey");
            if (saveKeyNode != null)
            {
                saveKeyNode.SetValue(XmlConvert.ToString((int)saveKey));
            }
            else
            {
                WriteNode(configDoc, "SaveKey", XmlConvert.ToString((int)saveKey));
            }
            XElement editKeyNode = QueryConfig(configDoc, "EditKey");
            if (editKeyNode != null)
            {
                editKeyNode.SetValue(XmlConvert.ToString((int)editKey));
            }
            else
            {
                WriteNode(configDoc, "EditKey", XmlConvert.ToString((int)editKey));
            }
            XElement viewKeyNode = QueryConfig(configDoc, "ViewKey");
            if (viewKeyNode != null)
            {
                viewKeyNode.SetValue(XmlConvert.ToString((int)viewKey));
            }
            else
            {
                WriteNode(configDoc, "ViewKey", XmlConvert.ToString((int)viewKey));
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
        } // CopyWindowFrm_FormClosing

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
                } 
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
                // Keys
                XElement quitKeyNode = QueryConfig(configDoc, "QuitKey");
                if (quitKeyNode != null)
                {
                    try
                    {
                        int.TryParse(quitKeyNode.Value, out int temp);
                        quitKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.quitKey = Keys.Q;
                    }
                }
                else
                {
                    this.quitKey = Keys.Q;
                }
                XElement clearKeyNode = QueryConfig(configDoc, "ClearKey");
                if (clearKeyNode != null)
                {
                    try
                    {
                        int.TryParse(clearKeyNode.Value, out int temp);
                        clearKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.clearKey = Keys.Z;
                    }
                }
                else
                {
                    this.clearKey = Keys.Z;
                }
                XElement tipKeyNode = QueryConfig(configDoc, "TipKey");
                if (tipKeyNode != null)
                {
                    try
                    {
                        int.TryParse(tipKeyNode.Value, out int temp);
                        tipKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.tipKey = Keys.T;
                    }
                }
                else
                {
                    this.tipKey = Keys.T;
                }
                XElement newKeyNode = QueryConfig(configDoc, "NewKey");
                if (newKeyNode != null)
                {
                    try
                    {
                        int.TryParse(newKeyNode.Value, out int temp);
                        newKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.newKey = Keys.R;
                    }
                }
                else
                {
                    this.newKey = Keys.R;
                }
                XElement featuresKeyNode = QueryConfig(configDoc, "FeaturesKey");
                if (featuresKeyNode != null)
                {
                    try
                    {
                        int.TryParse(featuresKeyNode.Value, out int temp);
                        featuresKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.featuresKey = Keys.F;
                    }
                }
                else
                {
                    this.featuresKey = Keys.F;
                }
                XElement selectKeyNode = QueryConfig(configDoc, "SelectAllKey");
                if (selectKeyNode != null)
                {
                    try
                    {
                        int.TryParse(selectKeyNode.Value, out int temp);
                        selectAllKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.selectAllKey = Keys.A;
                    }
                }
                else
                {
                    this.selectAllKey = Keys.A;
                }
                XElement borderKeyNode = QueryConfig(configDoc, "BorderKey");
                if (borderKeyNode != null)
                {
                    try
                    {
                        int.TryParse(borderKeyNode.Value, out int temp);
                        borderKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.borderKey = Keys.B;
                    }
                }
                else
                {
                    this.borderKey = Keys.B;
                }
                XElement copyKeyNode = QueryConfig(configDoc, "CopyKey");
                if (copyKeyNode != null)
                {
                    try
                    {
                        int.TryParse(copyKeyNode.Value, out int temp);
                        copyKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.copyKey = Keys.C;
                    }
                }
                else
                {
                    this.copyKey = Keys.C;
                }
                XElement saveKeyNode = QueryConfig(configDoc, "SaveKey");
                if (saveKeyNode != null)
                {
                    try
                    {
                        int.TryParse(saveKeyNode.Value, out int temp);
                        saveKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.saveKey = Keys.S;
                    }
                }
                else
                {
                    this.saveKey = Keys.S;
                }
                XElement editKeyNode = QueryConfig(configDoc, "EditKey");
                if (editKeyNode != null)
                {
                    try
                    {
                        int.TryParse(editKeyNode.Value, out int temp);
                        editKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.editKey = Keys.E;
                    }
                }
                else
                {
                    this.editKey = Keys.E;
                }
                XElement viewKeyNode = QueryConfig(configDoc, "ViewKey");
                if (viewKeyNode != null)
                {
                    try
                    {
                        int.TryParse(viewKeyNode.Value, out int temp);
                        viewKey = (Keys)temp;
                    }
                    catch (Exception)
                    {
                        this.viewKey = Keys.V;
                    }
                }
                else
                {
                    this.viewKey = Keys.V;
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
                this.quitKey = Keys.Q;
                this.clearKey = Keys.Z;
                this.tipKey = Keys.T;
                this.newKey = Keys.R;
                this.featuresKey = Keys.F;
                this.selectAllKey = Keys.A;
                this.borderKey = Keys.B;
                this.copyKey = Keys.C;
                this.saveKey = Keys.S;
                this.editKey = Keys.E;
                this.viewKey = Keys.V;
                Thread.Sleep(300);
                Log("InitSettings function returning with defaults...");
                return;
            }
        } // InitSettings

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
            CopySelection();
        } // CopySelection_Click

        // Saves users selection in Context Menu
        private void SaveSelection_Click(object sender, EventArgs e)
        {
            if (this.isDrawn == true && this.isDrawing == false && ComparePoints(this.startPt, this.currPt) != -1)  // Make sure user has made a selection
            {
                SaveSelection();
            }
        } // SaveSelection_Click

		// Prints user's selection
        private void PrintSelection_Click(object sender, EventArgs e)
        {
            PrintSelection();
        } // PrintSelection_Click
		
        // Sends users selection to default editor in Context Menu
        private void EditSelection_Click(object sender, EventArgs e)
        {
            if (this.isDrawn == true && this.isDrawing == false && ComparePoints(this.startPt, this.currPt) != -1)  // Make sure user has made a selection
            {
                EditSelection();
            }
        } // EditSelection_Click

        // Sends user to viewing window in Context Menu
        private void ViewSelection_Click(object sender, EventArgs e)
        {
            ViewSelection();
        }

        // Sends user to features window in Context Menu
        private void Settings_Click(object sender, EventArgs e)
        {
            OpenFeatures();
        } // Settings_Click

        // Funtion for editing in default editor
        private void EditSelection()
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
                    this.Close();
                }
                catch (Exception)
                {
                    MessageBox.Show(transparentFrm, "ERROR", "Unable to start image editor. Please try again.");
                    transparentFrm.TopMost = true;
                }
            }
        } // EditSelection

        // Function for saving to PNG
        private void SaveSelection()
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
        } // SaveSelection

		// Function for printing the selection
        private void PrintSelection()
        {
            if (this.isDrawn == true && this.isDrawing == false && ComparePoints(this.startPt, this.currPt) != -1)  // Make sure user has made a selection
            {
                this.transparentFrm.TopMost = false;

                PrintDocument printDoc = new PrintDocument();    // Prepare the print document
                printDoc.PrintPage += PrintPage;
                PrintDialog printDialog = new PrintDialog        // Configure print dialog with print document
                {
                    Document = printDoc,
                    UseEXDialog = true
                };
                if (printDialog.ShowDialog() == DialogResult.OK)  // Confirm User wants to print
                {
                    printDoc.Print();
                }
                this.transparentFrm.TopMost = true;
            }
        } // PrintSelection

        // Helper method for getting the bitmap selection to print
        private void PrintPage(object obj, PrintPageEventArgs e)
        {
            Rectangle imagePortion = GetImagePortion(true);
            using (Bitmap finalSelection = this.screenIm.Clone(imagePortion, screenIm.PixelFormat))  // Print selected bitmap
            {
                Rectangle pgBounds = e.PageBounds;  // Make sure the selection fits on the page
                Size selectionSize = ResizeFit(new Size(finalSelection.Width, finalSelection.Height), new Size(pgBounds.Width, pgBounds.Height));   // Resize if necessary
                
                e.Graphics.DrawImage(finalSelection, new Rectangle(0, 0, selectionSize.Width, selectionSize.Height));
            }
        } // PrintPage

        // Method for resizing selection when necessary
        // Helped by jbc @ https://stackoverflow.com/a/17197425
        private Size ResizeFit(Size originalSize, Size maxSize)
        {
            var widthRatio = (double)maxSize.Width / (double)originalSize.Width;
            var heightRatio = (double)maxSize.Height / (double)originalSize.Height;
            var minAspectRatio = Math.Min(widthRatio, heightRatio);
            if (minAspectRatio > 1) // Resize by the minimum aspect ration if necessary
            {
                return originalSize;
            }
            return new Size((int)(originalSize.Width * minAspectRatio), (int)(originalSize.Height * minAspectRatio));
        } // ResizeFit
		
        // Function for saving to clipboard
        private void CopySelection()
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
        } // CopySelection

        // Function for viewing selection in popup window
        private void ViewSelection()
        {
            if (this.isDrawn == true && this.isDrawing == false && ComparePoints(this.startPt, this.currPt) != -1)  // Make sure user has made a selection
            {
                this.transparentFrm.TopMost = false;
                Rectangle imagePortion = GetImagePortion(true);
                using (Bitmap viewSelection = this.screenIm.Clone(imagePortion, this.screenIm.PixelFormat))
                {
                    using (Form viewFrm = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        StartPosition = FormStartPosition.CenterScreen,
                        Size = new Size(imagePortion.Size.Width + 15, imagePortion.Size.Height + 15),
                        BackColor = Color.White,
                        Icon = this.Icon,
                    })
                    {
                        viewFrm.KeyUp += delegate (object sender, KeyEventArgs e) // Key handler for closing the popup
                        {
                            if (e.KeyCode == this.viewKey || e.KeyCode == Keys.Escape)
                            {
                                viewFrm.Hide();
                            }
                        };
                        // Check selection size
                        Size viewSize = imagePortion.Size;
                        if (viewFrm.Size.Width >= Screen.PrimaryScreen.Bounds.Width && viewFrm.Size.Height >= Screen.PrimaryScreen.Bounds.Height) 
                        {
                            viewFrm.Size = Screen.PrimaryScreen.Bounds.Size;
                        }
                        else if (viewFrm.Size.Width >= Screen.PrimaryScreen.Bounds.Width)
                        {
                            viewFrm.Width = Screen.PrimaryScreen.Bounds.Width;
                        }
                        else if (viewFrm.Size.Height >= Screen.PrimaryScreen.Bounds.Height)
                        {
                            viewFrm.Height = Screen.PrimaryScreen.Bounds.Height;
                        }
                        PictureBox viewPB = new PictureBox
                        {
                            Image = viewSelection,
                            Size = viewSize
                        };
                        viewPB.Location = new Point((viewFrm.Width / 2) - (viewPB.Width / 2), (viewFrm.Height / 2) - (viewPB.Height / 2));
                        viewFrm.Controls.Add(viewPB);
                        // Darken background
                        using (Panel p = new Panel
                        {
                            BackColor = Color.Black,
                            Size = this.transparentFrm.Size
                        })
                        {
                            this.transparentFrm.Controls.Add(p);
                            p.BringToFront();
                            viewFrm.ShowDialog();
                        }
                    }
                }
                this.transparentFrm.TopMost = true;
            }
        } // ViewSelection

        // Function to open the features window
        private void OpenFeatures()
        {
            this.transparentFrm.TopMost = false;
            using (FeaturesFrm features = new FeaturesFrm(this.userOpacity, this.pbColor, this.brushColor, this.txtColor,
                                                            this.userCursor, this.addBorder, this.showTips, this.showFeatureTips,
                                                            this.featureWinSize, new Keys[] { quitKey, clearKey, tipKey, newKey,
                                                            featuresKey, selectAllKey, borderKey, copyKey, saveKey, editKey, viewKey }))
            {
                features.ShowDialog();
                this.userOpacity = features.UserOpacity; // Opacity
                this.transparentFrm.Opacity = this.userOpacity;
                if (features.UserBackColor.Name.Substring(0, 2).ToLower().Equals("ff")) // BackColor
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
                // SET KEYS
                this.quitKey = features.QuitKey;
                this.clearKey = features.ClearKey;
                this.tipKey = features.TipKey;
                this.newKey = features.NewKey;
                this.featuresKey = features.FeaturesKey;
                this.selectAllKey = features.SelectAllKey;
                this.borderKey = features.BorderKey;
                this.copyKey = features.CopyKey;
                this.saveKey = features.SaveKey;
                this.editKey = features.EditKey;
                this.viewKey = features.ViewKey;
                if (features.cancel)
                {
                    this.Close();
                }
                if (features.newLeer)
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
                ConfigureTipLabelTxt();
                foreach (Label lbl in this.tipLabels)
                {
                    lbl.Visible = this.showTips ? true : false;
                    lbl.ForeColor = this.txtColor;
                }
                this.transparentFrm.TopMost = true;
            }
            this.topPicLayer.Invalidate();
        } // OpenFeatures
        
        // Sets the right click context menu for the top picture box
        private void AddContextMenu()
        {
            ContextMenu cmenu = new ContextMenu();
            cmenu.MenuItems.Add(new MenuItem("Copy", CopySelection_Click));
            cmenu.MenuItems.Add(new MenuItem("Edit", EditSelection_Click));
            cmenu.MenuItems.Add(new MenuItem("Save", SaveSelection_Click));
            cmenu.MenuItems.Add(new MenuItem("View", ViewSelection_Click));
			cmenu.MenuItems.Add(new MenuItem("Print", PrintSelection_Click));
            cmenu.MenuItems.Add(new MenuItem("Settings", Settings_Click));
            MenuItem exit = new MenuItem("Exit");
            exit.Click += delegate (object sender, EventArgs e)
            {
                this.Close();
            };
            cmenu.MenuItems.Add(exit);
            topPicLayer.ContextMenu = cmenu;
        } // AddContextMenu

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
        } // SelectCursor

        // Configures tip labels on startup
        private void ConfigureTipLabels(Boolean show)
        {
            int topScreen = Screen.PrimaryScreen.Bounds.Top;
            int rightScreen = Screen.PrimaryScreen.Bounds.Right;
            Label Ebtn = new Label()
            {
                Text = this.editKey + " - Edit with default editor",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 100)
            };
            Label Sbtn = new Label()
            {
                Text = this.saveKey + " - Save image to file",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 150)
            };
            Label Abtn = new Label()
            {
                Text = this.selectAllKey + " - Select entire screen",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 200)
            };
            Label Cbtn = new Label()
            {
                Text = this.copyKey + " - Copy to clipboard",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 250)
            };
            Label Zbtn = new Label()
            {
                Text = this.clearKey + " - Clear selection",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 350)
            };
            Label Rbtn = new Label()
            {
                Text = this.newKey + " - New Leer",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 400)
            };
            Label Tbtn = new Label()
            {
                Text = this.tipKey + " - Tips on/off",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 450)
            };
            Label Fbtn = new Label()
            {
                Text = this.featuresKey + " - Features window",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 500)
            };
            Label Arrowkeys = new Label()
            {
                Text = "Arrow Keys resize selection",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 600)
            };
            Label Qbtn = new Label()
            {
                Text = this.quitKey + " - Quit application",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 550)
            };
            Label Vbtn = new Label()
            {
                Text = this.viewKey + " - View selection",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericSansSerif, 12),
                ForeColor = txtColor,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(rightScreen - 300, 300)
            };
            this.tipLabels = new Label[] { Ebtn, Sbtn, Abtn, Cbtn, Zbtn, Rbtn, Tbtn, Fbtn, Arrowkeys, Qbtn, Vbtn};
            foreach (Label lbl in tipLabels)
            {
                this.topPicLayer.Controls.Add(lbl);
                if (!show)
                {
                    lbl.Visible = false;
                }
            }
        } // ConfigureTipLabels

        // Configure Tip Label Text when shortcut keys changed
        private void ConfigureTipLabelTxt()
        {
            tipLabels[0].Text = this.editKey + " - Edit with default editor";
            tipLabels[1].Text = this.saveKey + " - Save image to file";
            tipLabels[2].Text = this.selectAllKey + " - Select entire screen";
            tipLabels[3].Text = this.copyKey + " - Copy to clipboard";
            tipLabels[4].Text = this.clearKey + " - Clear selection";
            tipLabels[5].Text = this.newKey + " - New Leer";
            tipLabels[6].Text = this.tipKey + " - Tips on/off";
            tipLabels[7].Text = this.featuresKey + " - Features window";
            tipLabels[9].Text = this.quitKey + " - Quit application";
            tipLabels[10].Text = this.viewKey + " - View selection";
        }

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
        } // QueryConfig

        // Helper method for writing missing nodes to the config file
        private void WriteNode(XDocument doc, string node, string value)
        {
            XElement root = new XElement(node);
            root.SetValue(value);
            doc.Element("Properties").Add(root);
        } // WriteNode

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
        } // Log
    }
}

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
using System.Linq;
using System.Windows.Forms;

namespace Leer_Copy
{
    public partial class KeyShortcutFrm : Form
    {
        //Keeps track of all the keys for validation
        public Keys[] KeySet { get; private set; }

        public KeyShortcutFrm(Keys quitKey, Keys clearKey, Keys tipKey, Keys newKey, Keys featuresKey,
                                Keys selectAllKey, Keys borderKey, Keys copyKey, Keys saveKey, Keys editKey, Keys viewKey)
        {
            InitializeComponent();
            quitTextBox.Text = quitKey.ToString();
            clearTextBox.Text = clearKey.ToString();
            tipTextBox.Text = tipKey.ToString();
            newTextBox.Text = newKey.ToString();
            featureTextBox.Text = featuresKey.ToString();
            selectTextBox.Text = selectAllKey.ToString();
            borderTextBox.Text = borderKey.ToString();
            copyTextBox.Text = copyKey.ToString();
            saveTextBox.Text = saveKey.ToString();
            editTextBox.Text = editKey.ToString();
            viewTextBox.Text = viewKey.ToString();
            KeySet = new Keys[] { quitKey, clearKey, tipKey, newKey, featuresKey, selectAllKey, borderKey, copyKey, saveKey, editKey, viewKey };
        }

        // Handler for key input in text boxes
        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox curTB = (TextBox)sender;
            Keys curKey = (Keys)Enum.Parse(typeof(Keys), curTB.Text);
            string keyStr = e.KeyCode.ToString();
            Keys key = (Keys)Enum.Parse(typeof(Keys), keyStr);
            if (key != Keys.None && !MediaKey(key))
            {
                if (ValidKey(key))
                {
                    curTB.Text = keyStr;
                    KeySet[Array.IndexOf(KeySet, curKey)] = key;
                }
                else
                {
                    System.Media.SystemSounds.Exclamation.Play();
                }
            }
        } // TextBox_KeyDown

        // Check if the key is valid
        private Boolean ValidKey(Keys key)
        {
            if (KeySet.Contains(key))
            {
                return false;
            }
            else if ((int)key >= 0 && (int)key <= 20)
            {
                return false;
            }
            else if ((int)key >= 32 && (int)key <= 40)
            {
                return false;
            }
            else if ((int)key >= 45 && (int)key <= 47)
            {
                return false;
            }
            else if ((int)key >= 144 && (int)key <= 165)
            {
                return false;
            }
            else if (key == Keys.Escape || key == Keys.LWin || key == Keys.RWin || key == Keys.Menu || key == Keys.Apps)
            {
                return false;
            }
            return true;
        }  // ValidKey

        // Check for a media key
        private Boolean MediaKey(Keys key)
        {
            if ((int)key >= 166 && (int)key <= 183)
            {
                return true;
            }
            return false;
        } // MediaKey
    }
}

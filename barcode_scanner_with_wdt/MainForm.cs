using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace barcode_scanner_with_wdt
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent(); 
            // For the sake of simplicity, the TTF is copied to output directory...
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "3 of 9 Barcode Regular.ttf");
            // ... and loaded here.
            privateFontCollection.AddFontFile(path);

            var fontFamily = privateFontCollection.Families[0];
            Debug.Assert(fontFamily.Name == "3 of 9 Barcode", "Expecting correct font family name");
            textBoxBarcode.Font = new Font(fontFamily, 20F);

            textBoxEnterBC.TextChanged += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBoxEnterBC.Text))
                {
                    textBoxBarcode.Clear();
                }
                else if (textBoxEnterBC.Text.Length == _lenPrev - 1)
                {
                    // Backspace
                }
                else
                {
                    int delta;
                    bool ignore = false;
                    if(textBoxEnterBC.Text.Length > _lenPrev)
                    {
                        // Text is increasing in length.
                        delta = textBoxEnterBC.Text.Length - _lenPrev;
                    }
                   
                    else
                    {
                        delta = textBoxEnterBC.Text.Length;
                    }
                    _wdt.StartOrRestart(Math.Max(delta, 0));
                }
                _lenPrev = textBoxEnterBC.Text.Length;
            };
            textBoxEnterBC.KeyDown += (sender, e) =>
            {
                if(e.KeyData == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            _wdt.Completed += (sender, e) =>
            {
                Invoke((MethodInvoker)delegate
                {
                    // The fastest typist averages ~16 keystrokes per second

                    var startIndex = Math.Max(0, textBoxEnterBC.Text.Length - e.EventCount);
                    var barcode = textBoxEnterBC.Text.Substring(startIndex);
                    // Where word ~ 5 keystrokes
                    var wpm = 5 * (e.EventCount / e.TotalElapsed.TotalSeconds);

                    if (localDetectScan())
                    {
                        localOnBarcodeScan(barcode);
                    }
                    else
                    {
                        textBoxBarcode.Clear();
                        labelMessage.Text = "Humanoid detected.";
                    }

                    #region L o c a l    M e t h o d s
                    bool localDetectScan()
                    {
                        // BC must be at least 6 characters to qualify
                        if (e.EventCount < 6)
                        {
                            return false;
                        }

                        // Fastest typist is ~200 wpm
                        return (wpm >= 200);
                    }

                    void localOnBarcodeScan(string barcode)
                    {
                        textBoxEnterBC.Text = barcode;
                        // Crude QRcode detector
                        if (barcode.IndexOf("http") == 0)
                        {
                            textBoxBarcode.Clear();
                            labelMessage.Text ="QRCode detected.";
                        }
                        else
                        {
                            textBoxBarcode.Text = barcode;
                            textBoxEnterBC.SelectAll();
                            labelMessage.Text = $"{e.EventCount} events in {e.TotalElapsed.TotalSeconds} seconds {Math.Round(wpm)} WPM";
                        }
                    }
                    #endregion L o c a l    M e t h o d s
                });
            };
        }

        int _lenPrev = 0;

        WatchdogTimer _wdt = new WatchdogTimer(TimeSpan.FromMilliseconds(250)); 
        PrivateFontCollection privateFontCollection = new PrivateFontCollection();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
                privateFontCollection.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

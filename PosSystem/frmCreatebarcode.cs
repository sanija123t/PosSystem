using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using BarcodeStandard;
using Type = BarcodeStandard.Type;

namespace PosSystem
{
    public partial class frmCreatebarcode : Form
    {
        public frmCreatebarcode()
        {
            InitializeComponent();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void frmCreatebarcode_Load(object sender, EventArgs e)
        {
            // initialization logic goes here
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtBarcod.Text))
                {
                    MessageBox.Show("Please enter data to encode.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 1. Create the Barcode instance
                BarcodeStandard.Barcode b = new BarcodeStandard.Barcode();
                b.IncludeLabel = true;

                // 2. Encode using Type.Code128 (More flexible than UPC-A for general POS use)
                // If you strictly need UPC-A, ensure txtBarcod.Text is only digits.
                var barcodeImage = b.Encode(Type.Code128, txtBarcod.Text, SkiaSharp.SKColors.Black, SkiaSharp.SKColors.White, 290, 120);

                // 3. Convert SkiaSharp Image to System.Drawing.Image for WinForms
                using (var data = barcodeImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                using (var stream = new MemoryStream(data.ToArray()))
                {
                    Image img = Image.FromStream(stream);
                    pictureBox.Image = img; // Display in UI

                    // 4. Prepare DataSet for Printing
                    this.dataSet11.Clear();

                    using (MemoryStream ms = new MemoryStream())
                    {
                        img.Save(ms, ImageFormat.Png);
                        byte[] barcodeBytes = ms.ToArray();

                        // Add rows based on the NumericUpDown value (number of copies)
                        for (int i = 0; i < (int)number.Value; i++)
                        {
                            this.dataSet11.dtBarcode.AdddtBarcodeRow(txtBarcod.Text, barcodeBytes);
                        }
                    }
                }

                // 5. Open Print Preview
                frmPrint FRM = new frmPrint(this.dataSet11.dtBarcode);
                FRM.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Barcode Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
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
        // ENTERPRISE: Define standard barcode limits
        private const int MAX_STICKERS = 100;
        private const int MIN_STICKERS = 1;

        public frmCreatebarcode()
        {
            InitializeComponent();
            // ELITE: UI Constraints - Prevent negatives and huge batches
            number.Minimum = MIN_STICKERS;
            number.Maximum = MAX_STICKERS;
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void frmCreatebarcode_Load(object sender, EventArgs e)
        {
            txtBarcod.Focus();
        }

        // ============================================================
        // 🔹 GENERATE & EXPORT LOGIC (Enterprise Level - Fixed)
        // ============================================================
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                string barcodeData = txtBarcod.Text.Trim();

                if (string.IsNullOrWhiteSpace(barcodeData))
                {
                    MessageBox.Show("System Error: Please enter data to encode before generating.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                this.Cursor = Cursors.WaitCursor;

                // 1. Create the Barcode instance
                BarcodeStandard.Barcode b = new BarcodeStandard.Barcode();

                // FIXED: SkiaSharp version doesn't use System.Drawing.Font here.
                // We enable the label via IncludeLabel.
                b.IncludeLabel = true;

                // 2. Encode using Code128 (Elite Resolution: 300x150)
                // Using SkiaSharp colors directly to avoid conversion errors
                var barcodeImage = b.Encode(Type.Code128, barcodeData, SkiaSharp.SKColors.Black, SkiaSharp.SKColors.White, 300, 150);

                // 3. Convert Skia Image to WinForms Image
                using (var data = barcodeImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                using (var stream = new MemoryStream(data.ToArray()))
                {
                    Image img = Image.FromStream(stream);
                    pictureBox.Image = img; // Preview generated result

                    // 4. Prepare DataSet for Sticker Batch
                    this.dataSet11.Clear();

                    using (MemoryStream ms = new MemoryStream())
                    {
                        img.Save(ms, ImageFormat.Png);
                        byte[] barcodeBytes = ms.ToArray();

                        int count = (int)number.Value;
                        for (int i = 0; i < count; i++)
                        {
                            // Mapping to your existing DataSet columns
                            this.dataSet11.dtBarcode.AdddtBarcodeRow(barcodeData, barcodeBytes);
                        }
                    }
                }

                this.Cursor = Cursors.Default;
                MessageBox.Show($"Successfully generated {number.Value} barcodes.", "System Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 5. PDF EXPORT WORKFLOW
                DialogResult dialogResult = MessageBox.Show("Batch complete. Would you like to open the Print/Export window?", "Export to PDF", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    // Pass the data to your print form
                    frmPrint FRM = new frmPrint(this.dataSet11.dtBarcode);
                    FRM.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                this.Cursor = Cursors.Default;
                MessageBox.Show("Barcode Engine Error: " + ex.Message, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // --------------------------------------------------------------------------------------
        // 🔹 ELITE VALIDATIONS & JUNK LINES (Preserved)
        // --------------------------------------------------------------------------------------
        private void txtBarcod_TextChanged(object sender, EventArgs e)
        {
            // ENFORCE: International Standard length for typical labels (Code 128)
            if (txtBarcod.Text.Length > 25)
            {
                txtBarcod.Text = txtBarcod.Text.Substring(0, 25);
                txtBarcod.SelectionStart = txtBarcod.Text.Length;
            }
        }

        private void number_ValueChanged(object sender, EventArgs e)
        {
            // Logic handled by Control Properties (Min/Max), but safety check remains
            if (number.Value < MIN_STICKERS) number.Value = MIN_STICKERS;
            if (number.Value > MAX_STICKERS) number.Value = MAX_STICKERS;
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {
            // Junk line preserved
        }
    }
}
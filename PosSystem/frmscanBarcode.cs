using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZXing;

namespace PosSystem
{
    public partial class frmscanBarcode : Form
    {
        frmProduct f; // Parent form to update the barcode textbox
        FilterInfoCollection FilterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;

        // Create the barcode reader once for efficiency
        BarcodeReader reader = new BarcodeReader();

        public frmscanBarcode(frmProduct frm)
        {
            InitializeComponent();
            f = frm;

            // Optimize for Retail Barcodes
            reader.Options.PossibleFormats = new List<BarcodeFormat> {
                BarcodeFormat.EAN_13,
                BarcodeFormat.EAN_8,
                BarcodeFormat.UPC_A,
                BarcodeFormat.CODE_128
            };
            reader.Options.TryHarder = true; // High accuracy
            reader.AutoRotate = true;       // Scan even if the product is sideways
        }

        private void frmscanBarcode_Load(object sender, EventArgs e)
        {
            // Populate available cameras
            FilterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in FilterInfoCollection)
                cbCamara.Items.Add(device.Name);

            if (cbCamara.Items.Count > 0)
                cbCamara.SelectedIndex = 0;
            else
                MessageBox.Show("No camera detected.", "System Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (videoCaptureDevice != null && videoCaptureDevice.IsRunning) return;

            try
            {
                videoCaptureDevice = new VideoCaptureDevice(FilterInfoCollection[cbCamara.SelectedIndex].MonikerString);
                videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
                videoCaptureDevice.Start();

                btnStart.Enabled = false; // Prevent starting multiple times
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to start camera: " + ex.Message, "Camera Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VideoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = null;
            try
            {
                bitmap = (Bitmap)eventArgs.Frame.Clone();
                var result = reader.Decode(bitmap);

                if (pictureBox.InvokeRequired)
                {
                    pictureBox.Invoke(new MethodInvoker(delegate ()
                    {
                        // Update camera preview safely
                        if (pictureBox.Image != null) pictureBox.Image.Dispose();
                        pictureBox.Image = (Bitmap)bitmap.Clone();

                        // If barcode detected, update parent form
                        if (result != null)
                        {
                            if (f != null && f.txtBarcode != null)
                            {
                                f.txtBarcode.Invoke(new MethodInvoker(() =>
                                {
                                    f.txtBarcode.Text = result.Text;
                                }));
                            }

                            // Optional: beep on successful scan
                            System.Media.SystemSounds.Beep.Play();

                            // Stop camera and close form automatically
                            StopCamera();
                            this.Close();
                        }
                    }));
                }
                else
                {
                    if (pictureBox.Image != null) pictureBox.Image.Dispose();
                    pictureBox.Image = (Bitmap)bitmap.Clone();
                }
            }
            catch { /* ignore frame errors */ }
            finally
            {
                if (bitmap != null) bitmap.Dispose();
            }
        }

        private void StopCamera()
        {
            if (videoCaptureDevice != null)
            {
                if (videoCaptureDevice.IsRunning)
                {
                    videoCaptureDevice.SignalToStop();
                    videoCaptureDevice.WaitForStop(); // Ensure proper shutdown
                }
                videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;
                videoCaptureDevice = null;
                btnStart.Enabled = true; // Re-enable start button if form reused
            }
        }

        private void frmscanBarcode_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCamera(); // Ensure camera stops on form close
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close(); // Close form when clicking 'X' or custom close button
        }
    }
}
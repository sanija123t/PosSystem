using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ZXing;
using System.Diagnostics;

namespace PosSystem
{
    public partial class frmscanBarcode : Form
    {
        frmProduct f; // Parent form to update the barcode textbox
        FilterInfoCollection FilterInfoCollection;
        VideoCaptureDevice videoCaptureDevice;

        // Barcode reader
        BarcodeReader reader = new BarcodeReader();

        // Throttle decoding
        Stopwatch decodeTimer = new Stopwatch();
        const int DecodeInterval = 200; // milliseconds

        bool barcodeDetected = false;

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
            reader.AutoRotate = true;       // Scan even if sideways
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

            decodeTimer.Start();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartCamera();
        }

        private void btnReStart_Click(object sender, EventArgs e)
        {
            barcodeDetected = false; // Reset flag
            StartCamera();           // Restart camera and scanning
        }

        private void StartCamera()
        {
            if (videoCaptureDevice != null && videoCaptureDevice.IsRunning) return;

            try
            {
                videoCaptureDevice = new VideoCaptureDevice(FilterInfoCollection[cbCamara.SelectedIndex].MonikerString);
                videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
                videoCaptureDevice.Start();

                btnStart.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to start camera: " + ex.Message, "Camera Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void VideoCaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
            {
                // Throttle decoding to reduce CPU usage
                if (!barcodeDetected && decodeTimer.ElapsedMilliseconds >= DecodeInterval)
                {
                    decodeTimer.Restart();
                    var result = reader.Decode(bitmap);
                    if (result != null)
                    {
                        barcodeDetected = true;
                        UpdateParentBarcode(result.Text);
                    }
                }

                // Update camera preview safely
                pictureBox.Invoke(new MethodInvoker(() =>
                {
                    if (pictureBox.Image != null) pictureBox.Image.Dispose();
                    pictureBox.Image = (Bitmap)bitmap.Clone();
                }));
            }
        }

        private void UpdateParentBarcode(string barcode)
        {
            if (f != null && f.txtBarcode != null)
            {
                f.txtBarcode.Invoke(new MethodInvoker(() =>
                {
                    f.txtBarcode.Text = barcode;
                }));
            }

            // Optional: beep on successful scan
            System.Media.SystemSounds.Beep.Play();

            // Stop camera after success
            StopCamera();
        }

        private void StopCamera()
        {
            if (videoCaptureDevice != null && videoCaptureDevice.IsRunning)
            {
                videoCaptureDevice.SignalToStop();
                videoCaptureDevice.WaitForStop();
                videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;
                videoCaptureDevice = null;
            }
            btnStart.Invoke(new MethodInvoker(() => btnStart.Enabled = true));
        }

        private void frmscanBarcode_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopCamera();

            // Dispose preview image to free memory
            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
                pictureBox.Image = null;
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close(); // Close form when clicking 'X' or custom close button
        }
    }
}
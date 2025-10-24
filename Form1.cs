using System;
using System.Drawing;
using System.Windows.Forms;
using Vosk;
using NAudio.Wave;

namespace VoskWorking
{
    public partial class Form1 : Form
    {
        private Model? model;
        private VoskRecognizer? recognizer;
        private WaveInEvent? waveIn;
        private bool isRecording = false;

        public Form1()
        {
            InitializeComponent();
            InitializeVosk();
        }

        private void InitializeVosk()
        {
            try
            {
                // Vosk modelini yükle (İngilizce)
                // Not: Model dosyasını C:\vosk-model-en-us-0.22 klasörüne indirmeniz gerekiyor
                model = new Model("C:\\vosk-model-en-us-0.22\\vosk-model-en-us-0.22");
                recognizer = new VoskRecognizer(model, 16000);

                lblStatus.Text = "✅ Vosk Speech Recognition hazır!";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Vosk Model Hatası: {ex.Message}\n\n" +
                               "💡 Çözüm:\n" +
                               "1. https://alphacephei.com/vosk/models adresine git\n" +
                               "2. 'vosk-model-en-us-0.22' indir\n" +
                               "3. C:\\vosk-model-en-us-0.22 klasörüne çıkar\n" +
                               "4. Uygulamayı tekrar çalıştır";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                if (recognizer == null)
                {
                    lblStatus.Text = "❌ Vosk Recognizer başlatılamadı!";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }

                if (!isRecording)
                {
                    StartRecording();
                }
                else
                {
                    StopRecording();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Hata: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void StartRecording()
        {
            try
            {
                // O adamın kodunu kullan
                waveIn = new WaveInEvent();
                waveIn.DeviceNumber = 0; // Varsayılan mikrofon
                waveIn.WaveFormat = new WaveFormat(16000, 1); // 16kHz mono
                waveIn.BufferMilliseconds = 1000; // 1 saniye buffer

                waveIn.DataAvailable += (sender, e) =>
                {
                    if (recognizer != null && recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                    {
                        // Ana thread'de UI güncelle
                        if (InvokeRequired)
                        {
                        Invoke(new Action(() =>
                        {
                            // JSON'dan sadece metni çıkar
                            string result = recognizer.Result();
                            string text = ExtractTextFromJson(result);
                            txtResult.Text = text;
                            lblStatus.Text = "✅ Konuşma tanındı!";
                            lblStatus.ForeColor = Color.Green;
                        }));
                        }
                    }
                };

                waveIn.StartRecording();
                isRecording = true;
                btnStart.Text = "DURDUR";
                btnStart.BackColor = Color.Red;
                lblStatus.Text = "🎤 Dinleniyor...";
                lblStatus.ForeColor = Color.Blue;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Kayıt Hatası: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void StopRecording()
        {
            try
            {
                if (waveIn != null)
                {
                    waveIn.StopRecording();
                    waveIn.Dispose();
                    waveIn = null;
                }

                isRecording = false;
                btnStart.Text = "BAŞLA";
                btnStart.BackColor = Color.Green;
                lblStatus.Text = "⏹️ Durduruldu";
                lblStatus.ForeColor = Color.Gray;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Durdurma Hatası: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private string ExtractTextFromJson(string jsonResult)
        {
            try
            {
                // JSON'dan "text" değerini çıkar
                if (jsonResult.Contains("\"text\""))
                {
                    // "text": "hello" formatından "hello" kısmını çıkar
                    int textStart = jsonResult.IndexOf("\"text\"");
                    int colonIndex = jsonResult.IndexOf(":", textStart);
                    int quoteStart = jsonResult.IndexOf("\"", colonIndex) + 1;
                    int quoteEnd = jsonResult.IndexOf("\"", quoteStart);
                    
                    if (quoteStart > 0 && quoteEnd > quoteStart)
                    {
                        return jsonResult.Substring(quoteStart, quoteEnd - quoteStart);
                    }
                }
                return ""; // JSON çıkarılamazsa boş string döndür
            }
            catch
            {
                return ""; // Hata durumunda boş string döndür
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopRecording();
            recognizer?.Dispose();
            model?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_bot
{
    public partial class Form1 : Form
    {
        private readonly HttpClient http = new HttpClient();
        private readonly string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";
        private readonly string apiKey = "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";

        private Panel panelChat;
        private TextBox txtInput;
        private Button btnSend;

        [System.Runtime.InteropServices.DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.panelChat = new Panel();
            this.txtInput = new TextBox();
            this.btnSend = new Button();

            // panelChat
            this.panelChat.AutoScroll = true;
            this.panelChat.BackColor = Color.WhiteSmoke;
            this.panelChat.Location = new Point(10, 10);
            this.panelChat.Size = new Size(780, 380);

            // txtInput
            this.txtInput.Location = new Point(10, 400);
            this.txtInput.Size = new Size(680, 50);
            this.txtInput.Multiline = true;
            this.txtInput.ScrollBars = ScrollBars.Vertical;
            this.txtInput.Font = new Font("Segoe UI", 11);

            // btnSend
            this.btnSend.Location = new Point(700, 410);
            this.btnSend.Size = new Size(90, 30);
            this.btnSend.Text = "Send";
            this.btnSend.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            this.btnSend.Click += async (s, e) => await SendMessage();

            // Enter key
            this.txtInput.KeyDown += async (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    await SendMessage();
                }
            };

            this.ClientSize = new Size(800, 470);
            this.Controls.Add(panelChat);
            this.Controls.Add(txtInput);
            this.Controls.Add(btnSend);
            this.Text = "ChatBot GPT Style";
        }

        private Control AddMessage(string text, bool isUser)
        {
            int yOffset = 15;

            foreach (Control c in panelChat.Controls)
                yOffset = Math.Max(yOffset, c.Bottom + 20);

            // Bubble Panel
            Panel bubble = new Panel();
            bubble.Padding = new Padding(14);
            bubble.MaximumSize = new Size(500, 0);
            bubble.AutoSize = true;
            bubble.Top = yOffset;

            bubble.BackColor = isUser
                ? Color.FromArgb(52, 152, 219)
                : Color.FromArgb(236, 240, 241);

            // Text Label
            Label lbl = new Label();
            lbl.Text = text;
            lbl.AutoSize = true;
            lbl.MaximumSize = new Size(480, 0);
            lbl.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lbl.ForeColor = isUser ? Color.White : Color.Black;
            bubble.Controls.Add(lbl);

            // Position bubble first to compute real size
            bubble.Left = isUser
                ? panelChat.ClientSize.Width - bubble.PreferredSize.Width - 25
                : 15;

            panelChat.Controls.Add(bubble);

            // Now apply rounded region AFTER size is computed
            bubble.Region = Region.FromHrgn(CreateRoundRectRgn(
                0, 0, bubble.Width, bubble.Height, 22, 22));

            bubble.BringToFront();
            panelChat.VerticalScroll.Value = panelChat.VerticalScroll.Maximum;
            panelChat.PerformLayout();

            return bubble;
        }

        private async Task SendMessage()
        {
            string userMsg = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(userMsg)) return;

            AddMessage(userMsg, true);
            txtInput.Clear();

            var thinking = AddMessage("جاري التفكير...", false);

            try
            {
                var payload = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[]
                    {
                        new { role = "user", content = userMsg }
                    },
                    temperature = 0.3,
                    max_tokens = 4000
                };

                var json = JsonSerializer.Serialize(payload);

                using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
                req.Headers.Add("Authorization", $"Bearer {apiKey}");
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var res = await http.SendAsync(req);
                var resTxt = await res.Content.ReadAsStringAsync();

                string reply = "لم أستطع قراءة رد الـ API.";

                if (res.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(resTxt);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var msgObj = choices[0].GetProperty("message");
                        reply = msgObj.GetProperty("content").GetString();
                    }
                }
                else reply = $"خطأ: {res.StatusCode}";

                panelChat.Controls.Remove(thinking);
                AddMessage(reply, false);
            }
            catch (Exception ex)
            {
                panelChat.Controls.Remove(thinking);
                AddMessage("خطأ: " + ex.Message, false);
            }
        }
    }
}

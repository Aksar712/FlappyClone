using DevExpress.LookAndFeel;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace FlappyClone
{
    public partial class Form1 : XtraForm
    {
        // Oyun alanı ve değişkenler
        private GamePanel playArea;
        private Timer gameTimer;
        private List<Pipe> pipes = new List<Pipe>();
        private Random rnd = new Random();

        private int score = 0;
        private int bestScore = 0;

        private float birdY = 0f;
        private float birdVelocity = 0f;
        private const float Gravity = 0.7f;
        private const float FlapStrength = -8f;

        private const int birdX = 100;
        private const int birdWidth = 40;
        private const int birdHeight = 35;

        private int pipeWidth = 120;
        private int pipeGap = 170;
        private int pipeSpeed = 5;
        private int spawnCounter = 0;
        private int spawnInterval = 200;

        private LabelControl lblScore;
        private LabelControl lblBest;
        private SimpleButton btnStart;
        private SimpleButton btnRestart;

        private bool isRunning = false;
        private Image birdSprite;
        private Image columnBottomSprite;
        private Image columnTopSprite;

        private PictureBox birdBox;

        public Form1()
        {
            DefaultLookAndFeel lookAndFeel = new DefaultLookAndFeel();
            lookAndFeel.LookAndFeel.SkinName = "Office 2019 Colorful";

            InitializeComponent();

            // Resim Yükleme
            try { birdSprite = Image.FromFile("bird.png"); }
            catch (Exception ex) { birdSprite = null; Debug.WriteLine("HATA: bird.png yüklenemedi: " + ex.Message); }

            try
            {
                columnBottomSprite = Image.FromFile("column_bottom.png");
                columnTopSprite = Image.FromFile("column_top.png");
            }
            catch (Exception ex)
            {
                columnBottomSprite = null;
                columnTopSprite = null;
                Debug.WriteLine("HATA: Sütun resimleri yüklenemedi: " + ex.Message);
            }

            // Form Ayarları
            Text = "Flappy Bird Klon";
            Width = 800;
            Height = 720;
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;

            // Oyun Alanı Ayarları
            playArea = new GamePanel
            {
                Location = new Point(10, 50),
                Size = new Size(768, 600),
                BackgroundImageLayout = ImageLayout.Stretch
            };
            try { playArea.BackgroundImage = Image.FromFile("Background.png"); }
            catch (Exception ex) { Debug.WriteLine("HATA: Background.png yüklenemedi: " + ex.Message); }
            Controls.Add(playArea);

            // Kuş için PictureBox
            birdBox = new PictureBox
            {
                Width = birdWidth,
                Height = birdHeight,
                Left = birdX,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent,
                Image = birdSprite
            };
            playArea.Controls.Add(birdBox);

            // UI Elemanları
            lblScore = new LabelControl { Location = new Point(12, 12), Text = "Skor: 0", Appearance = { Font = new Font("Segoe UI", 12, FontStyle.Bold) } };
            Controls.Add(lblScore);
            lblBest = new LabelControl { Location = new Point(350, 12), Text = "Best: 0", Appearance = { Font = new Font("Segoe UI", 12, FontStyle.Bold) } };
            Controls.Add(lblBest);
            btnStart = new SimpleButton { Location = new Point(660, 8), Text = "Start", Size = new Size(60, 30) };
            btnStart.Click += (s, e) => StartGame();
            Controls.Add(btnStart);
            btnRestart = new SimpleButton { Location = new Point(720, 8), Text = "Restart", Size = new Size(60, 30) };
            btnRestart.Click += (s, e) => ResetGame();
            Controls.Add(btnRestart);

            // Timer ve Olaylar
            gameTimer = new Timer { Interval = 20 };
            gameTimer.Tick += GameTimer_Tick;
            KeyDown += Form1_KeyDown;
            playArea.MouseDown += (s, e) => Flap();

            LoadBestScore();
            ResetGame();
        }

        private void LoadBestScore()
        {
            try
            {
                var path = Path.Combine(System.Windows.Forms.Application.StartupPath, "bestscore.txt");
                if (File.Exists(path)) int.TryParse(File.ReadAllText(path), out bestScore);
                lblBest.Text = $"Best: {bestScore}";
            }
            catch { bestScore = 0; }
        }

        private void SaveBestScore()
        {
            try
            {
                var path = Path.Combine(System.Windows.Forms.Application.StartupPath, "bestscore.txt");
                File.WriteAllText(path, bestScore.ToString());
            }
            catch { }
        }

        // 🔥 Skora göre dinamik spawn interval hesaplayan fonksiyon
        private int GetSpawnIntervalByScore(int score)
        {
            if (score < 10) return 150;    // 3 saniye
            else if (score < 20) return 100; // 2 saniye
            else if (score < 30) return 70;  // 1.4 saniye
            else return 50;                  // 1 saniye
        }

        private void ResetGame()
        {
            isRunning = false;
            gameTimer.Stop();
            foreach (var p in pipes)
            {
                playArea.Controls.Remove(p.TopPipe);
                playArea.Controls.Remove(p.BottomPipe);
            }
            pipes.Clear();
            score = 0;
            birdVelocity = 0;
            birdY = playArea.Height / 2 - birdHeight / 2;
            birdBox.Top = (int)birdY;
            spawnCounter = 0;
            // Başlangıç spawn interval (seyrek)
            spawnInterval = GetSpawnIntervalByScore(0);
            lblScore.Text = "Skor: 0";
            pipeSpeed = 3;
        }

        private void StartGame()
        {
            if (!isRunning)
            {
                isRunning = true;
                birdVelocity = FlapStrength; // Oyuna başlarken kuş hemen zıplasın
                spawnCounter = 0;

                // 🔥 Oyunun başında hemen ilk boruyu koy
                CreatePipe(); 

                gameTimer.Start();  


            }
        }

        private void GameOver()
        {
            isRunning = false;
            gameTimer.Stop();
            if (score > bestScore)
            {
                bestScore = score;
                lblBest.Text = $"Best: {bestScore}";
                SaveBestScore();
            }
            MessageBox.Show($"Oyun bitti!\nSkor: {score}", "Game Over");
            ResetGame();
        }

        private void Flap()
        {
            birdVelocity = FlapStrength;
            if (!isRunning) StartGame();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            if (!isRunning) return;

            // Fizik
            birdVelocity += Gravity;
            birdY += birdVelocity;
            birdBox.Top = (int)birdY;

            // Dinamik interval güncelle
            spawnInterval = GetSpawnIntervalByScore(score);

            // Boru oluşturma
            spawnCounter++;
            if (spawnCounter >= spawnInterval)
            {
                bool canSpawn = true;
                int minHorizontalGap = 200; // minimum yatay mesafe (px)

                if (pipes.Count > 0)
                {
                    var last = pipes[pipes.Count - 1];
                    if (last.X > playArea.Width - minHorizontalGap)
                        canSpawn = false;
                }

                if (canSpawn)
                {
                    spawnCounter = 0;
                    CreatePipe();
                }
            }

            // Boruları hareket ettir
            for (int i = pipes.Count - 1; i >= 0; i--)
            {
                pipes[i].X -= pipeSpeed;

                if (pipes[i].X + pipeWidth < 0)
                {
                    playArea.Controls.Remove(pipes[i].TopPipe);
                    playArea.Controls.Remove(pipes[i].BottomPipe);
                    pipes.RemoveAt(i);
                }
            }

            // Çarpışma kontrolü
            Rectangle birdRect = birdBox.Bounds;
            if (birdRect.Top < 0 || birdRect.Bottom > playArea.Height)
            {
                GameOver();
                return;
            }

            foreach (var p in pipes)
            {
                if (birdRect.IntersectsWith(p.TopPipe.Bounds) || birdRect.IntersectsWith(p.BottomPipe.Bounds))
                {
                    GameOver();
                    return;
                }

                if (!p.Passed && p.X + pipeWidth < birdX)
                {
                    p.Passed = true;
                    score++;
                    lblScore.Text = $"Skor: {score}";

                    // Boru hızı artışı
                    if (score % 5 == 0) pipeSpeed++;
                }
            }
        }

        private void CreatePipe()
        {
            int minCenter = pipeGap / 2 + 150;
            int maxCenter = playArea.Height - pipeGap / 2 - 150;
            if (minCenter >= maxCenter) return;

            int center = rnd.Next(minCenter, maxCenter);
            int pipeX = playArea.Width;

            var top = new PictureBox
            {
                Width = pipeWidth,
                Height = center - (pipeGap / 2),
                Left = pipeX,
                Top = 0,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = columnTopSprite
            };

            var bottom = new PictureBox
            {
                Width = pipeWidth,
                Height = playArea.Height - (center + pipeGap / 2),
                Left = pipeX,
                Top = center + pipeGap / 2,
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = columnBottomSprite
            };

            playArea.Controls.Add(top);
            playArea.Controls.Add(bottom);

            pipes.Add(new Pipe { TopPipe = top, BottomPipe = bottom, Passed = false });
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                Flap();
                e.Handled = true;
            }
        }

        private class Pipe
        {
            public PictureBox TopPipe { get; set; }
            public PictureBox BottomPipe { get; set; }
            public bool Passed { get; set; }

            public int X
            {
                get => TopPipe.Left;
                set
                {
                    TopPipe.Left = value;
                    BottomPipe.Left = value;
                }
            }
        }
    }

    public class GamePanel : Panel
    {
        public GamePanel()
        {
            this.DoubleBuffered = true;
        }
    }
}

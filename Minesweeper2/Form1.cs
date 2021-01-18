using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Text.Json;

namespace Minesweeper
{

    public partial class Form1 : Form
    {
        readonly private string assetsPath = @"..\..\..\assets\";
        private const int WinGame = 1000;
        private const int Opened = 300;
        private const int Marked = 200;
        private const int Unmarked = 201;
        private const int Mine = 100;
        private const int Blocked = 400;
        private const int MarkersAreOver = 222;
        private int MarkersLeft;
        readonly private int MinesCount = 40;
        readonly private int height = 16;
        readonly private int width = 13;
        private int NotMinedCellsCount;
        private bool Started = false;
        readonly private CellsManager cm;
        private DateTime dt;
        private TimeSpan ts;
        readonly private RecordsManager rm;
        public Form1()
        {
            InitializeComponent();
            cm = new CellsManager(height, width, MinesCount);
            cm.Restart();
            SetMarkers(MinesCount);
            NotMinedCellsCount = height * width - MinesCount;
            rm = new RecordsManager();
        }

        private void SetMarkers(int n)
        {
            MarkersLeft = n;
            if(n < 10) markers_label.Text = "00" + n;
            else markers_label.Text = "0" + n;
        }

        private void ShowRecords(object sender, EventArgs e)
        {
            rm.ShowRecords();
        }

        private void ShowTime(object sender, EventArgs e)
        {
            if (!Started)
            {
                dt = DateTime.Now;
                Started = true;
            }
            ts = DateTime.Now - dt;
            string tmp;
            if (ts.Minutes < 10)
            {
                tmp = "0" + ts.Minutes + ":";
            }
            else tmp = ts.Minutes + ":";
            if (ts.Seconds < 10)
            {
                tmp += "0" + ts.Seconds;
            } else tmp += ts.Seconds;
            time_label.Text = tmp;
        }

        private void Lose()
        {
            EndGame();
            SetLoseSmile();
            OpenAll();
            MessageBox.Show("You lost");
        }

        private void Win()
        {
            EndGame();
            RecordWin();
            SetWinSmile();
            OpenAll();
            MessageBox.Show("You did it, man! Congratulations!");
        }

        private void EndGame()
        {
            Started = false;
            timer1.Enabled = false;
        }

        private void SetImageOfNumber(PictureBox pb, int n)
        {
            pb.ImageLocation = assetsPath + $"Opened{n}.png";
            NotMinedCellsCount--;
        }

        private void SetImageOfMarker(PictureBox pb)
        {
            pb.ImageLocation = assetsPath + "Marker.png";
            SetMarkers(MarkersLeft - 1);
        }

        private void SetClosedImage(PictureBox pb)
        {
            pb.ImageLocation = assetsPath + "Closed.png";
            SetMarkers(MarkersLeft + 1);
        }

        private void SetImageOfMine(PictureBox pb)
        {
            pb.ImageLocation = assetsPath + "Mine.png";
        }

        private void RecordWin()
        {
            rm.AddRecord("player", DateTime.Now.ToString(), (int)ts.TotalSeconds);
        }

        private void SetWinSmile()
        {
            Smile.ImageLocation = assetsPath + "Smiley0.png";
        }

        private void StartGame()
        {
            ShowTime(null, null);
            timer1.Enabled = true;
        }

        private static void NoMarkersLeftWarning()
        {
            MessageBox.Show("You've used all the markers and still no win. Maybe you are wrong?");
        }

        private void PictureBox_Click(object sender, EventArgs e)
        {
            if (!Started) StartGame();
            MouseEventArgs ev = (MouseEventArgs)e;
            PictureBox pb = (PictureBox)sender;
            int res;
            int index = Convert.ToInt32(pb.Name[10..]);
            if (ev.Button == MouseButtons.Left) res = cm.HandleClick(index);
            else res = cm.HandleClick(index, true);
            switch (res)
            {
                case Blocked:
                    return;
                case Opened:
                    return;
                case Mine:
                    Lose();
                    return;
                case MarkersAreOver:
                    NoMarkersLeftWarning();
                    return;
                case WinGame:
                    SetImageOfMarker(pb);
                    Win();
                    return;
                case Marked:
                    SetImageOfMarker(pb);
                    return;
                case Unmarked:
                    SetClosedImage(pb);
                    return;
                default:
                    SetImageOfNumber(pb, res);
                    if(NotMinedCellsCount == 0)Win();
                    return;
            }
        }

        private void OpenAll()
        {
            int count = 0;
            for (int i = 0; i < cm.MinesField.GetLength(0); i++)
            {
                for (int j = 0; j < cm.MinesField.GetLength(1); j++)
                {
                    PictureBox pb = (PictureBox)tableLayoutPanel1.Controls[count];
                    if (cm.MinesField[i, j] < 9)
                    {
                        SetImageOfNumber(pb, cm.MinesField[i, j]);
                    }
                    else
                    {
                        SetImageOfMine(pb);
                    }
                    count++;
                }
            }
        }

        private void CloseAll()
        {
            foreach (Control ctrl in tableLayoutPanel1.Controls)
            {
                PictureBox pb = (PictureBox)ctrl;
                pb.ImageLocation = assetsPath + "Closed.png";
            }
        }

        private void SetLoseSmile()
        {
            Smile.ImageLocation = assetsPath + "Smiley3-1.png";
        }

        private void SetDefaultSmile()
        {
            Smile.ImageLocation = assetsPath + "Smiley11.png";
        }

        private void ClearGame(object sender = null, EventArgs e = null)
        {
            EndGame();
            CloseAll();
            cm.Restart();
            SetMarkers(MinesCount);
            time_label.Text = "00:00";
            SetDefaultSmile();
        }

        class CellsManager
        {
            public int[,] MinesField { get; private set; }
            readonly private int CellsCount;
            private (int, int)[] MinesPositions;
            readonly private int MinesCount;
            private bool[,] MarkedField;
            private bool[,] OpenedFields;
            private int MarkedCellsCount;
            public CellsManager(int h, int w, int n)
            {
                if (n > h * w) throw new Exception("Nice joke, man");
                MinesField = new int[h, w];
                CellsCount = h * w;
                MinesPositions = new (int, int)[n];
                MarkedField = new bool[h, w];
                OpenedFields = new bool[h, w];
                MinesCount = n;
                MarkedCellsCount = 0;
            }

            public void Restart()
            {
                ClearMinesField();
                GenerateMines();
                AddNumbersToMatrix();
            }

            public int HandleClick(int n, bool toMark = false)
            {
                (int y, int x) = MinesPositionToCoords(n);
                if (toMark)
                {
                    if(OpenedFields[y, x])
                    {
                        return Opened;
                    }
                    if(!MarkedField[y, x])
                    {
                        if(MarkedCellsCount == MinesCount)
                        {
                            return MarkersAreOver;
                        }
                        MarkedField[y, x] = true;
                        MarkedCellsCount++;
                        if (MarkedCellsCount == MinesCount)
                        {
                            if (CheckIfAllMinesMarked())
                            {
                                return WinGame;
                            }
                        }
                        return Marked;
                    } 
                    else
                    {
                        MarkedField[y, x] = false;
                        MarkedCellsCount--;
                        return Unmarked;
                    }
                }
                if(MarkedField[y, x])
                {
                    return Blocked;
                }
                if (MinesField[y, x] >= Mine)
                {
                    return Mine;
                }
                else 
                {
                    OpenedFields[y, x] = true;
                    return MinesField[y, x]; 
                }
            }
            private bool CheckIfAllMinesMarked()
            {
                for(int i = 0; i < MinesPositions.Length; i++)
                {
                    (int y, int x) = MinesPositions[i];
                    if (!MarkedField[y, x])
                    {
                        return false;
                    }
                }
                return true;
            }
            public void GenerateMines()
            {
                int count = 0;
                Random rnd = new Random();
                while (count < MinesCount)
                {
                    int rndNum = rnd.Next(CellsCount);
                    (int y, int x) = MinesPositionToCoords(rndNum);
                    if (!MinesPositions.Contains((y, x)))
                    {
                        MinesPositions[count] = (y, x);
                        MinesField[y, x] = Mine;
                        count++;
                    }
                }
            }

            private (int, int) MinesPositionToCoords(int a)
            {
                if (a == 0) return (0, 0);
                double y = (double)a / MinesField.GetLength(1) - 0.0000001;
                int x = a - (int)y * MinesField.GetLength(1) - 1;
                return ((int)y, x);
            }

            public void ShowMinesField()
            {
                for (int i = 0; i < MinesField.GetLength(0); i++)
                {
                    for (int j = 0; j < MinesField.GetLength(1); j++)
                    {
                        Console.Write(MinesField[i, j] + " ");
                    }
                    Console.WriteLine();
                }
            }

            public void ClearMinesField()
            {
                MinesField = new int[MinesField.GetLength(0), MinesField.GetLength(1)];
                MinesPositions = new (int, int)[MinesCount];
                MarkedField = new bool[MinesField.GetLength(0), MinesField.GetLength(1)];
                OpenedFields = new bool[MinesField.GetLength(0), MinesField.GetLength(1)];
            }

            public void AddNumbersToMatrix()
            {
                for (int i = 0; i < MinesPositions.Length; i++)
                {
                    (int y, int x) = MinesPositions[i];
                    int w = MinesField.GetLength(1);
                    int h = MinesField.GetLength(0);
                    if (x != 0 && x != w - 1 && y != 0 && y != h - 1)
                    {
                        MinesField[y - 1, x]++;
                        MinesField[y - 1, x - 1]++;
                        MinesField[y - 1, x + 1]++;
                        MinesField[y + 1, x]++;
                        MinesField[y + 1, x + 1]++;
                        MinesField[y + 1, x - 1]++;
                        MinesField[y, x + 1]++;
                        MinesField[y, x - 1]++;
                        continue;
                    }
                    if (x == 0 && y == 0)
                    {
                        MinesField[y + 1, x]++;
                        MinesField[y + 1, x + 1]++;
                        MinesField[y, x + 1]++;
                        continue;
                    }
                    if (x == w - 1 && y == h - 1)
                    {
                        MinesField[y - 1, x]++;
                        MinesField[y - 1, x - 1]++;
                        MinesField[y, x - 1]++;
                        continue;
                    }
                    if (x == 0 && y != 0 && y != h - 1)
                    {
                        MinesField[y - 1, x]++;
                        MinesField[y - 1, x + 1]++;
                        MinesField[y + 1, x]++;
                        MinesField[y + 1, x + 1]++;
                        MinesField[y, x + 1]++;
                        continue;
                    }
                    if (y == 0 && x != 0 && x != w - 1)
                    {
                        MinesField[y + 1, x]++;
                        MinesField[y + 1, x + 1]++;
                        MinesField[y + 1, x - 1]++;
                        MinesField[y, x + 1]++;
                        MinesField[y, x - 1]++;
                        continue;
                    }
                    if (x == w - 1 && y != 0 && y != h - 1)
                    {
                        MinesField[y - 1, x]++;
                        MinesField[y - 1, x - 1]++;
                        MinesField[y, x - 1]++;
                        MinesField[y + 1, x - 1]++;
                        MinesField[y + 1, x]++;
                        continue;
                    }
                    if (y == h - 1 && x != 0 && x != w - 1)
                    {
                        MinesField[y - 1, x]++;
                        MinesField[y - 1, x - 1]++;
                        MinesField[y, x - 1]++;
                        MinesField[y, x + 1]++;
                        MinesField[y - 1, x + 1]++;
                        continue;
                    }
                    if (y == h - 1 && x == 0)
                    {
                        MinesField[y - 1, x]++;
                        MinesField[y, x + 1]++;
                        MinesField[y - 1, x + 1]++;
                        continue;
                    }
                    if (x == w - 1 && y == 0)
                    {
                        MinesField[y, x - 1]++;
                        MinesField[y + 1, x - 1]++;
                        MinesField[y + 1, x]++;
                    }
                }
            }
        }
        class RecordsManager
        {
            readonly private string filePath = "../../../data/records.json";
            List<Record> Records { get; set; }
            public RecordsManager()
            {
                StreamReader sr = new StreamReader(filePath);
                string str = sr.ReadToEnd();
                sr.Close();
                Record[] r = JsonSerializer.Deserialize<Record[]>(str);
                Records = r.ToList<Record>();
            }

            public void ShowRecords()
            {
                if (Records.Count == 0)
                {
                    MessageBox.Show("You have no recorded wins", "Your records");
                    return;
                }
                string tmp = "";
                for (int i = 0; i < Records.Count; i++)
                {
                    tmp += $"Name: {Records[i].Name}, date: {Records[i].Date}, time: {Records[i].WinTime}s \n";
                }
                MessageBox.Show(tmp, "Your records");
            }

            public void DeleteLastRecord()
            {
                Records.RemoveAt(Records.Count - 1);
                Save();
            }

            public void DeleteAllRecords()
            {
                Records.Clear();
                Save();
            }

            public void AddRecord(string N, string DT, int WT)
            {
                Record r = new Record{Name = N, Date = DT, WinTime = WT};
                Records.Add(r);
                Save();
            }

            private void Save()
            {
                Record[] r = Records.ToArray();
                string str = JsonSerializer.Serialize<Record[]>(r);
                StreamWriter sw = new StreamWriter(filePath);
                sw.Write(str);
                sw.Close();
            }

            private class Record
            {
                public string Name { get; set; }
                public string Date { get; set; }
                public int WinTime { get; set; }
            }
        }
    }
}
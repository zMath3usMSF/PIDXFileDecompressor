using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PIDXFileDecompressor
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Files (*.*)|*.*";
            if(openFileDialog.ShowDialog() == DialogResult.OK)
            {
                CheckFile(openFileDialog.FileName);
            }
        }
        private void CheckFile(string filePath)
        {
            FileStream fileStream = new FileStream(filePath, (FileMode)FileAccess.ReadWrite);

            byte[] fileMagic = new byte[4];
            fileStream.Read(fileMagic, 0, fileMagic.Length);
            bool isValid = fileMagic[0] == 0x20 && fileMagic[1] == 0x33 && fileMagic[2] == 0x3B &&
                           fileMagic[3] == 0x30 || fileMagic[3] == 0x31 ? true : false;

            int decompressType = fileMagic[3];

            if(isValid == true)
            {
                if(decompressType == 0x30)
                {
                    DecompressFileType30(filePath, fileStream);
                }
                else
                {
                    DecompressFileType31(filePath, fileStream);
                }
            }
            else
            {
                MessageBox.Show("Invalid File");
            }
        }
        private void DecompressFileType30(string filePath, FileStream fileStream)
        {
            fileStream.Seek(0x4, SeekOrigin.Begin);

            byte[] compressdFileLength = new byte[4];
            fileStream.Read(compressdFileLength, 0, compressdFileLength.Length);

            int fileLength = (int)fileStream.Length - 0x8;
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            List<byte> xored = new List<byte>();
            for (int i = 0; i < fileLength; i++)
            {
                int currentByte = fileStream.ReadByte();
                int xoredByte = currentByte ^ 0x72;
                xored.Add(Convert.ToByte(xoredByte));
            }

            string fileName = Path.GetFileName(filePath);
            string outputPath = Path.Combine(desktop, fileName);
            fileStream.Dispose();
            File.WriteAllBytes(outputPath, xored.ToArray());
        }
        private void DecompressFileType31(string filePath, FileStream fileStream)
        {
            fileStream.Seek(0x4, SeekOrigin.Begin);

            byte[] decompressedFileLengthBytes = new byte[4];
            fileStream.Read(decompressedFileLengthBytes, 0, decompressedFileLengthBytes.Length);
            int decompressedFileLength = BitConverter.ToInt32(decompressedFileLengthBytes, 0);

            int fileLength = (int)fileStream.Length - 0x8;
            int a0C = 0x0;
            int currentByte = 0x0;
            int count = 0;
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            List<byte> fileWordCache = new List<byte>();
            for (int i = 0; i < 0xFEE; i++)
            {
                fileWordCache.Add(0x0);
            }

            List<byte> xored = new List<byte>();
            int t1C = 0xFEE;
            for (int i = 0; i < fileLength; i++)
            {
                if (i == 0 || a0C == 0xFF)
                {
                    count = a0C & 1;
                    currentByte = fileStream.ReadByte() ^ 0x72;
                    a0C = 0xFF00 + currentByte;
                    count = a0C & 0x1;
                }
                else
                {
                    if ((a0C & 1) == 0)
                    {
                        count = 0;

                        currentByte = (byte)fileStream.ReadByte();
                        int xoredByte = currentByte ^ 0x72;

                        int currentByte2 = fileStream.ReadByte();
                        int xoredByte2 = currentByte2 ^ 0x72;
                        i++;

                        int t3 = xoredByte2 & 0xF0;
                        t3 = t3 << 0x4;

                        int t6 = xoredByte | t3;
                        t3 = xoredByte2 & 0xF;
                        xoredByte = t3 + 0x2;
                        while (true)
                        {
                            t3 = t6 + count;
                            t3 = t3 & 0xFFF;
                            int t4 = fileWordCache[t3];

                            xored.Add(Convert.ToByte(t4));
                            count++;
                            t1C++;

                            if (fileWordCache.Count > t1C)
                            {
                                fileWordCache[t1C - 1] = Convert.ToByte(t4);
                            }
                            else
                            {
                                fileWordCache.Add(Convert.ToByte(t4));
                            }

                            int at = (count <= xoredByte) ? 1 : 0;
                            if (at == 0)
                            {
                                a0C = a0C >> 1;
                                break;
                            }
                            t1C = t1C & 0xFFF;
                        }
                    }
                    else
                    {
                        a0C = a0C >> 1;

                        currentByte = fileStream.ReadByte();
                        int xoredByte = currentByte ^ 0x72;
                        xored.Add(Convert.ToByte(xoredByte));

                        count = t1C;

                        t1C++;
                        if (fileWordCache.Count > count)
                        {
                            fileWordCache[count] = Convert.ToByte(xoredByte);
                        }
                        else
                        {
                            fileWordCache.Add(Convert.ToByte(xoredByte));
                        }
                        t1C = t1C & 0xFFF;
                    }
                }
            }

            string fileName = Path.GetFileName(filePath);
            string outputPath = Path.Combine(desktop, fileName);
            fileStream.Dispose();
            File.WriteAllBytes(outputPath, xored.ToArray());
        }
        private void OldDecompressType31(string filePath, FileStream fileStream)
        {
            fileStream.Seek(0x4, SeekOrigin.Begin);
            byte[] ccsLengthBytes = new byte[4];

            fileStream.Read(ccsLengthBytes, 0, ccsLengthBytes.Length);
            int ccsLength = BitConverter.ToInt32(ccsLengthBytes, 0);
            int fileLength = (int)fileStream.Length - 0x8;
            int xorNumber = 0x72;
            int v1 = 0x8;
            int a0C = 0x0;
            int v0C = 0x0;
            int currentByte = 0x0;
            int count = 0;
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            List<byte> cacheCCS = new List<byte>();
            for (int i = 0; i < 0xFEE; i++)
            {
                cacheCCS.Add(0x0);
            }
            List<byte> cacheCCSBkp = new List<byte>();
            int generalCount = 0x0;
            List<byte> xored = new List<byte>();
            int t1C = 0xFEE;
            for (int i = 0; i < fileLength; i++)
            {
                if (i == 0 || a0C == 0xFF)
                {
                    count = a0C & 1;
                    currentByte = fileStream.ReadByte() ^ xorNumber;
                    v1++;
                    a0C = 0xFF00 + currentByte;
                    count = a0C & 0x1;
                }
                else
                {
                    if ((a0C & 1) == 0)
                    {
                        List<byte> bytesCache = new List<byte>();
                        count = 0;
                        currentByte = (byte)fileStream.ReadByte();
                        int xoredByte = currentByte ^ xorNumber;
                        int currentByte2 = fileStream.ReadByte();
                        i++;
                        int xoredByte2 = currentByte2 ^ xorNumber;
                        int t3 = xoredByte2 & 0xF0;
                        t3 = t3 << 0x4;
                        int t6 = xoredByte | t3;
                        t3 = xoredByte2 & 0xF;
                        xoredByte = t3 + 0x2;
                        while (true)
                        {
                            t3 = t6 + count;
                            t3 = t3 & 0xFFF;
                            int t4 = cacheCCS[t3];
                            t3 = ccsLength;
                            t3 = t3 < v0C ? 0 : 1;
                            int lengthCompare = (i - 1 < ccsLength) ? 1 : 0;
                            if (lengthCompare == 1)
                            {
                                xored.Add(Convert.ToByte(t4));
                                t3 = v0C;
                                t3 = t1C;
                                count++;
                                t1C++;
                                int at = (count <= xoredByte) ? 1 : 0;
                                v0C++;
                                if (cacheCCS.Count > t1C)
                                {
                                    cacheCCS[t1C - 1] = Convert.ToByte(t4);
                                }
                                else
                                {
                                    cacheCCS.Add(Convert.ToByte(t4));
                                }
                                generalCount = generalCount + count;
                                if (at == 0)
                                {
                                    a0C = a0C >> 1;
                                    break;
                                }
                                t1C = t1C & 0xFFF;
                            }
                            else
                            {
                                MessageBox.Show("OK");
                            }
                        }
                    }
                    else
                    {
                        a0C = a0C >> 1;
                        currentByte = fileStream.ReadByte();
                        count = ccsLength;
                        count = v0C < count ? 0 : 1;
                        int xoredByte = currentByte ^ xorNumber;
                        count = t1C;
                        xored.Add(Convert.ToByte(xoredByte));
                        t1C++;
                        if (cacheCCS.Count > count)
                        {
                            cacheCCS[count] = Convert.ToByte(xoredByte);
                        }
                        else
                        {
                            cacheCCS.Add(Convert.ToByte(xoredByte));
                        }
                        v0C++;
                        t1C = t1C & 0xFFF;
                    }
                }
            }

            string fileName = Path.GetFileName(filePath);
            string outputPath = Path.Combine(desktop, fileName);
            File.WriteAllBytes(outputPath, xored.ToArray());
            /*string outputPathCache = Path.Combine(desktop, "outputCache.bin");
            File.WriteAllBytes(outputPathCache, cacheCCS.ToArray());*/
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("A quick tool to unpack files from the PIDX container\ntool made by zMath3usMSF.");
        }
    }
}

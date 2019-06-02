using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GifViewer
{
    public class GifParser
    {
        public static byte[] GetDecodedData(GifRenderBlock imgBlock)
        {
            // Combine LZW compressed data
            List<byte> lzwData = new List<byte>();
            for (int i = 0; i < imgBlock.DataList.Length; i++)
            {
                for (int k = 0; k < imgBlock.DataList[i].Length; k++)
                {
                    lzwData.Add(imgBlock.DataList[i][k]);
                }
            }

            // LZW decode
            int needDataSize = imgBlock.Height * imgBlock.Width;
            byte[] decodedData = DecodeGifLZW(lzwData, imgBlock.LZWMinCodeSize, needDataSize);

            // Sort interlace GIF
            if (imgBlock.InterlaceFlag)
            {
                decodedData = SortInterlaceGifData(decodedData, imgBlock.Width);
            }
            return decodedData;
        }
        /// <summary>
        /// Sort interlace GIF data
        /// </summary>
        /// <param name="decodedData">Decoded GIF data</param>
        /// <param name="xNum">Pixel number of horizontal row</param>
        /// <returns>Sorted data</returns>
        private static byte[] SortInterlaceGifData(byte[] decodedData, int xNum)
        {
            int rowNo = 0;
            int dataIndex = 0;
            var newArr = new byte[decodedData.Length];
            // Every 8th. row, starting with row 0.
            for (int i = 0; i < newArr.Length; i++)
            {
                if (rowNo % 8 == 0)
                {
                    newArr[i] = decodedData[dataIndex];
                    dataIndex++;
                }
                if (i != 0 && i % xNum == 0)
                {
                    rowNo++;
                }
            }
            rowNo = 0;
            // Every 8th. row, starting with row 4.
            for (int i = 0; i < newArr.Length; i++)
            {
                if (rowNo % 8 == 4)
                {
                    newArr[i] = decodedData[dataIndex];
                    dataIndex++;
                }
                if (i != 0 && i % xNum == 0)
                {
                    rowNo++;
                }
            }
            rowNo = 0;
            // Every 4th. row, starting with row 2.
            for (int i = 0; i < newArr.Length; i++)
            {
                if (rowNo % 4 == 2)
                {
                    newArr[i] = decodedData[dataIndex];
                    dataIndex++;
                }
                if (i != 0 && i % xNum == 0)
                {
                    rowNo++;
                }
            }
            rowNo = 0;
            // Every 2nd. row, starting with row 1.
            for (int i = 0; i < newArr.Length; i++)
            {
                if (rowNo % 8 != 0 && rowNo % 8 != 4 && rowNo % 4 != 2)
                {
                    newArr[i] = decodedData[dataIndex];
                    dataIndex++;
                }
                if (i != 0 && i % xNum == 0)
                {
                    rowNo++;
                }
            }

            return newArr;
        }


        /// <summary>
        /// GIF LZW decode
        /// </summary>
        /// <param name="compData">LZW compressed data</param>
        /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
        /// <param name="needDataSize">Need decoded data size</param>
        /// <returns>Decoded data array</returns>
        private static byte[] DecodeGifLZW(List<byte> compData, int lzwMinimumCodeSize, int needDataSize)
        {
            int clearCode = 0;
            int finishCode = 0;

            // Initialize dictionary
            Dictionary<int, string> dic = new Dictionary<int, string>();
            int lzwCodeSize = 0;
            InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);

            // Convert to bit array
            byte[] compDataArr = compData.ToArray();
            var bitData = new BitArray(compDataArr);

            byte[] output = new byte[needDataSize];
            int outputAddIndex = 0;

            string prevEntry = null;

            bool dicInitFlag = false;

            int bitDataIndex = 0;

            // LZW decode loop
            while (bitDataIndex < bitData.Length)
            {
                if (dicInitFlag)
                {
                    InitDictionary(dic, lzwMinimumCodeSize, out lzwCodeSize, out clearCode, out finishCode);
                    dicInitFlag = false;
                }

                int key = bitData.GetNumeral(bitDataIndex, lzwCodeSize);

                string entry = null;

                if (key == clearCode)
                {
                    // Clear (Initialize dictionary)
                    dicInitFlag = true;
                    bitDataIndex += lzwCodeSize;
                    prevEntry = null;
                    continue;
                }
                else if (key == finishCode)
                {
                    // Exit
                    //   Debug.LogWarning("early stop code. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                    break;
                }
                else if (dic.ContainsKey(key))
                {
                    // Output from dictionary
                    entry = dic[key];
                }
                else if (key >= dic.Count)
                {
                    if (prevEntry != null)
                    {
                        // Output from estimation
                        entry = prevEntry + prevEntry[0];
                    }
                    else
                    {
                        //  Debug.LogWarning("It is strange that come here. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                        bitDataIndex += lzwCodeSize;
                        continue;
                    }
                }
                else
                {
                    //Debug.LogWarning("It is strange that come here. bitDataIndex:" + bitDataIndex + " lzwCodeSize:" + lzwCodeSize + " key:" + key + " dic.Count:" + dic.Count);
                    bitDataIndex += lzwCodeSize;
                    continue;
                }

                // Output
                // Take out 8 bits from the string.
                byte[] temp = Encoding.Unicode.GetBytes(entry);
                for (int i = 0; i < temp.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        output[outputAddIndex] = temp[i];
                        outputAddIndex++;
                    }
                }

                if (outputAddIndex >= needDataSize)
                {
                    // Exit
                    break;
                }

                if (prevEntry != null)
                {
                    // Add to dictionary
                    dic.Add(dic.Count, prevEntry + entry[0]);
                }

                prevEntry = entry;

                bitDataIndex += lzwCodeSize;

                if (lzwCodeSize == 3 && dic.Count >= 8)
                {
                    lzwCodeSize = 4;
                }
                else if (lzwCodeSize == 4 && dic.Count >= 16)
                {
                    lzwCodeSize = 5;
                }
                else if (lzwCodeSize == 5 && dic.Count >= 32)
                {
                    lzwCodeSize = 6;
                }
                else if (lzwCodeSize == 6 && dic.Count >= 64)
                {
                    lzwCodeSize = 7;
                }
                else if (lzwCodeSize == 7 && dic.Count >= 128)
                {
                    lzwCodeSize = 8;
                }
                else if (lzwCodeSize == 8 && dic.Count >= 256)
                {
                    lzwCodeSize = 9;
                }
                else if (lzwCodeSize == 9 && dic.Count >= 512)
                {
                    lzwCodeSize = 10;
                }
                else if (lzwCodeSize == 10 && dic.Count >= 1024)
                {
                    lzwCodeSize = 11;
                }
                else if (lzwCodeSize == 11 && dic.Count >= 2048)
                {
                    lzwCodeSize = 12;
                }
                else if (lzwCodeSize == 12 && dic.Count >= 4096)
                {
                    int nextKey = bitData.GetNumeral(bitDataIndex, lzwCodeSize);
                    if (nextKey != clearCode)
                    {
                        dicInitFlag = true;
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Initialize dictionary
        /// </summary>
        /// <param name="dic">Dictionary</param>
        /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
        /// <param name="lzwCodeSize">out LZW code size</param>
        /// <param name="clearCode">out Clear code</param>
        /// <param name="finishCode">out Finish code</param>
        private static void InitDictionary(Dictionary<int, string> dic, int lzwMinimumCodeSize, out int lzwCodeSize, out int clearCode, out int finishCode)
        {
            int dicLength = (int)Math.Pow(2, lzwMinimumCodeSize);

            clearCode = dicLength;
            finishCode = clearCode + 1;

            dic.Clear();

            for (int i = 0; i < dicLength + 2; i++)
            {
                dic.Add(i, ((char)i).ToString());
            }

            lzwCodeSize = lzwMinimumCodeSize + 1;
        }

        public static GifContainer Parse(string path)
        {
            GifContainer ret = new GifContainer();


            using (var fs = new FileStream(path, FileMode.Open))
            {
                ret.Header.Read(fs);
                ret.LogicalScreen.Read(fs);
                int findex = 0;
                //lookahead 
                while (true)
                {
                    var b = fs.ReadByte();
                    fs.Position--;
                    if (b == 0x3b) break;

                    if (b == 0x21)
                    {
                        b = fs.ReadByte();
                        var b2 = fs.ReadByte();
                        fs.Position -= 2; ;
                        if (b2 == 0xf9)
                        {
                            ret.Data.Add(GraphicControlExtension.Read(fs));
                        }
                        else
                        if (b2 == 0xff)
                        {
                            ret.Data.Add(GifApplicationExtension.Read(fs));
                        }
                        else
                        if (b2 == 0xfe)
                        {
                            ret.Data.Add(GifCommentBlock.Read(fs));
                        }
                        else
                        {
                            throw new GifParsingException("unknown header: " + b.ToString("X2") + b2.ToString("X2") + ", shift: " + fs.Position);
                        }
                    }
                    else
                    if (b == 0x2c)
                    {
                        var r = GifRenderBlock.Read(fs);
                        ret.Data.Add(r);
                        r.FrameIndex = findex++;
                    }
                    else
                    {
                        throw new GifParsingException("unknown header: " + b.ToString("X2") + ", shift: " + fs.Position);

                    }
                }

            }
            //ret.Bmp = Bitmap.FromFile(path) as Bitmap;
            ret.UpdateInfo();
            return ret;
        }
    }

}

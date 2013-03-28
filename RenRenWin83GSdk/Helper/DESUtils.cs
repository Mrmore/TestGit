using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security;
using Windows.Security.Cryptography;
using System.Reflection;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
//using Org.BouncyCastle.Crypto.Paddings;
//using Org.BouncyCastle.Crypto.Modes;
//using Org.BouncyCastle.Crypto.Engines;
//using Org.BouncyCastle.Crypto.Parameters;
//using Org.BouncyCastle.Utilities.Encoders;
//using Org.BouncyCastle.Crypto;
//using Org.BouncyCastle.Security;

namespace RenRenAPI.Helper
{
    public class DESUtils
    {
        //public static string Decode(string str, string key)
        //{
        //    try
        //    {
        //        DESCryptoServiceProvider provider = new DESCryptoServiceProvider();
        //        // 密钥
        //        provider.Key = Encoding.ASCII.GetBytes(key.Substring(0, 8));
        //        // 偏移量
        //        provider.IV = Encoding.ASCII.GetBytes(key.Substring(0, 8));
        //        byte[] buffer = new byte[str.Length / 2];
        //        for (int i = 0; i < (str.Length / 2); i++)
        //        {
        //            int num2 = Convert.ToInt32(str.Substring(i * 2, 2), 0x10);
        //            buffer[i] = (byte)num2;
        //        }
        //        MemoryStream stream = new MemoryStream();
        //        CryptoStream stream2 = new CryptoStream(stream, provider.CreateDecryptor(), CryptoStreamMode.Write);
        //        stream2.Write(buffer, 0, buffer.Length);
        //        stream2.FlushFinalBlock();
        //        stream.Close();
        //        return Encoding.GetEncoding("GB2312").GetString(stream.ToArray());
        //    }
        //    catch (Exception) { return ""; }
        //}

        // To hold the initialised DES cipher
        //private static IBufferedCipher cipher = null;

        /*
            * This method performs all the decryption and writes
            * the plain text to the buffered output stream created
            * previously.
            */
        //static public string Decode(string encyStr, string key, string iv)
        //{
        //    /* 
        //            * Setup the DES cipher engine, with PKCS5PADDING
        //            * in CBC mode.
        //    //        */

        //    //if (cipher == null)
        //    //{
        //    //    cipher = CipherUtilities.GetCipher("DES/CBC/PKCS5PADDING");
        //    //}

        //    string result = null;
        //    //byte[] key_ = Encoding.UTF8.GetBytes(key.Substring(0, 8));
        //    //byte[] iv_ = Encoding.UTF8.GetBytes(iv.Substring(0, 8));

        //    //cipher.Init(false, new ParametersWithIV(new DesParameters(key_), iv_));

        //    //try
        //    //{
        //    //    int outL = 0;
        //    //    byte[] outblock = null;
        //    //    string decodeUrl = Uri.UnescapeDataString(encyStr);
        //    //    byte[] inblock = Convert.FromBase64String(decodeUrl);
        //    //    outblock = new byte[cipher.GetOutputSize(inblock.Length)];

        //    //    outL = cipher.ProcessBytes(inblock, 0, inblock.Length, outblock, 0);
        //    //    /*
        //    //             * Before we write anything out, we need to make sure
        //    //             * that we've got something to write out. 
        //    //             */
        //    //    if (outL > 0)
        //    //    {
        //    //        cipher.DoFinal(outblock, outL);
        //    //    }
        //    //    result = Encoding.UTF8.GetString(outblock, 0, outblock.Length);
        //    //}
        //    //catch (IOException) { }

        //    return result;
        //}

        static public string Decode(string encyStr, string key, string _iv)
        {
            String strDecrypted = string.Empty;
            try
            {


                string decodeUrl = Uri.UnescapeDataString(encyStr);
                byte[] inblock = Convert.FromBase64String(decodeUrl);


                // Declare a buffer to contain the decrypted data.t
                IBuffer buffDecrypted;

                // Open an symmetric algorithm provider for the specified algorithm. 
                SymmetricKeyAlgorithmProvider objAlg = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.DesCbcPkcs7);

                IBuffer buffEncrypt = CryptographicBuffer.CreateFromByteArray(inblock);

                IBuffer iv = CryptographicBuffer.ConvertStringToBinary(_iv, BinaryStringEncoding.Utf8);

                //IBuffer keyMaterial = CryptographicBuffer.GenerateRandom(8);
                IBuffer keyMaterial = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
                CryptographicKey _key = objAlg.CreateSymmetricKey(keyMaterial);

                // The input key must be securely shared between the sender of the encrypted message
                // and the recipient. The initialization vector must also be shared but does not
                // need to be shared in a secure manner. If the sender encodes a message string 
                // to a buffer, the binary encoding method must also be shared with the recipient.
                buffDecrypted = CryptographicEngine.Decrypt(_key, buffEncrypt, iv);

                // Convert the decrypted buffer to a string (for display). If the sender created the
                // original message buffer from a string, the sender must tell the recipient what 
                // BinaryStringEncoding value was used. Here, BinaryStringEncoding.Utf8 is used to
                // convert the message to a buffer before encryption and to convert the decrypted
                // buffer back to the original plaintext.
                strDecrypted = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buffDecrypted);
            }
            catch
            {
            }

            return strDecrypted;
        }

    }
}

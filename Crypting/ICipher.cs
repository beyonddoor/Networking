using System;
using System.Collections.Generic;
using System.Text;

namespace SolarGames.Networking.Crypting
{
    /// <summary>
    /// 简单加密接口
    /// </summary>
    public interface ICipher
    {
        void Encrypt(ref byte[] input, int len);
        void Decrypt(ref byte[] input, int len);
    }
}

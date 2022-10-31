using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WCell.Core.Network;

namespace WCell.Core
{
    public static class Asda2EncodingHelper
    {
        private static readonly char[] RuChars;

        private static readonly char[] EngTranslitChars;

        private static readonly byte[] RuEncode;

        private static readonly char[] ArChars;

        private static readonly char[] EngTranslitCharsForAr;

        private static readonly byte[] ArEncode;

        private static readonly byte[] RuEncodeTranslit;

        private static readonly byte[] ArEncodeTranslit;

        public static char[] RuCharacters;

        public static char[] ArCharacters;

        public static byte[] ArCharactersReversed;

        public static byte[] ArCharactersReversedTranslit;

        public static byte[] RuCharactersReversed;

        public static byte[] RuCharactersReversedTranslit;

        public static char[] ForReverseTranslit;

        public static bool[] AllowedEnglishSymbols;

        public static bool[] AllowedEnglishNameSymbols;

        public static string AllowedEnglishSymbolsStr;

        public static string AllowedEnglishNameSymbolsStr;

        public static bool[] AllowedArabicSymbols;

        public static bool[] AllowedArabicNameSymbols;

        public static string AllowedArabicSymbolsStr;

        public static string AllowedArabicNameSymbolsStr;

        static Asda2EncodingHelper()
        {
            Asda2EncodingHelper.RuChars = "йцукенгшщзхъфывапролджэячсмитьбюЙЦУКЕНГШЩЗХЪФЫВАПРОЛДЖЭЯЧСМИТЬБЮёЁ".ToArray();
            Asda2EncodingHelper.EngTranslitChars = "ycukeng#%zh'f@vaproldj394smit[bwYCUKENG#%ZH]F@VAPROLDJ394SMIT'BW<<".ToArray();
            Asda2EncodingHelper.RuEncode = "E9 F6 F3 EA E5 ED E3 F8 F9 E7 F5 FA F4 FB E2 E0 EF F0 EE EB E4 E6 FD FF F7 F1 EC E8 F2 FC E1 FE C9 D6 D3 CA C5 CD C3 D8 D9 C7 D5 DA D4 DB C2 C0 CF D0 CE CB C4 C6 DD DF D7 D1 CC C8 D2 DC C1 DE B8 A8".AsBytes();
            Asda2EncodingHelper.ArChars = "ضصثقفغعهخحجدشسيبلاتنمكطئءؤرلاىةوزظذلآآ,.><أ".ToArray();
            Asda2EncodingHelper.EngTranslitCharsForAr = "ycukeng#%zh'f@vaproldj394smit[bwYCUKENG#%ZH]F@VAPROLDJ394SMIT'BW<<".ToArray();
            Asda2EncodingHelper.ArEncode = "D6 D5 CB DE DD DB DA E5 CE CD CC CF D4 D3 ED C8 E1 C7 CA E4 E3 DF D8 C6 C1 C4 D1 E1 C7 EC C9 E6 D2 D9 D0 E1 C2 C2 2C 2E 3E 3C C3".AsBytes();
            Asda2EncodingHelper.RuEncodeTranslit = Encoding.ASCII.GetBytes(Asda2EncodingHelper.EngTranslitChars);
            Asda2EncodingHelper.ArEncodeTranslit = Encoding.ASCII.GetBytes(Asda2EncodingHelper.EngTranslitCharsForAr);
            Asda2EncodingHelper.RuCharacters = new char[256];
            Asda2EncodingHelper.ArCharacters = new char[256];
            Asda2EncodingHelper.ArCharactersReversed = new byte[65535];
            Asda2EncodingHelper.ArCharactersReversedTranslit = new byte[65535];
            Asda2EncodingHelper.RuCharactersReversed = new byte[65535];
            Asda2EncodingHelper.RuCharactersReversedTranslit = new byte[65535];
            Asda2EncodingHelper.ForReverseTranslit = new char[65535];
            Asda2EncodingHelper.AllowedEnglishSymbols = new bool[65535];
            Asda2EncodingHelper.AllowedEnglishNameSymbols = new bool[65535];
            Asda2EncodingHelper.AllowedEnglishSymbolsStr = "`1234567890-=qwertyuiop[]asdfghjkl;'zxcvbnm,./~!@#$%^&*()_+QWERTYUIOP{}ASDFGHJKL:\"ZXCVBNM<>?; \\";
            Asda2EncodingHelper.AllowedEnglishNameSymbolsStr = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
            Asda2EncodingHelper.AllowedArabicSymbols = new bool[65535];
            Asda2EncodingHelper.AllowedArabicNameSymbols = new bool[65535];
            Asda2EncodingHelper.AllowedArabicSymbolsStr = "`1234567890-=ضصثقفغعهخحجدذشسيبلاتنمكطظزوةىلارؤءئ[];',./~!@#$%^&*()_+{}:\"<>?; ";
            Asda2EncodingHelper.AllowedArabicNameSymbolsStr = " ضصثقفغعهخحجدشسيبلاتنمكطذئءؤرلاىةوزظ.123456789";
            for (int i3 = 0; i3 < 256; i3++)
            {
                Asda2EncodingHelper.RuCharacters[i3] = (char)i3;
                Asda2EncodingHelper.ArCharacters[i3] = (char)i3;
            }
            for (int i3 = 0; i3 < Asda2EncodingHelper.RuEncode.Length; i3++)
            {
                Asda2EncodingHelper.RuCharacters[Asda2EncodingHelper.RuEncode[i3]] = Asda2EncodingHelper.RuChars[i3];
            }
            for (int i3 = 0; i3 < Asda2EncodingHelper.ArEncode.Length; i3++)
            {
                Asda2EncodingHelper.ArCharacters[Asda2EncodingHelper.ArEncode[i3]] = Asda2EncodingHelper.ArChars[i3];
            }
            for (int i3 = 0; i3 < Asda2EncodingHelper.RuCharactersReversed.Length; i3++)
            {
                if (i3 >= 256)
                {
                    Asda2EncodingHelper.RuCharactersReversed[i3] = 63;
                    Asda2EncodingHelper.RuCharactersReversedTranslit[i3] = 63;
                    Asda2EncodingHelper.ForReverseTranslit[i3] = '?';
                }
                else
                {
                    Asda2EncodingHelper.RuCharactersReversed[i3] = (byte)i3;
                    Asda2EncodingHelper.RuCharactersReversedTranslit[i3] = (byte)i3;
                    Asda2EncodingHelper.ForReverseTranslit[i3] = (char)i3;
                }
            }
            for (int i3 = 0; i3 < Asda2EncodingHelper.ArCharactersReversed.Length; i3++)
            {
                if (i3 >= 256)
                {
                    Asda2EncodingHelper.ArCharactersReversed[i3] = 63;
                    Asda2EncodingHelper.ArCharactersReversedTranslit[i3] = 63;
                    Asda2EncodingHelper.ForReverseTranslit[i3] = '?';
                }
                else
                {
                    Asda2EncodingHelper.ArCharactersReversed[i3] = (byte)i3;
                    Asda2EncodingHelper.ArCharactersReversedTranslit[i3] = (byte)i3;
                    Asda2EncodingHelper.ForReverseTranslit[i3] = (char)i3;
                }
            }
            for (int i3 = 0; i3 < Asda2EncodingHelper.RuChars.Length; i3++)
            {
                Asda2EncodingHelper.RuCharactersReversed[Asda2EncodingHelper.RuChars[i3]] = Asda2EncodingHelper.RuEncode[i3];
                Asda2EncodingHelper.RuCharactersReversedTranslit[Asda2EncodingHelper.RuChars[i3]] = Asda2EncodingHelper.RuEncodeTranslit[i3];
            }
            for (int i3 = 0; i3 < Asda2EncodingHelper.ArChars.Length; i3++)
            {
                Asda2EncodingHelper.ArCharactersReversed[Asda2EncodingHelper.ArChars[i3]] = Asda2EncodingHelper.ArEncode[i3];
                Asda2EncodingHelper.ArCharactersReversedTranslit[Asda2EncodingHelper.ArChars[i3]] = Asda2EncodingHelper.ArEncodeTranslit[i3];
            }
            for (int i3 = 0; i3 < Asda2EncodingHelper.EngTranslitChars.Length; i3++)
            {
                Asda2EncodingHelper.ForReverseTranslit[Asda2EncodingHelper.EngTranslitChars[i3]] = Asda2EncodingHelper.RuChars[i3];
            }
            Asda2EncodingHelper.InitAllowedEnglishSymbols();
        }

        public static string Decode(byte[] data, Locale locale)
        {
            Encoding.Default.GetString(data);
            char[] r = new char[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                r[i] = locale == Locale.Ru ? Asda2EncodingHelper.RuCharacters[data[i]] : Asda2EncodingHelper.ArCharacters[data[i]];
            }
            return new string(r);
        }

        public static byte[] Encode(string s, Locale locale)
        {
            Encoding.Default.GetBytes(s);
            byte[] r = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                r[i] = locale == Locale.Ru ? Asda2EncodingHelper.RuCharactersReversed[s[i]] : Asda2EncodingHelper.ArCharactersReversed[s[i]];
            }
            return r;
        }

        public static byte[] EncodeTranslit(string s)
        {
            byte[] r = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                r[i] = Asda2EncodingHelper.RuCharactersReversedTranslit[s[i]];
            }
            return r;
        }

        public static string Translit(string name)
        {
            char[] res = new char[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                res[i] = (char)Asda2EncodingHelper.RuCharactersReversedTranslit[name[i]];
            }
            return new string(res);
        }

        public static string ReverseTranslit(string name)
        {
            char[] res = new char[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                res[i] = Asda2EncodingHelper.ForReverseTranslit[name[i]];
            }
            return new string(res);
        }

        private static void InitAllowedEnglishSymbols()
        {
            string allowedEnglishSymbolsStr = Asda2EncodingHelper.AllowedEnglishSymbolsStr;
            foreach (char b in allowedEnglishSymbolsStr)
            {
                Asda2EncodingHelper.AllowedEnglishSymbols[b] = true;
            }
            allowedEnglishSymbolsStr = Asda2EncodingHelper.AllowedEnglishNameSymbolsStr;
            foreach (char b in allowedEnglishSymbolsStr)
            {
                Asda2EncodingHelper.AllowedEnglishNameSymbols[b] = true;
            }
        }

        public static bool IsPrueEnglish(string s)
        {
            return s.All((char c) => Asda2EncodingHelper.AllowedEnglishSymbols[c]);
        }

        public static bool IsPrueEnglishName(string s)
        {
            return s.All((char c) => Asda2EncodingHelper.AllowedEnglishNameSymbols[c]);
        }

        public static Locale MinimumAvailableLocale(Locale clientLocale, string message)
        {
            bool isPrueEnglish = Asda2EncodingHelper.IsPrueEnglish(message);
            Locale locale = Locale.En;
            if (!isPrueEnglish)
            {
                locale = clientLocale;
            }
            return locale;
        }
    }
}

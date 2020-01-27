using System.Linq;
using System.Text;

namespace SysBot.Base
{
    /// <summary>
    /// Encodes commands for a <see cref="SwitchBot"/> to be sent as a <see cref="byte"/> array.
    /// </summary>
    public static class SwitchCommand
    {
        private static readonly Encoding Encoder = Encoding.UTF8;
        private static byte[] Encode(string command) => Encoder.GetBytes(command + "\r\n");

        /* 
         *
         * Controller Button Commands
         *
         */

        /// <summary>
        /// Presses and releases a <see cref="SwitchButton"/> for 50ms.
        /// </summary>
        /// <param name="button">Button to click.</param>
        /// <remarks>Press &amp; Release timing is performed by the console automatically.</remarks>
        /// <returns>Encoded command bytes</returns>
       public static byte[] Click(SwitchButton button) => Encode($"click {GetButtonState(button)}");

        /// <summary>
        /// Presses and does NOT release a <see cref="SwitchButton"/>.
        /// </summary>
        /// <param name="button">Button to hold.</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Hold(SwitchButton button) => Encode($"press {GetButtonState(button)}");

        /// <summary>
        /// Releases the held <see cref="SwitchButton"/>.
        /// </summary>
        /// <param name="button">Button to release.</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Release(SwitchButton button) => Encode($"release {GetButtonState(button)}");


        private static string GetButtonState(SwitchButton button)
        {
            switch((int)button)
            {
                case 0:
                    return "A";
                case 1:
                    return "B";
                case 2:
                    return "X";
                case 3:
                    return "Y";
                case 4:
                    return "RSTICK";
                case 5:
                    return "LSTICK";
                case 6:
                    return "L";
                case 7:
                    return "R";
                case 8:
                    return "ZL";
                case 9:
                    return "ZR";
                case 10:
                    return "PLUS";
                case 11:
                    return "MINUS";
                case 12:
                    return "DUP";
                case 13:
                    return "DDOWN";
                case 14:
                    return "DLEFT";
                case 15:
                    return "DRIGHT";
                case 16:
                    return "HOME";
                case 17:
                    return "CAPTURE";
                default:
                    return "";
            }
        }

        /* 
         *
         * Controller Stick Commands
         *
         */

        /// <summary>
        /// Sets the specified <see cref="stick"/> to the desired <see cref="x"/> and <see cref="y"/> positions.
        /// </summary>
        /// <returns>Encoded command bytes</returns>
        public static byte[] SetStick(SwitchStick stick, int x, int y) => Encode($"setStick {GetStickState(stick)} {x} {y}");

        /// <summary>
        /// Resets the specified <see cref="stick"/> to (0,0)
        /// </summary>
        /// <returns>Encoded command bytes</returns>
        public static byte[] ResetStick(SwitchStick stick) => SetStick(stick, 0, 0);

        private static string GetStickState(SwitchStick stick)
        {
            switch ((int)stick)
            {
                case 0:
                    return "LEFT";
                case 1:
                    return "RIGHT";
                default:
                    return "";
            }
        }

        /* 
         *
         * Memory I/O Commands
         *
         */

        /// <summary>
        /// Requests the Bot to send <see cref="count"/> bytes from <see cref="offset"/>.
        /// </summary>
        /// <param name="offset">Address of the data</param>
        /// <param name="count">Amount of bytes</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Peek(uint offset, int count) => Encode($"peek 0x{offset:X8} {count}");

        /// <summary>
        /// Sends the Bot <see cref="data"/> to be written to <see cref="offset"/>.
        /// </summary>
        /// <param name="offset">Address of the data</param>
        /// <param name="data">Data to write</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Poke(uint offset, byte[] data) => Encode($"poke 0x{offset:X8} 0x{string.Concat(data.Select(z => $"{z:X2}"))}");
    }
}

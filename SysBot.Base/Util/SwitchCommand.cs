using System.Linq;
using System.Text;

namespace SysBot.Base
{
    /// <summary>
    /// Encodes commands to be sent as a <see cref="byte"/> array to a Nintendo Switch running a sys-module.
    /// </summary>
    public static class SwitchCommand
    {
        private static readonly Encoding Encoder = Encoding.ASCII;

        private static byte[] Encode(string command, bool crlf = true)
        {
            if (crlf)
                command += "\r\n";
            return Encoder.GetBytes(command);
        }

        /// <summary>
        /// Removes the virtual controller from the bot. Allows physical controllers to control manually.
        /// </summary>
        /// <returns>Encoded command bytes</returns>
        public static byte[] DetachController(bool crlf = true) => Encode("detachController", crlf);

        /// <summary>
        /// Configures the sys-botbase parameter to the specified value.
        /// </summary>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Configure(SwitchConfigureParameter p, int ms, bool crlf = true) => Encode($"configure {p} {ms}", crlf);

        /* 
         *
         * Controller Button Commands
         *
         */

        /// <summary>
        /// Presses and releases a <see cref="SwitchButton"/> for 50ms.
        /// </summary>
        /// <remarks>Press &amp; Release timing is performed by the console automatically.</remarks>
        /// <param name="button">Button to click.</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Click(SwitchButton button, bool crlf = true) => Encode($"click {button}", crlf);

        /// <summary>
        /// Presses and does NOT release a <see cref="SwitchButton"/>.
        /// </summary>
        /// <param name="button">Button to hold.</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Hold(SwitchButton button, bool crlf = true) => Encode($"press {button}", crlf);

        /// <summary>
        /// Releases the held <see cref="SwitchButton"/>.
        /// </summary>
        /// <param name="button">Button to release.</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Release(SwitchButton button, bool crlf = true) => Encode($"release {button}", crlf);

        /* 
         *
         * Controller Stick Commands
         *
         */

        /// <summary>
        /// Sets the specified <see cref="stick"/> to the desired <see cref="x"/> and <see cref="y"/> positions.
        /// </summary>
        /// <param name="stick">Stick to reset</param>
        /// <param name="x">X position</param>
        /// <param name="y">Y position</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] SetStick(SwitchStick stick, short x, short y, bool crlf = true) => Encode($"setStick {stick} {x} {y}", crlf);

        /// <summary>
        /// Resets the specified <see cref="stick"/> to (0,0)
        /// </summary>
        /// <param name="stick">Stick to reset</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] ResetStick(SwitchStick stick, bool crlf = true) => SetStick(stick, 0, 0, crlf);

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
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Peek(uint offset, int count, bool crlf = true) => Encode($"peek 0x{offset:X8} {count}", crlf);

        /// <summary>
        /// Sends the Bot <see cref="data"/> to be written to <see cref="offset"/>.
        /// </summary>
        /// <param name="offset">Address of the data</param>
        /// <param name="data">Data to write</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] Poke(uint offset, byte[] data, bool crlf = true) => Encode($"poke 0x{offset:X8} 0x{string.Concat(data.Select(z => $"{z:X2}"))}", crlf);

        /// <summary>
        /// Requests the Bot to send <see cref="count"/> bytes from absolute <see cref="offset"/>.
        /// </summary>
        /// <param name="offset">Absolute address of the data</param>
        /// <param name="count">Amount of bytes</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] PeekAbsolute(ulong offset, int count, bool crlf = true) => Encode($"peekAbsolute 0x{offset:X16} {count}", crlf);

        /// <summary>
        /// Sends the Bot <see cref="data"/> to be written to absolute <see cref="offset"/>.
        /// </summary>
        /// <param name="offset">Absolute address of the data</param>
        /// <param name="data">Data to write</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] PokeAbsolute(ulong offset, byte[] data, bool crlf = true) => Encode($"pokeAbsolute 0x{offset:X16} 0x{string.Concat(data.Select(z => $"{z:X2}"))}", crlf);

        /// <summary>
        /// Requests the Bot to send <see cref="count"/> bytes from main <see cref="offset"/>.
        /// </summary>
        /// <param name="offset">Main NSO address of the data</param>
        /// <param name="count">Amount of bytes</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] PeekMain(ulong offset, int count, bool crlf = true) => Encode($"peekMain 0x{offset:X16} {count}", crlf);

        /// <summary>
        /// Sends the Bot <see cref="data"/> to be written to main <see cref="offset"/>.
        /// </summary>
        /// <param name="offset">Main NSO address of the data</param>
        /// <param name="data">Data to write</param>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] PokeMain(ulong offset, byte[] data, bool crlf = true) => Encode($"pokeMain 0x{offset:X16} 0x{string.Concat(data.Select(z => $"{z:X2}"))}", crlf);

        /* 
         *
         * Process Info Commands
         *
         */

        /// <summary>
        /// Requests the main NSO base of attached process.
        /// </summary>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] GetMainNsoBase(bool crlf = true) => Encode("getMainNsoBase", crlf);

        /// <summary>
        /// Requests the heap base of attached process.
        /// </summary>
        /// <param name="crlf">Line terminator (unused by USB's protocol)</param>
        /// <returns>Encoded command bytes</returns>
        public static byte[] GetHeapBase(bool crlf = true) => Encode("getHeapBase", crlf);
    }
}

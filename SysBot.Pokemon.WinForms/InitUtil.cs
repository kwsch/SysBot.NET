#if NETFRAMEWORK
using PKHeX.Core;
using PKHeX.Drawing;

namespace SysBot.Pokemon.WinForms
{
    public static class InitUtil
    {
        public static void InitializeStubs()
        {
            var sav8 = new SAV8SWSH();
            SetUpSpriteCreator(sav8);
        }

        private static void SetUpSpriteCreator(SaveFile sav)
        {
            SpriteUtil.Initialize(sav);
            StreamSettings.CreateSpriteFile = (pk, fn) =>
            {
                var png = pk.Sprite();
                png.Save(fn);
            };
        }
    }
}
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.SleepingPlayerWatcher
{
    public static class Util
    {
        public static string Translate(string TranslationKey, params object[] Placeholders) =>
            Plugin.Inst.Translations.Instance.Translate(TranslationKey, Placeholders);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace ItemManager;

public partial class LocalizeKey
{
    public string GetCurrent(string lang)
    {
        if (Localizations.ContainsKey(lang))
        {
            return Localizations[lang];
        }

        return "";
    }

    public string English() => GetCurrent("English");
    public string Swedish() => GetCurrent("Swedish");
    public string French() => GetCurrent("French");
    public string Italian() => GetCurrent("Italian");
    public string German() => GetCurrent("German");
    public string Spanish() => GetCurrent("Spanish");
    public string Russian() => GetCurrent("Russian");
    public string Romanian() => GetCurrent("Romanian");
    public string Bulgarian() => GetCurrent("Bulgarian");
    public string Macedonian() => GetCurrent("Macedonian");
    public string Finnish() => GetCurrent("Finnish");
    public string Danish() => GetCurrent("Danish");
    public string Norwegian() => GetCurrent("Norwegian");
    public string Icelandic() => GetCurrent("Icelandic");
    public string Turkish() => GetCurrent("Turkish");
    public string Lithuanian() => GetCurrent("Lithuanian");
    public string Czech() => GetCurrent("Czech");
    public string Hungarian() => GetCurrent("Hungarian");
    public string Slovak() => GetCurrent("Slovak");
    public string Polish() => GetCurrent("Polish");
    public string Dutch() => GetCurrent("Dutch");
    public string Portuguese_European() => GetCurrent("Portuguese_European");
    public string Portuguese_Brazilian() => GetCurrent("Portuguese_Brazilian");
    public string Chinese() => GetCurrent("Chinese");
    public string Japanese() => GetCurrent("Japanese");
    public string Korean() => GetCurrent("Korean");
    public string Hindi() => GetCurrent("Hindi");
    public string Thai() => GetCurrent("Thai");
    public string Abenaki() => GetCurrent("Abenaki");
    public string Croatian() => GetCurrent("Croatian");
    public string Georgian() => GetCurrent("Georgian");
    public string Greek() => GetCurrent("Greek");
    public string Serbian() => GetCurrent("Serbian");
    public string Ukrainian() => GetCurrent("Ukrainian");
}

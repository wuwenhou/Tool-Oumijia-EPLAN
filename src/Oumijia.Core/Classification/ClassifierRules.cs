using System;
using System.Collections.Generic;

namespace Oumijia.Core.Classification;

/// <summary>One category rule: first matching rule (in list order) wins.</summary>
public sealed class CategoryRule
{
    public string Folder { get; set; } = "";
    public List<string> Keywords { get; set; } = new();
    public List<string> EClassPrefixes { get; set; } = new();
}

/// <summary>
/// Configurable rule set for automatic manufacturer / category classification.
/// Written to "rules.json" on first run so users can tune it without rebuilding.
/// Keywords are matched case-insensitively against product group, designation,
/// part number and file name.
/// </summary>
public sealed class ClassifierRules
{
    public string FallbackCategory { get; set; } = "_Unsorted";
    public string FallbackManufacturer { get; set; } = "_Unknown";

    /// <summary>Raw manufacturer text (lowercase key) =&gt; folder name.</summary>
    public Dictionary<string, string> ManufacturerAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<CategoryRule> Categories { get; set; } = new();

    public static ClassifierRules CreateDefault()
    {
        var rules = new ClassifierRules();

        void Alias(string raw, string folder) => rules.ManufacturerAliases[raw] = folder;
        Alias("siemens", "Siemens");
        Alias("siemens ag", "Siemens");
        Alias("abb", "ABB");
        Alias("abb ltd", "ABB");
        Alias("abb ltd.", "ABB");
        Alias("schneider electric", "Schneider Electric");
        Alias("schneider", "Schneider Electric");
        Alias("phoenix contact", "Phoenix Contact");
        Alias("phoenix contact gmbh & co. kg", "Phoenix Contact");
        Alias("weidmüller", "Weidmüller");
        Alias("weidmueller", "Weidmüller");
        Alias("weidmuller", "Weidmüller");
        Alias("rittal", "Rittal");
        Alias("eaton", "Eaton");
        Alias("eaton corporation", "Eaton");
        Alias("rockwell automation", "Rockwell Automation");
        Alias("mitsubishi electric", "Mitsubishi Electric");
        Alias("omron", "Omron");
        Alias("festo", "Festo");
        Alias("ifm", "ifm electronic");
        Alias("ifm electronic", "ifm electronic");
        Alias("murrelektronik", "Murrelektronik");
        Alias("wago", "WAGO");
        Alias("wago kontakttechnik", "WAGO");
        Alias("te connectivity", "TE Connectivity");
        Alias("legrand", "Legrand");
        Alias("hager", "Hager");
        Alias("ls electric", "LS Electric");
        Alias("ls industrial systems", "LS Electric");
        Alias("mitsubishi", "Mitsubishi Electric");
        Alias("sick", "SICK");
        Alias("turck", "Turck");
        Alias("balluff", "Balluff");
        Alias("pilz", "PILZ");
        Alias("beckhoff", "Beckhoff");
        Alias("b&r", "B&R");
        Alias("bachmann", "Bachmann");
        Alias("carlo gavazzi", "Carlo Gavazzi");
        Alias("eao", "EAO");
        Alias("gefran", "Gefran");
        Alias("pepperl+fuchs", "Pepperl+Fuchs");

        void Cat(string folder, string pipeSeparatedKeywords)
        {
            rules.Categories.Add(new CategoryRule
            {
                Folder = folder,
                Keywords = new List<string>(pipeSeparatedKeywords.Split('|'))
            });
        }

        // Order matters: first match wins. More specific categories come first.
        Cat("MotorProtection", "motor protection|motorschutz|overload relay|thermal overload|3ru|motor circuit|schutzrelais");
        Cat("RCD", "rcd|rcbo|rccb|residual current|fi-schalter|fi schutz|ds201|pfi|fehlerstrom");
        Cat("MCCB", "mccb|moulded case|molded case|leistungsschalter|3va|nsx|easypact|masterpact|xt1|xt2|xt3");
        Cat("MCB", "mcb|miniature circuit breaker|leitungsschutz|s200|s201|s202|5sl|5sy|5sp|c60|ic60|faz|plsm|nbn");
        Cat("Contactor", "contactor|schütz|schuetz|lc1|3rt|sirius|hilfsschütz");
        Cat("Relay", "relay|relais|interface relay|coupling relay|optocoupler|my2|rt4|relay module");
        Cat("PLC", "plc|programmable logic|s7-|simatic|logo!|melsec|modicon|controllogix|compactlogix|automation controller");
        Cat("HMI", "hmi|touch panel|panelview|comfort panel|basic panel|operator panel|display 7|display 10");
        Cat("PSU", "power supply|netzteil|psu|sitop|quint|trio|dc power supply|mean well|meanwell");
        Cat("Surge", "surge|spd|lightning|blitz|overvoltage|überspannung|ueberspannung|arrester");
        Cat("Fuse", "fuse|sicherung|fusible|5x20|5x25|class cc|cartridge fuse|gmc");
        Cat("Transformer", "transformer|trafo|transformator");
        Cat("Drive", "drive|frequenzumrichter|inverter|vfd|sinamics|altivar|ac drive|dc drive|servo drive|soft starter|softstart");
        Cat("Sensor", "sensor|inductive|capacitive|photoelectric|proximity|light barrier|lichtschranke|encoder|transducer");
        Cat("Terminal", "terminal block|reihenklemme|klemme|push-in|wdu|uk 2|uk 5|st 2|zrk|a-series terminal");
        Cat("Connector", "connector|stecker|coupling|socket|han |plug |cable gland|соединитель");
        Cat("Cable", "cable|kabel|leitung|liyy|nyy|h07|h05|leitung");
        Cat("Wire", "wire|draht|aederleitung|flexible wire|hook-up wire");
        Cat("Switch", "switch|schalter|cam switch|disconnect|main switch|t0-|position switch|toggle");
        Cat("Button", "pushbutton|push button|taster|emergency stop|not-aus|notaus");
        Cat("Signal", "pilot light|indicator light|lamp|buzzer|signal|monitor light|led element|leuchtmelder");
        Cat("Meter", "energy meter|zähler|zaehler|multimeter|measuring|current transformer|counter|hour meter|wattmeter");
        Cat("Heating", "heater|heating|frost guard|enclosure heater|heizung");
        Cat("Fan", "fan|lüfter|luefter|ventilator|filter fan");
        Cat("Lighting", "luminaire|leuchte|light fitting|lamp holder|ballast");
        Cat("Label", "label|marking|beschriftung|marker|printjet|weidmüller tag|wire marker");
        Cat("Enclosure", "enclosure|gehäuse|gehaeuse|schaltschrank|cabinet|spacial|wall mount");
        Cat("Mounting", "mounting|din rail|busbar|hutschiene|support rail|rail |montage");

        return rules;
    }
}

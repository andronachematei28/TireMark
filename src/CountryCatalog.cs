using System.Globalization;

namespace TireMark;

internal sealed record CountryInfo(
    string Code, string Name, string LanguageCode, string Culture, string Currency,
    string RoadToll, string Inspection, string Insurance, string Region)
{
    public IReadOnlyList<VehicleCategory> Categories { get; init; } = [];
}

internal sealed record VehicleCategory(string Id, string Label, bool HasPurchaseDate = false, bool HasStartDate = false, bool HasCompany = false);

internal sealed record LanguageInfo(string Code, string Name, string Culture);

internal static class CountryCatalog
{
    private static readonly CountryInfo[] CuratedCountries =
    [
        new("GB", "United Kingdom", "en", "en-GB", "GBP", "Vehicle tax", "MOT", "Car insurance", "Europe"),
        new("AT", "Austria", "de", "de-AT", "EUR", "Vignette / GO-Maut", "Pickerl / §57a", "Kfz-Versicherung", "European Union"),
        new("BE", "Belgium", "nl", "nl-BE", "EUR", "Road tax", "Autokeuring / Contrôle technique", "Car insurance", "European Union"),
        new("BG", "Bulgaria", "bg", "bg-BG", "EUR", "Винетка", "ГТП", "Гражданска отговорност", "European Union"),
        new("HR", "Croatia", "hr", "hr-HR", "EUR", "Cestarina", "Tehnički pregled", "Obvezno osiguranje", "European Union"),
        new("CY", "Cyprus", "el", "el-CY", "EUR", "Road tax", "MOT", "Motor insurance", "European Union / Asia"),
        new("CZ", "Czechia", "cs", "cs-CZ", "CZK", "Dálniční známka", "STK", "Povinné ručení", "European Union"),
        new("DK", "Denmark", "da", "da-DK", "DKK", "Road tax", "Bilsyn", "Bilforsikring", "European Union"),
        new("EE", "Estonia", "et", "et-EE", "EUR", "Teemaks", "Tehnoülevaatus", "Liikluskindlustus", "European Union"),
        new("FI", "Finland", "fi", "fi-FI", "EUR", "Ajoneuvovero", "Katsastus", "Liikennevakuutus", "European Union"),
        new("FR", "France", "fr", "fr-FR", "EUR", "Péage", "Contrôle technique", "Assurance auto", "European Union"),
        new("DE", "Germany", "de", "de-DE", "EUR", "Kfz-Steuer", "TÜV / HU", "Kfz-Versicherung", "European Union"),
        new("GR", "Greece", "el", "el-GR", "EUR", "Τέλη κυκλοφορίας", "ΚΤΕΟ", "Ασφάλεια αυτοκινήτου", "European Union"),
        new("HU", "Hungary", "hu", "hu-HU", "HUF", "E-matrica", "Műszaki vizsga", "Kötelező biztosítás", "European Union"),
        new("IE", "Ireland", "en", "en-IE", "EUR", "Motor tax", "NCT", "Car insurance", "European Union"),
        new("IT", "Italy", "it", "it-IT", "EUR", "Pedaggio", "Revisione", "Assicurazione RCA", "European Union"),
        new("LV", "Latvia", "lv", "lv-LV", "EUR", "Autoceļu nodeva", "Tehniskā apskate", "OCTA", "European Union"),
        new("LT", "Lithuania", "lt", "lt-LT", "EUR", "Kelių mokestis", "Techninė apžiūra", "Privalomasis draudimas", "European Union"),
        new("LU", "Luxembourg", "fr", "fr-LU", "EUR", "Taxe sur les véhicules", "Contrôle technique", "Assurance auto", "European Union"),
        new("MT", "Malta", "mt", "mt-MT", "EUR", "Road licence", "VRT", "Motor insurance", "European Union"),
        new("NL", "Netherlands", "nl", "nl-NL", "EUR", "Motorrijtuigenbelasting", "APK", "Autoverzekering", "European Union"),
        new("PL", "Poland", "pl", "pl-PL", "PLN", "Opłaty drogowe", "Badanie techniczne", "OC", "European Union"),
        new("PT", "Portugal", "pt", "pt-PT", "EUR", "Portagem", "Inspeção periódica", "Seguro automóvel", "European Union"),
        new("RO", "Romania", "ro", "ro-RO", "RON", "Rovinietă", "ITP", "Asigurare", "European Union"),
        new("SK", "Slovakia", "sk", "sk-SK", "EUR", "Diaľničná známka", "STK", "PZP", "European Union"),
        new("SI", "Slovenia", "sl", "sl-SI", "EUR", "E-vinjeta", "Tehnični pregled", "Avtomobilsko zavarovanje", "European Union"),
        new("ES", "Spain", "es", "es-ES", "EUR", "Peaje", "ITV", "Seguro del coche", "European Union"),
        new("SE", "Sweden", "sv", "sv-SE", "SEK", "Fordonsskatt", "Kontrollbesiktning", "Bilförsäkring", "European Union"),
        new("AF", "Afghanistan", "ps", "ps-AF", "AFN", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("AM", "Armenia", "hy", "hy-AM", "AMD", "Road tax", "Տեխնիկական զննում", "ԱՊՊԱ", "Asia"),
        new("AZ", "Azerbaijan", "az", "az-AZ", "AZN", "Road tax", "Texniki baxış", "İcbari sığorta", "Asia"),
        new("BH", "Bahrain", "ar", "ar-BH", "BHD", "Road toll", "Vehicle inspection", "Motor insurance", "Asia"),
        new("BD", "Bangladesh", "bn", "bn-BD", "BDT", "Road tax", "Fitness certificate", "Motor insurance", "Asia"),
        new("BT", "Bhutan", "dz", "dz-BT", "BTN", "Road tax", "Vehicle fitness test", "Motor insurance", "Asia"),
        new("BN", "Brunei", "ms", "ms-BN", "BND", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("KH", "Cambodia", "km", "km-KH", "KHR", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("CN", "China", "zh", "zh-CN", "CNY", "Vehicle use tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("GE", "Georgia", "ka", "ka-GE", "GEL", "Road tax", "ტექნიკური ინსპექტირება", "ავტოდაზღვევა", "Asia"),
        new("IN", "India", "hi", "hi-IN", "INR", "Road tax", "PUC / Fitness certificate", "Motor insurance", "Asia"),
        new("ID", "Indonesia", "id", "id-ID", "IDR", "Pajak kendaraan", "Uji KIR / Uji berkala", "Asuransi kendaraan", "Asia"),
        new("IR", "Iran", "fa", "fa-IR", "IRR", "Road toll", "معاینه فنی", "بیمه شخص ثالث", "Asia"),
        new("IQ", "Iraq", "ar", "ar-IQ", "IQD", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("IL", "Israel", "he", "he-IL", "ILS", "Road toll", "מבחן רישוי (טסט)", "ביטוח רכב", "Asia"),
        new("JP", "Japan", "ja", "ja-JP", "JPY", "Expressway toll", "車検", "自動車保険", "Asia"),
        new("JO", "Jordan", "ar", "ar-JO", "JOD", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("KZ", "Kazakhstan", "kk", "kk-KZ", "KZT", "Road toll", "Техникалық байқау", "Міндетті автосақтандыру", "Asia"),
        new("KW", "Kuwait", "ar", "ar-KW", "KWD", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("KG", "Kyrgyzstan", "ky", "ky-KG", "KGS", "Road tax", "Техникалык кароо", "Милдеттүү камсыздандыруу", "Asia"),
        new("LA", "Laos", "lo", "lo-LA", "LAK", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("LB", "Lebanon", "ar", "ar-LB", "LBP", "Road tax", "الميكانيك", "تأمين المركبات", "Asia"),
        new("MY", "Malaysia", "ms", "ms-MY", "MYR", "Tol lebuh raya", "Pemeriksaan PUSPAKOM", "Insurans kenderaan", "Asia"),
        new("MV", "Maldives", "dv", "dv-MV", "MVR", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("MN", "Mongolia", "mn", "mn-MN", "MNT", "Road tax", "Техникийн үзлэг", "Автомашины даатгал", "Asia"),
        new("MM", "Myanmar", "my", "my-MM", "MMK", "Road tax", "ယာဉ်စစ်ဆေးခြင်း", "မော်တော်ယာဉ်အာမခံ", "Asia"),
        new("NP", "Nepal", "ne", "ne-NP", "NPR", "Road tax", "Vehicle fitness test", "Motor insurance", "Asia"),
        new("OM", "Oman", "ar", "ar-OM", "OMR", "Road toll", "الفحص الفني", "تأمين المركبات", "Asia"),
        new("PK", "Pakistan", "ur", "ur-PK", "PKR", "Motorway toll", "Vehicle fitness certificate", "موٹر انشورنس", "Asia"),
        new("PS", "Palestine", "ar", "ar-PS", "ILS", "Road tax", "الفحص الفني", "تأمين المركبات", "Asia"),
        new("PH", "Philippines", "tl", "en-PH", "PHP", "Road user charge", "LTO inspection", "Compulsory third-party liability", "Asia"),
        new("QA", "Qatar", "ar", "ar-QA", "QAR", "Road toll", "الفحص الفني", "تأمين المركبات", "Asia"),
        new("SA", "Saudi Arabia", "ar", "ar-SA", "SAR", "Road toll", "الفحص الدوري", "تأمين المركبات", "Asia"),
        new("SG", "Singapore", "en", "en-SG", "SGD", "ERP / Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("KR", "South Korea", "ko", "ko-KR", "KRW", "고속도로 통행료", "자동차 검사", "자동차 책임보험", "Asia"),
        new("LK", "Sri Lanka", "si", "si-LK", "LKR", "Road tax", "Vehicle emission test", "Motor insurance", "Asia"),
        new("SY", "Syria", "ar", "ar-SY", "SYP", "Road tax", "الفحص الفني", "تأمين المركبات", "Asia"),
        new("TJ", "Tajikistan", "tg", "tg-TJ", "TJS", "Road tax", "Муоинаи техникӣ", "Суғуртаи ҳатмӣ", "Asia"),
        new("TH", "Thailand", "th", "th-TH", "THB", "Road toll", "ตรอ. / ตรวจสภาพรถ", "ประกันภัยรถยนต์", "Asia"),
        new("TL", "Timor-Leste", "pt", "pt-TL", "USD", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("TR", "Türkiye", "tr", "tr-TR", "TRY", "HGS / OGS", "Araç muayenesi", "Trafik sigortası", "Asia"),
        new("TW", "Taiwan", "zh", "zh-TW", "TWD", "Road toll", "Vehicle inspection", "Motor insurance", "Asia"),
        new("TM", "Turkmenistan", "tk", "tk-TM", "TMT", "Road tax", "Tehniki gözegçilik", "Awtoulag ätiýaçlandyrmasy", "Asia"),
        new("AE", "United Arab Emirates", "ar", "ar-AE", "AED", "Salik / Road toll", "الفحص الفني", "تأمين المركبات", "Asia"),
        new("UZ", "Uzbekistan", "uz", "uz-UZ", "UZS", "Road tax", "Texnik ko'rik", "Majburiy avtosug'urta", "Asia"),
        new("VN", "Vietnam", "vi", "vi-VN", "VND", "Phí đường bộ", "Đăng kiểm", "Bảo hiểm xe cơ giới", "Asia"),
        new("YE", "Yemen", "ar", "ar-YE", "YER", "Road tax", "Vehicle inspection", "Motor insurance", "Asia"),
        new("RU", "Russia", "ru", "ru-RU", "RUB", "Платные дороги", "Техосмотр", "ОСАГО", "Europe / Asia")
    ];

    private static readonly Dictionary<string, string> PreferredCultures = new(StringComparer.Ordinal)
    {
        ["AD"] = "ca-AD", ["AL"] = "sq-AL", ["AO"] = "pt-AO", ["AU"] = "en-AU", ["BA"] = "bs-Latn-BA",
        ["BO"] = "es-BO", ["BR"] = "pt-BR", ["BY"] = "be-BY", ["CA"] = "en-CA", ["CH"] = "de-CH",
        ["CM"] = "fr-CM", ["CO"] = "es-CO", ["CR"] = "es-CR", ["DO"] = "es-DO", ["DZ"] = "ar-DZ",
        ["EG"] = "ar-EG", ["GL"] = "da-GL", ["GH"] = "en-GH", ["GT"] = "es-GT", ["IS"] = "is-IS",
        ["KE"] = "sw-KE", ["LY"] = "ar-LY", ["MA"] = "ar-MA", ["MG"] = "mg-MG", ["MX"] = "es-MX",
        ["NG"] = "en-NG", ["NI"] = "es-NI", ["NZ"] = "en-NZ", ["PE"] = "es-PE", ["SD"] = "ar-SD",
        ["SG"] = "en-SG", ["SN"] = "fr-SN", ["SS"] = "en-SS", ["TW"] = "zh-TW", ["TZ"] = "sw-TZ",
        ["UA"] = "uk-UA", ["US"] = "en-US", ["ZA"] = "en-ZA", ["ZW"] = "en-ZW", ["XK"] = "sq-XK",
        ["MD"] = "ro-MD", ["ME"] = "sr-Latn-ME", ["MK"] = "mk-MK", ["RS"] = "sr-Latn-RS", ["MC"] = "fr-MC",
        ["SM"] = "it-SM", ["VA"] = "it-VA", ["RU"] = "ru-RU"
    };

    private static readonly Dictionary<string, (string Road, string Inspection, string Insurance)> LocalLabels = new(StringComparer.Ordinal)
    {
        ["af"] = ("Padgeld", "Voertuiginspeksie", "Motorversekering"), ["ar"] = ("رسوم الطرق", "الفحص الفني للمركبات", "تأمين المركبات"),
        ["az"] = ("Yol haqqı", "Texniki baxış", "Avtomobil sığortası"), ["bg"] = ("Пътна такса", "Годишен технически преглед", "Гражданска отговорност"),
        ["bn"] = ("সড়ক শুল্ক", "যানবাহন পরিদর্শন", "গাড়ির বীমা"), ["bs"] = ("Putarina", "Tehnički pregled", "Osiguranje vozila"),
        ["ca"] = ("Peatge", "Inspecció tècnica", "Assegurança del vehicle"), ["cs"] = ("Mýtné", "Technická prohlídka", "Pojištění vozidla"),
        ["da"] = ("Vejafgift", "Bilsyn", "Bilforsikring"), ["de"] = ("Straßengebühr", "Fahrzeugprüfung", "Kfz-Versicherung"),
        ["dz"] = ("ལམ་གྱི་ཁྲལ།", "རླངས་འཁོར་ཞིབ་དཔྱད།", "རླངས་འཁོར་ཉེན་སྲུང་།"),
        ["el"] = ("Τέλη κυκλοφορίας", "Τεχνικός έλεγχος οχημάτων", "Ασφάλιση οχήματος"), ["en"] = ("Road use fee", "Vehicle inspection", "Motor insurance"),
        ["es"] = ("Peaje", "Inspección técnica", "Seguro del vehículo"), ["et"] = ("Teemaks", "Sõiduki tehnoülevaatus", "Sõidukikindlustus"),
        ["fa"] = ("عوارض جاده", "معاینه فنی خودرو", "بیمه خودرو"), ["fi"] = ("Tienkäyttömaksu", "Katsastus", "Ajoneuvovakuutus"),
        ["fr"] = ("Péage", "Contrôle technique", "Assurance automobile"), ["ga"] = ("Dola bóthair", "Tástáil NCT", "Árachas gluaisteáin"),
        ["he"] = ("אגרת דרך", "מבחן רישוי לרכב", "ביטוח רכב"), ["hi"] = ("सड़क शुल्क", "वाहन निरीक्षण", "मोटर बीमा"),
        ["hr"] = ("Cestarina", "Tehnički pregled", "Osiguranje vozila"), ["hu"] = ("Útdíj", "Műszaki vizsga", "Gépjármű-biztosítás"),
        ["hy"] = ("Ճանապարհային վճար", "Տրանսպորտային միջոցի զննում", "Ավտոմեքենայի ապահովագրություն"), ["id"] = ("Tarif jalan", "Uji kendaraan", "Asuransi kendaraan"),
        ["is"] = ("Veggjald", "Bifreiðaskoðun", "Ökutækjatrygging"), ["it"] = ("Pedaggio", "Revisione", "Assicurazione auto"),
        ["ja"] = ("道路料金", "自動車検査", "自動車保険"), ["ka"] = ("გზის გადასახადი", "ტექნიკური ინსპექტირება", "ავტომობილის დაზღვევა"),
        ["kk"] = ("Жол ақысы", "Көлік құралдарын техникалық байқау", "Автокөлікті сақтандыру"), ["km"] = ("កម្រៃផ្លូវ", "ការត្រួតពិនិត្យយានយន្ត", "ធានារ៉ាប់រងយានយន្ត"),
        ["ko"] = ("통행료", "자동차 검사", "자동차 책임보험"), ["ky"] = ("Жол акысы", "Унааны техникалык кароо", "Унаа камсыздандыруу"),
        ["lo"] = ("ຄ່າທາງ", "ການກວດສະພາບລົດ", "ປະກັນໄພລົດ"), ["lt"] = ("Kelių rinkliava", "Techninė apžiūra", "Transporto priemonės draudimas"),
        ["lv"] = ("Ceļu nodeva", "Tehniskā apskate", "Transportlīdzekļa apdrošināšana"), ["mk"] = ("Патарина", "Технички преглед", "Осигурување на возило"),
        ["ms"] = ("Tol jalan", "Pemeriksaan kenderaan", "Insurans kenderaan"), ["mt"] = ("Ħlas tat-triq", "Spezzjoni tal-vettura", "Assigurazzjoni tal-vettura"),
        ["my"] = ("လမ်းအသုံးပြုခ", "ယာဉ်စစ်ဆေးခြင်း", "မော်တော်ယာဉ်အာမခံ"), ["ne"] = ("सडक शुल्क", "सवारीसाधन जाँच", "मोटर बीमा"),
        ["nl"] = ("Wegentol", "Autokeuring", "Autoverzekering"), ["no"] = ("Veibruksavgift", "EU-kontroll", "Bilforsikring"),
        ["pl"] = ("Opłata drogowa", "Badanie techniczne", "Ubezpieczenie pojazdu"), ["ps"] = ("د سړک فیس", "د موټر تخنیکي معاینه", "د موټر بیمه"),
        ["pt"] = ("Portagem", "Inspeção periódica", "Seguro automóvel"), ["ro"] = ("Taxă de drum", "Inspecție tehnică", "Asigurare auto"),
        ["ru"] = ("Плата за проезд", "Технический осмотр", "Автострахование"), ["si"] = ("මාර්ග ගාස්තුව", "වාහන පරීක්ෂාව", "වාහන රක්ෂණය"),
        ["sk"] = ("Mýto", "Technická kontrola", "Poistenie vozidla"), ["sl"] = ("Cestnina", "Tehnični pregled", "Zavarovanje vozila"),
        ["sq"] = ("Tarifë rrugore", "Kontrolli teknik", "Sigurimi i automjetit"), ["sr"] = ("Путарина", "Технички преглед", "Осигурање возила"),
        ["sv"] = ("Vägavgift", "Kontrollbesiktning", "Fordonsförsäkring"), ["sw"] = ("Ada ya barabara", "Ukaguzi wa gari", "Bima ya gari"),
        ["ta"] = ("சாலை கட்டணம்", "வாகன ஆய்வு", "வாகன காப்பீடு"), ["th"] = ("ค่าผ่านทาง", "การตรวจสภาพรถ", "ประกันภัยรถยนต์"),
        ["tl"] = ("Bayad sa kalsada", "Inspeksiyon ng sasakyan", "Seguro ng sasakyan"), ["tr"] = ("Yol geçiş ücreti", "Araç muayenesi", "Araç sigortası"),
        ["uk"] = ("Плата за проїзд", "Технічний огляд", "Автострахування"), ["ur"] = ("سڑک کا محصول", "گاڑی کا معائنہ", "گاڑی کا بیمہ"),
        ["uz"] = ("Yo‘l haqi", "Texnik ko‘rik", "Avtomobil sug‘urtasi"), ["vi"] = ("Phí đường bộ", "Đăng kiểm xe", "Bảo hiểm xe cơ giới"),
        ["zh"] = ("道路通行费", "机动车检验", "机动车保险")
    };

    private static readonly HashSet<string> EnglishGenericLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Road tax", "Road toll", "Road user charge", "Motorway toll", "Vehicle use tax", "Road licence", "Vehicle tax", "Motor tax",
        "Vehicle inspection", "Fitness certificate", "Vehicle fitness test", "Vehicle fitness certificate", "Vehicle emission test", "LTO inspection",
        "Motor insurance", "Car insurance", "Compulsory third-party liability", "ERP / Road tax"
    };

    public static IReadOnlyList<CountryInfo> Countries { get; }

    static CountryCatalog() => Countries = BuildCountries();

    private static IReadOnlyList<CountryInfo> BuildCountries()
    {
        var countries = CuratedCountries.Where(country => country.Code != "KP")
            .Select(LocalizeGenericEnglishTerms).ToDictionary(country => country.Code, StringComparer.Ordinal);
        var regionalCultures = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture =>
            {
                try { return (Culture: culture, Region: new RegionInfo(culture.Name)); }
                catch (ArgumentException) { return (Culture: culture, Region: (RegionInfo?)null); }
            })
            .Where(item => item.Region is not null && item.Region.TwoLetterISORegionName.Length == 2)
            .GroupBy(item => item.Region!.TwoLetterISORegionName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        foreach (var (code, candidates) in regionalCultures)
        {
            if (code == "KP" || countries.ContainsKey(code)) continue;
            var selectedCultureName = PreferredCultures.GetValueOrDefault(code);
            var selected = selectedCultureName is null
                ? candidates.OrderByDescending(item => item.Culture.Name.StartsWith("en-", StringComparison.OrdinalIgnoreCase)).ThenBy(item => item.Culture.Name, StringComparer.Ordinal).First()
                : candidates.FirstOrDefault(item => string.Equals(item.Culture.Name, selectedCultureName, StringComparison.OrdinalIgnoreCase));
            if (selected.Region is null) selected = candidates[0];
            var culture = selected.Culture;
            var codeLanguage = culture.TwoLetterISOLanguageName;
            var local = LocalLabels.GetValueOrDefault(codeLanguage, LocalLabels["en"]);
            var region = selected.Region!;
            countries[code] = new CountryInfo(code, region.EnglishName, codeLanguage, culture.Name, region.ISOCurrencySymbol,
                local.Road, local.Inspection, local.Insurance, MapRegion(code));
        }

        return countries.Values.Select(WithCategories).OrderBy(country => country.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static CountryInfo WithCategories(CountryInfo country)
    {
        var categories = country.Code switch
        {
            "US" => new[] { Registration(country, "registration"), SafetyInspection(country), Emissions(country), Insurance(country) },
            "CA" => new[] { Registration(country, "registration"), SafetyInspection(country), Insurance(country) },
            "MX" => new[] { Registration(country, "vehicle_tax"), Emissions(country), Insurance(country) },
            "AU" => new[] { Registration(country, "registration"), SafetyInspection(country), Insurance(country) },
            "NZ" => new[] { Registration(country, "registration"), InspectionCategory(country, LocalCategory(country.LanguageCode, "warrant_fitness")), Insurance(country) },
            "IN" => new[] { Toll(country), InspectionCategory(country, country.Inspection), Insurance(country) },
            "JP" or "KR" or "CN" => new[] { Toll(country), InspectionCategory(country, country.Inspection), Emissions(country), Insurance(country) },
            "BR" => new[] { Registration(country, "vehicle_tax"), InspectionCategory(country, country.Inspection), Insurance(country) },
            _ => new[] { Toll(country), InspectionCategory(country, country.Inspection), Insurance(country) }
        };
        return country with { Categories = categories };
    }

    private static VehicleCategory Toll(CountryInfo country) => new("road_toll", country.RoadToll, HasPurchaseDate: true);
    private static VehicleCategory InspectionCategory(CountryInfo country, string label) => new("inspection", label, HasStartDate: true);
    private static VehicleCategory Insurance(CountryInfo country) => new("insurance", country.Insurance, HasStartDate: true, HasCompany: true);
    private static VehicleCategory Registration(CountryInfo country, string id) => new(id, LocalCategory(country.LanguageCode, id), HasPurchaseDate: true);
    private static VehicleCategory SafetyInspection(CountryInfo country) => new("safety_inspection", LocalCategory(country.LanguageCode, "safety_inspection"), HasStartDate: true);
    private static VehicleCategory Emissions(CountryInfo country) => new("emissions_test", LocalCategory(country.LanguageCode, "emissions_test"), HasStartDate: true);

    private static string LocalCategory(string languageCode, string id)
    {
        var english = id switch
        {
            "registration" => "Vehicle registration",
            "vehicle_tax" => "Vehicle tax",
            "safety_inspection" => "Safety inspection",
            "emissions_test" => "Emissions test",
            "warrant_fitness" => "Warrant of Fitness",
            _ => id
        };
        return CategoryTranslations.Value.TryGetValue(languageCode, out var labels) && labels.TryGetValue(id, out var translated) ? translated : english;
    }

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> CategoryTranslations = new(() => new(StringComparer.Ordinal)
    {
        ["ar"] = new() { ["registration"] = "تسجيل المركبة", ["vehicle_tax"] = "ضريبة المركبة", ["safety_inspection"] = "فحص سلامة المركبة", ["emissions_test"] = "فحص الانبعاثات" },
        ["de"] = new() { ["registration"] = "Fahrzeugzulassung", ["vehicle_tax"] = "Kfz-Steuer", ["safety_inspection"] = "Sicherheitsprüfung", ["emissions_test"] = "Abgasuntersuchung" },
        ["el"] = new() { ["registration"] = "Ταξινόμηση οχήματος", ["vehicle_tax"] = "Τέλη κυκλοφορίας", ["safety_inspection"] = "Έλεγχος ασφαλείας", ["emissions_test"] = "Έλεγχος εκπομπών" },
        ["en"] = new() { ["registration"] = "Vehicle registration", ["vehicle_tax"] = "Vehicle tax", ["safety_inspection"] = "Safety inspection", ["emissions_test"] = "Emissions test", ["warrant_fitness"] = "Warrant of Fitness" },
        ["es"] = new() { ["registration"] = "Matriculación del vehículo", ["vehicle_tax"] = "Impuesto del vehículo", ["safety_inspection"] = "Inspección de seguridad", ["emissions_test"] = "Prueba de emisiones" },
        ["fr"] = new() { ["registration"] = "Immatriculation du véhicule", ["vehicle_tax"] = "Taxe sur les véhicules", ["safety_inspection"] = "Contrôle de sécurité", ["emissions_test"] = "Contrôle des émissions" },
        ["it"] = new() { ["registration"] = "Immatricolazione del veicolo", ["vehicle_tax"] = "Tassa automobilistica", ["safety_inspection"] = "Controllo di sicurezza", ["emissions_test"] = "Controllo delle emissioni" },
        ["ja"] = new() { ["registration"] = "自動車登録", ["vehicle_tax"] = "自動車税", ["safety_inspection"] = "安全検査", ["emissions_test"] = "排出ガス検査" },
        ["ko"] = new() { ["registration"] = "자동차 등록", ["vehicle_tax"] = "자동차세", ["safety_inspection"] = "자동차 안전검사", ["emissions_test"] = "배출가스 검사" },
        ["nl"] = new() { ["registration"] = "Voertuigregistratie", ["vehicle_tax"] = "Motorrijtuigenbelasting", ["safety_inspection"] = "Veiligheidskeuring", ["emissions_test"] = "Emissietest" },
        ["pl"] = new() { ["registration"] = "Rejestracja pojazdu", ["vehicle_tax"] = "Podatek od pojazdu", ["safety_inspection"] = "Badanie bezpieczeństwa", ["emissions_test"] = "Badanie emisji" },
        ["pt"] = new() { ["registration"] = "Registo do veículo", ["vehicle_tax"] = "Imposto automóvel", ["safety_inspection"] = "Inspeção de segurança", ["emissions_test"] = "Teste de emissões" },
        ["ro"] = new() { ["registration"] = "Înmatriculare auto", ["vehicle_tax"] = "Taxă auto", ["safety_inspection"] = "Inspecție de siguranță", ["emissions_test"] = "Test de emisii" },
        ["ru"] = new() { ["registration"] = "Регистрация автомобиля", ["vehicle_tax"] = "Транспортный налог", ["safety_inspection"] = "Проверка безопасности", ["emissions_test"] = "Проверка выбросов" },
        ["tr"] = new() { ["registration"] = "Araç tescili", ["vehicle_tax"] = "Motorlu taşıt vergisi", ["safety_inspection"] = "Güvenlik muayenesi", ["emissions_test"] = "Emisyon testi" },
        ["zh"] = new() { ["registration"] = "车辆登记", ["vehicle_tax"] = "车辆税", ["safety_inspection"] = "安全检查", ["emissions_test"] = "排放检测" }
    });

    private static CountryInfo LocalizeGenericEnglishTerms(CountryInfo country)
    {
        if (country.LanguageCode == "en" || !LocalLabels.TryGetValue(country.LanguageCode, out var terms)) return country;
        return country with
        {
            RoadToll = EnglishGenericLabels.Contains(country.RoadToll) ? terms.Road : country.RoadToll,
            Inspection = EnglishGenericLabels.Contains(country.Inspection) ? terms.Inspection : country.Inspection,
            Insurance = EnglishGenericLabels.Contains(country.Insurance) ? terms.Insurance : country.Insurance
        };
    }

    private static string MapRegion(string code) => code switch
    {
        "AQ" => "Antarctica",
        "AU" or "FJ" or "FM" or "KI" or "MH" or "NC" or "NF" or "NR" or "NU" or "NZ" or "PF" or "PG" or "PN" or "PW" or "SB" or "TO" or "TV" or "VU" or "WF" or "WS" => "Oceania",
        "AF" or "AM" or "AZ" or "BD" or "BH" or "BN" or "BT" or "CN" or "GE" or "HK" or "ID" or "IN" or "IO" or "IR" or "IQ" or "IL" or "JO" or "JP" or "KG" or "KH" or "KP" or "KR" or "KW" or "KZ" or "LA" or "LB" or "LK" or "MM" or "MN" or "MV" or "MY" or "NP" or "OM" or "PH" or "PK" or "PS" or "QA" or "SA" or "SG" or "SY" or "TH" or "TJ" or "TL" or "TM" or "TW" or "AE" or "UZ" or "VN" or "YE" => "Asia",
        "DZ" or "AO" or "BJ" or "BW" or "BF" or "BI" or "CV" or "CM" or "CF" or "TD" or "KM" or "CD" or "CG" or "CI" or "DJ" or "EG" or "GQ" or "ER" or "SZ" or "ET" or "GA" or "GM" or "GH" or "GN" or "GW" or "KE" or "LS" or "LR" or "LY" or "MG" or "MW" or "ML" or "MR" or "MU" or "YT" or "MA" or "MZ" or "NA" or "NE" or "NG" or "RE" or "RW" or "SH" or "SN" or "SC" or "SL" or "SO" or "ZA" or "SS" or "SD" or "ST" or "TZ" or "TG" or "TN" or "UG" or "EH" or "ZM" or "ZW" => "Africa",
        "AR" or "AG" or "AW" or "BS" or "BB" or "BZ" or "BM" or "BO" or "BR" or "CA" or "KY" or "CL" or "CO" or "CR" or "CU" or "CW" or "DM" or "DO" or "EC" or "SV" or "FK" or "GF" or "GD" or "GP" or "GT" or "GY" or "HT" or "HN" or "JM" or "MQ" or "MX" or "NI" or "PA" or "PY" or "PE" or "PR" or "BL" or "KN" or "LC" or "MF" or "PM" or "VC" or "SX" or "SR" or "TT" or "TC" or "US" or "UY" or "VE" or "VG" or "VI" => "Americas",
        _ => "Europe"
    };

    public static CountryInfo? Find(string code) => Countries.FirstOrDefault(x => x.Code == code);
    public static VehicleCategory? Category(string code, string categoryId) => Find(code)?.Categories.FirstOrDefault(category => category.Id == categoryId);

    public static IReadOnlyList<LanguageInfo> Languages { get; } =
    [
        new("en", "English", "en-GB"), new("ar", "Arabic", "ar-SA"), new("bg", "Bulgarian", "bg-BG"), new("hr", "Croatian", "hr-HR"),
        new("fr", "French", "fr-FR"), new("de", "German", "de-DE"), new("el", "Greek", "el-GR"), new("id", "Indonesian", "id-ID"),
        new("it", "Italian", "it-IT"), new("pl", "Polish", "pl-PL"), new("pt", "Portuguese", "pt-PT"), new("ro", "Romanian", "ro-RO"),
        new("ru", "Russian", "ru-RU"), new("sl", "Slovenian", "sl-SI"), new("es", "Spanish", "es-ES"), new("th", "Thai", "th-TH"),
        new("tr", "Turkish", "tr-TR"), new("uk", "Ukrainian", "uk-UA"), new("vi", "Vietnamese", "vi-VN")
    ];
}

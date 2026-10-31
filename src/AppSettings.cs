using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TireMark;

internal sealed class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TireMark", "settings.json");
    public static AppSettings Current { get; private set; } = new();
    public string Country { get; set; } = "RO";
    public List<string> CountryProfiles { get; set; } = [];
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "light";
    public string Accent { get; set; } = "#2864D7";
    public string BackgroundTint { get; set; } = "#2864D7";
    public double TextScale { get; set; } = 1.0;

    public static void Load()
    {
        try { if (File.Exists(SettingsPath)) Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new(); }
        catch { Current = new(); }
        if (!CountryCatalog.Countries.Any(country => country.Code == Current.Country)) Current.Country = "RO";
        Current.CountryProfiles ??= [];
        Current.CountryProfiles = Current.CountryProfiles.Where(code => CountryCatalog.Find(code) is not null).Distinct(StringComparer.Ordinal).ToList();
        if (!Current.CountryProfiles.Contains(Current.Country, StringComparer.Ordinal)) Current.CountryProfiles.Add(Current.Country);
        if (!Languages.Any(language => language.Code == Current.Language)) Current.Language = "en";
        if (Current.Theme is not ("light" or "dark")) Current.Theme = "light";
        if (!IsHexColor(Current.Accent)) Current.Accent = "#2864D7";
        if (!IsHexColor(Current.BackgroundTint)) Current.BackgroundTint = "#2864D7";
        Current.TextScale = Math.Clamp(Current.TextScale, .85, 1.4);
    }

    private static bool IsHexColor(string? value) => value is not null && Regex.IsMatch(value, "^#[0-9A-Fa-f]{6}$");

    public static void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static IReadOnlyList<LanguageInfo> Languages => CountryCatalog.Languages;
    public static IReadOnlyList<CountryInfo> Countries => CountryCatalog.Countries;

    public static CultureInfo CountryCulture
    {
        get
        {
            var name = CountryCatalog.Find(Current.Country)?.Culture ?? "en-GB";
            try { return CultureInfo.GetCultureInfo(name); }
            catch (CultureNotFoundException) { return CultureInfo.GetCultureInfo("en-GB"); }
        }
    }

    public static string CountryDatePattern => CountryCulture.DateTimeFormat.ShortDatePattern;

    public static string CountryName(string code)
    {
        return CountryCatalog.Find(code)?.Name ?? code;
    }

    public static string CurrencyCode => CountryCatalog.Find(Current.Country)?.Currency ?? "EUR";
    public static bool IsRightToLeft => Current.Language is "ar" or "dv" or "fa" or "he" or "ps" or "ur";

    public static CultureInfo Culture
    {
        get
        {
            var name = Languages.FirstOrDefault(x => x.Code == Current.Language)?.Culture ?? "en-GB";
            try { return CultureInfo.GetCultureInfo(name); }
            catch (CultureNotFoundException) { return CultureInfo.GetCultureInfo("en-GB"); }
        }
    }

    public static void ApplyCulture()
    {
        var culture = Culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static string DocumentLabel(string type)
    {
        var country = Current.Country;
        var profile = CountryCatalog.Find(country);
        return profile?.Categories.FirstOrDefault(category => category.Id == type)?.Label
            ?? (type switch { "road_toll" => profile?.RoadToll, "inspection" => profile?.Inspection, _ => profile?.Insurance })
            ?? Localization.T("Insurance");
    }
}

internal static class Localization
{
    private static readonly HashSet<string> CuratedLanguages = new(StringComparer.Ordinal) { "en", "de", "it", "fr", "es", "el", "tr", "ru", "ro" };
    private static readonly Dictionary<string, Dictionary<string, string>> GeneratedRows = LoadGeneratedRows();

    private static Dictionary<string, Dictionary<string, string>> LoadGeneratedRows()
    {
        try
        {
            using var stream = typeof(Localization).Assembly.GetManifestResourceStream("TireMark.Localization.generated.json");
            return stream is null
                ? new(StringComparer.Ordinal)
                : JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(stream) ?? new(StringComparer.Ordinal);
        }
        catch { return new(StringComparer.Ordinal); }
    }

    private static readonly Dictionary<string, string[]> Rows = new(StringComparer.Ordinal)
    {
        ["Garage"] = ["Garage", "Garage", "Garage", "Garage", "Garaje", "Γκαράζ", "Garaj", "Гараж", "Garajul tău"],
        ["←   Înapoi la garaj"] = ["←   Back to garage", "←   Zurück zur Garage", "←   Torna al garage", "←   Retour au garage", "←   Volver al garaje", "←   Πίσω στο γκαράζ", "←   Garaja dön", "←   Назад в гараж", "←   Înapoi la garaj"],
        ["Editează mașina"] = ["Edit car", "Auto bearbeiten", "Modifica auto", "Modifier la voiture", "Editar coche", "Επεξεργασία αυτοκινήτου", "Aracı düzenle", "Изменить автомобиль", "Editează mașina"],
        ["Add car"] = ["Add car", "Auto hinzufügen", "Aggiungi auto", "Ajouter une voiture", "Añadir coche", "Προσθήκη αυτοκινήτου", "Araç ekle", "Добавить автомобиль", "Adaugă mașină"],
        ["Car"] = ["Car", "Auto", "Auto", "Voiture", "Coche", "Αυτοκίνητο", "Araç", "Автомобиль", "Mașină"],
        ["Back to garage"] = ["←   Back to garage", "←   Zurück zur Garage", "←   Torna al garage", "←   Retour au garage", "←   Volver al garaje", "←   Πίσω στο γκαράζ", "←   Garaja dön", "←   Назад в гараж", "←   Înapoi la garaj"],
        ["Edit car"] = ["Edit car", "Auto bearbeiten", "Modifica auto", "Modifier la voiture", "Editar coche", "Επεξεργασία αυτοκινήτου", "Aracı düzenle", "Изменить автомобиль", "Editează mașina"],
        ["Open car"] = ["Open car  →", "Auto öffnen  →", "Apri auto  →", "Ouvrir la voiture  →", "Abrir coche  →", "Άνοιγμα αυτοκινήτου  →", "Aracı aç  →", "Открыть автомобиль  →", "Deschide mașina  →"],
        ["Settings"] = ["Settings", "Einstellungen", "Impostazioni", "Paramètres", "Ajustes", "Ρυθμίσεις", "Ayarlar", "Настройки", "Setări"],
        ["Region"] = ["Country or region", "Land oder Region", "Paese o regione", "Pays ou région", "País o región", "Χώρα ή περιοχή", "Ülke veya bölge", "Страна или регион", "Țară sau regiune"],
        ["Language"] = ["Language", "Sprache", "Lingua", "Langue", "Idioma", "Γλώσσα", "Dil", "Язык", "Limbă"],
        ["Appearance"] = ["Appearance", "Darstellung", "Aspetto", "Apparence", "Apariencia", "Εμφάνιση", "Görünüm", "Оформление", "Aspect"],
        ["Light"] = ["Light", "Hell", "Chiaro", "Clair", "Claro", "Φωτεινό", "Açık", "Светлая", "Deschisă"],
        ["Dark"] = ["Dark", "Dunkel", "Scuro", "Sombre", "Oscuro", "Σκούρο", "Koyu", "Тёмная", "Întunecată"],
        ["Accent color"] = ["Button color", "Akzentfarbe", "Colore pulsanti", "Couleur des boutons", "Color de botones", "Χρώμα κουμπιών", "Düğme rengi", "Цвет кнопок", "Culoarea butoanelor"],
        ["Background color"] = ["Background color", "Hintergrundfarbe", "Colore di sfondo", "Couleur d’arrière-plan", "Color de fondo", "Χρώμα φόντου", "Arka plan rengi", "Цвет фона", "Culoarea fundalului"],
        ["Choose custom color"] = ["Choose custom color", "Eigene Farbe wählen", "Scegli un colore", "Choisir une couleur", "Elegir color", "Επιλογή χρώματος", "Özel renk seç", "Выбрать цвет", "Alege o culoare"],
        ["Hue"] = ["Hue", "Farbton", "Tonalità", "Teinte", "Tono", "Απόχρωση", "Ton", "Тон", "Nuanță"],
        ["Saturation"] = ["Saturation", "Sättigung", "Saturazione", "Saturation", "Saturación", "Κορεσμός", "Doygunluk", "Насыщенность", "Saturație"],
        ["Brightness"] = ["Brightness", "Helligkeit", "Luminosità", "Luminosité", "Brillo", "Φωτεινότητα", "Parlaklık", "Яркость", "Luminozitate"],
        ["Done"] = ["Done", "Fertig", "Fatto", "Terminé", "Listo", "Τέλος", "Bitti", "Готово", "Gata"],
        ["Text size"] = ["Text size", "Textgröße", "Dimensione testo", "Taille du texte", "Tamaño del texto", "Μέγεθος κειμένου", "Metin boyutu", "Размер текста", "Scalare text"],
        ["Continue"] = ["Continue", "Weiter", "Continua", "Continuer", "Continuar", "Συνέχεια", "Devam et", "Продолжить", "Continuă"],
        ["Choose settings"] = ["Choose your region and language", "Region und Sprache auswählen", "Scegli paese e lingua", "Choisissez le pays et la langue", "Elige el país y el idioma", "Επιλέξτε χώρα και γλώσσα", "Ülke ve dili seçin", "Выберите страну и язык", "Alege țara și limba"],
        ["Car count one"] = ["(1 car)", "(1 Auto)", "(1 auto)", "(1 voiture)", "(1 coche)", "(1 αυτοκίνητο)", "(1 araç)", "(1 автомобиль)", "(1 mașină)"],
        ["Car count many"] = ["({0} cars)", "({0} Autos)", "({0} auto)", "({0} voitures)", "({0} coches)", "({0} αυτοκίνητα)", "({0} araç)", "({0} автомобиля)", "({0} mașini)"],
        ["Add first record"] = ["Add the first record for this category.", "Fügen Sie den ersten Eintrag für diese Kategorie hinzu.", "Aggiungi il primo elemento per questa categoria.", "Ajoutez le premier document de cette catégorie.", "Añade el primer registro de esta categoría.", "Προσθέστε την πρώτη καταχώριση σε αυτή την κατηγορία.", "Bu kategoriye ilk kaydı ekleyin.", "Добавьте первую запись в этой категории.", "Adaugă prima intrare pentru această categorie."],
        ["History empty"] = ["No history yet.", "Noch kein Verlauf.", "Nessuno storico.", "Aucun historique.", "Aún no hay historial.", "Δεν υπάρχει ιστορικό.", "Henüz geçmiş yok.", "История пуста.", "Istoricul este gol."],
        ["Search countries"] = ["Search countries", "Länder suchen", "Cerca paesi", "Rechercher des pays", "Buscar países", "Αναζήτηση χωρών", "Ülke ara", "Поиск стран", "Caută țări"],
        ["Search languages"] = ["Search languages", "Sprachen suchen", "Cerca lingue", "Rechercher des langues", "Buscar idiomas", "Αναζήτηση γλωσσών", "Dil ara", "Поиск языков", "Caută limbi"],
        ["Select country"] = ["Select a country", "Land auswählen", "Seleziona un paese", "Sélectionner un pays", "Selecciona un país", "Επιλέξτε χώρα", "Ülke seçin", "Выберите страну", "Selectați o țară"],
        ["Select language"] = ["Select a language", "Sprache auswählen", "Seleziona una lingua", "Sélectionner une langue", "Selecciona un idioma", "Επιλέξτε γλώσσα", "Dil seçin", "Выберите язык", "Selectați o limbă"],
        ["Country change"] = ["Country selection", "Länderauswahl", "Scelta del paese", "Choix du pays", "Selección de país", "Επιλογή χώρας", "Ülke seçimi", "Выбор страны", "Selectarea țării"],
        ["Add country"] = ["Add country", "Land hinzufügen", "Aggiungi paese", "Ajouter le pays", "Añadir país", "Προσθήκη χώρας", "Ülke ekle", "Добавить страну", "Adaugă țara"],
        ["Use existing country"] = ["Switch to existing country", "Zum vorhandenen Land wechseln", "Passa al paese esistente", "Passer au pays existant", "Cambiar al país existente", "Μετάβαση στην υπάρχουσα χώρα", "Mevcut ülkeye geç", "Перейти к существующей стране", "Treci la țara existentă"],
        ["Delete country"] = ["Delete country", "Land löschen", "Elimina paese", "Supprimer le pays", "Eliminar país", "Διαγραφή χώρας", "Ülkeyi sil", "Удалить страну", "Șterge țara"],
        ["Delete country confirmation"] = ["Delete {0} and all its cars and records?", "{0} und alle Fahrzeuge und Einträge löschen?", "Eliminare {0} con tutte le auto e i documenti?", "Supprimer {0} et tous ses véhicules et documents ?", "¿Eliminar {0} y todos sus vehículos y registros?", "Να διαγραφεί το {0} μαζί με όλα τα οχήματα και τις εγγραφές του;", "{0} ve tüm araçları ile kayıtları silinsin mi?", "Удалить страну «{0}», все автомобили и записи?", "Ștergi țara {0} și toate mașinile și documentele ei?"],
        ["Keep one country"] = ["Keep one country", "Ein Land behalten", "Mantieni un paese", "Conserver un pays", "Conserva un país", "Διατηρήστε μία χώρα", "Bir ülke bırakın", "Оставьте одну страну", "Păstrează o țară"],
        ["Keep country message"] = ["At least one country must remain in your garage.", "Mindestens ein Land muss in der Garage bleiben.", "Deve rimanere almeno un paese nel garage.", "Au moins un pays doit rester dans le garage.", "Debe quedar al menos un país en el garaje.", "Πρέπει να παραμείνει τουλάχιστον μία χώρα στο γκαράζ.", "Garajda en az bir ülke kalmalıdır.", "В гараже должна остаться хотя бы одна страна.", "Trebuie să rămână cel puțin o țară în garaj."],
        ["Countries"] = ["Countries", "Länder", "Paesi", "Pays", "Países", "Χώρες", "Ülkeler", "Страны", "Țări"],
        ["Current country"] = ["Current", "Aktuell", "Attuale", "Actuel", "Actual", "Τρέχουσα", "Geçerli", "Текущая", "Curentă"],
        ["Delete"] = ["Delete", "Löschen", "Elimina", "Supprimer", "Eliminar", "Διαγραφή", "Sil", "Удалить", "Șterge"],
        ["OK"] = ["OK", "OK", "OK", "OK", "Aceptar", "Εντάξει", "Tamam", "ОК", "OK"],
        ["Expired"] = ["Expired", "Abgelaufen", "Scaduto", "Expiré", "Caducado", "Έληξε", "Süresi doldu", "Истёк", "Expirat"],
        ["Valid"] = ["Valid", "Gültig", "Valido", "Valide", "Válido", "Σε ισχύ", "Geçerli", "Действителен", "Valabil"],
        ["No data"] = ["No data", "Keine Daten", "Nessun dato", "Aucune donnée", "Sin datos", "Χωρίς στοιχεία", "Veri yok", "Нет данных", "Fără date"],
        ["Future"] = ["Upcoming", "Ausstehend", "In arrivo", "À venir", "Próximo", "Προσεχώς", "Yaklaşan", "Предстоит", "Viitor"],
        ["Expires today"] = ["Expires today", "Läuft heute ab", "Scade oggi", "Expire aujourd'hui", "Caduca hoy", "Λήγει σήμερα", "Bugün sona eriyor", "Истекает сегодня", "Expiră azi"],
        ["Expires in {0}"] = ["Expires in {0}", "Läuft in {0} ab", "Scade tra {0}", "Expire dans {0}", "Caduca en {0}", "Λήγει σε {0}", "{0} içinde sona eriyor", "Истекает через {0}", "Expiră în {0}"],
        ["Starts in {0}"] = ["Starts in {0}", "Gültig in {0}", "Entra in vigore tra {0}", "Entre en vigueur dans {0}", "Entra en vigor en {0}", "Ισχύει σε {0}", "{0} sonra yürürlüğe giriyor", "Вступит в силу через {0}", "Intră în vigoare în {0}"],
        ["Expired: {0} ago · {1}"] = ["Expired: {0} ago · {1}", "Abgelaufen vor {0} · {1}", "Scaduto da {0} · {1}", "Expiré depuis {0} · {1}", "Caducado hace {0} · {1}", "Έληξε πριν από {0} · {1}", "{0} önce sona erdi · {1}", "Истёк {0} назад · {1}", "A expirat acum {0} · {1}"],
        ["Starts on {0} · Expires on {1}"] = ["Starts on {0} · Expires on {1}", "Beginn: {0} · Ablauf: {1}", "Inizia il {0} · Scade il {1}", "Début : {0} · Expiration : {1}", "Empieza el {0} · Caduca el {1}", "Έναρξη: {0} · Λήξη: {1}", "Başlangıç: {0} · Bitiş: {1}", "Начало: {0} · Истекает: {1}", "Începe la {0} · Expiră la {1}"],
        ["Expires on {0}"] = ["Expires on {0}", "Läuft ab am {0}", "Scade il {0}", "Expire le {0}", "Caduca el {0}", "Λήγει στις {0}", "{0} tarihinde sona eriyor", "Истекает {0}", "Expiră la {0}"],
        ["Purchased on {0}"] = ["Purchased on {0}", "Gekauft am {0}", "Acquistata il {0}", "Acheté le {0}", "Comprado el {0}", "Αγοράστηκε στις {0}", "{0} tarihinde satın alındı", "Куплено {0}", "Cumpărată la {0}"],
        ["Effective from {0}"] = ["Effective from {0}", "Gültig ab {0}", "Decorrenza {0}", "En vigueur le {0}", "En vigor desde {0}", "Ισχύει από {0}", "{0} tarihinden itibaren geçerli", "Действует с {0}", "Intră în vigoare la {0}"],
        ["Price: {0}"] = ["Price: {0}", "Preis: {0}", "Prezzo: {0}", "Prix : {0}", "Precio: {0}", "Τιμή: {0}", "Fiyat: {0}", "Цена: {0}", "Preț: {0}"],
        ["Purchase date"] = ["Purchase date", "Kaufdatum", "Data di acquisto", "Date d'achat", "Fecha de compra", "Ημερομηνία αγοράς", "Satın alma tarihi", "Дата покупки", "Data cumpărării"],
        ["Effective date"] = ["Effective from", "Gültig ab", "Decorrenza", "En vigueur le", "En vigor desde", "Ισχύει από", "Başlangıç tarihi", "Действует с", "Intră în vigoare"],
        ["Expiry date"] = ["Expiry date", "Ablaufdatum", "Data di scadenza", "Date d'expiration", "Fecha de caducidad", "Ημερομηνία λήξης", "Son kullanma tarihi", "Дата окончания", "Data expirării"],
        ["Price"] = ["Price", "Preis", "Prezzo", "Prix", "Precio", "Τιμή", "Fiyat", "Цена", "Preț"],
        ["Insurer"] = ["Insurer", "Versicherer", "Compagnia assicurativa", "Assureur", "Aseguradora", "Ασφαλιστική εταιρεία", "Sigorta şirketi", "Страховщик", "Asigurator"],
        ["Plate"] = ["LICENSE PLATE", "KENNZEICHEN", "TARGA", "IMMATRICULATION", "MATRÍCULA", "ΠΙΝΑΚΙΔΑ", "PLAKA", "ГОСНОМЕР", "NUMĂR DE ÎNMATRICULARE"],
        ["VIN"] = ["VIN", "FIN", "VIN", "VIN", "VIN", "VIN", "Şasi no", "VIN", "VIN"],
        ["Not entered"] = ["Not entered", "Nicht angegeben", "Non inserito", "Non renseigné", "No indicado", "Δεν έχει συμπληρωθεί", "Belirtilmedi", "Не указано", "Necompletat"],
        ["Car name"] = ["Car name", "Fahrzeugname", "Nome auto", "Nom du véhicule", "Nombre del coche", "Όνομα αυτοκινήτου", "Araç adı", "Название автомобиля", "Denumire"],
        ["Registration number"] = ["Registration number", "Kennzeichen", "Targa", "Immatriculation", "Matrícula", "Αριθμός πινακίδας", "Plaka", "Регистрационный номер", "Număr de înmatriculare"],
        ["Insurance company"] = ["Insurance company", "Versicherungsgesellschaft", "Compagnia assicurativa", "Compagnie d'assurance", "Aseguradora", "Ασφαλιστική εταιρεία", "Sigorta şirketi", "Страховая компания", "Firma de asigurare"],
        ["Save"] = ["Save", "Speichern", "Salva", "Enregistrer", "Guardar", "Αποθήκευση", "Kaydet", "Сохранить", "Salvează"],
        ["Cancel"] = ["Cancel", "Abbrechen", "Annulla", "Annuler", "Cancelar", "Ακύρωση", "İptal", "Отмена", "Anulează"],
        ["Delete car"] = ["Delete car", "Auto löschen", "Elimina auto", "Supprimer la voiture", "Eliminar coche", "Διαγραφή αυτοκινήτου", "Aracı sil", "Удалить автомобиль", "Șterge mașina"],
        ["Delete record"] = ["Delete record", "Eintrag löschen", "Elimina voce", "Supprimer l'entrée", "Eliminar registro", "Διαγραφή καταχώρισης", "Kaydı sil", "Удалить запись", "Șterge intrarea"],
        ["Add entry"] = ["Add entry", "Eintrag hinzufügen", "Aggiungi voce", "Ajouter une entrée", "Añadir registro", "Προσθήκη καταχώρισης", "Kayıt ekle", "Добавить запись", "Adaugă intrarea"],
        ["Edit"] = ["Edit", "Bearbeiten", "Modifica", "Modifier", "Editar", "Επεξεργασία", "Düzenle", "Изменить", "Editează"],
        ["Add"] = ["Add", "Hinzufügen", "Aggiungi", "Ajouter", "Añadir", "Προσθήκη", "Ekle", "Добавить", "Adaugă"],
        ["Insurance"] = ["Insurance", "Versicherung", "Assicurazione", "Assurance", "Seguro", "Ασφάλεια", "Sigorta", "Страхование", "Asigurare"],
        ["FOTOGRAFIE MAȘINĂ"] = ["CAR PHOTO", "FAHRZEUGFOTO", "FOTO AUTO", "PHOTO DU VÉHICULE", "FOTO DEL COCHE", "ΦΩΤΟΓΡΑΦΙΑ ΑΥΤΟΚΙΝΗΤΟΥ", "ARAÇ FOTOĞRAFI", "ФОТО АВТОМОБИЛЯ", "FOTOGRAFIE MAȘINĂ"],
        ["Photo"] = ["Photo", "Foto", "Foto", "Photo", "Foto", "Φωτογραφία", "Fotoğraf", "Фото", "Fotografie"],
        ["Car photo"] = ["Car photo", "Fahrzeugfoto", "Foto auto", "Photo du véhicule", "Foto del coche", "Φωτογραφία αυτοκινήτου", "Araç fotoğrafı", "Фото автомобиля", "Fotografia mașinii"],
        ["Choose photo"] = ["Choose photo", "Foto auswählen", "Scegli foto", "Choisir une photo", "Elegir foto", "Επιλογή φωτογραφίας", "Fotoğraf seç", "Выбрать фото", "Alege fotografia"],
        ["Remove photo"] = ["Remove photo", "Foto entfernen", "Rimuovi foto", "Supprimer la photo", "Quitar foto", "Αφαίρεση φωτογραφίας", "Fotoğrafı kaldır", "Удалить фото", "Elimină fotografia"],
        ["Crop photo"] = ["Crop photo", "Foto zuschneiden", "Ritaglia foto", "Recadrer la photo", "Recortar foto", "Περικοπή φωτογραφίας", "Fotoğrafı kırp", "Обрезать фото", "Decupează fotografia"],
        ["Use crop"] = ["Use crop", "Zuschnitt verwenden", "Usa ritaglio", "Utiliser le recadrage", "Usar recorte", "Χρήση περικοπής", "Kırpmayı kullan", "Использовать обрезку", "Folosește decupajul"],
        ["Crop size"] = ["Crop size", "Zuschnittgröße", "Dimensione ritaglio", "Taille du recadrage", "Tamaño del recorte", "Μέγεθος περικοπής", "Kırpma boyutu", "Размер обрезки", "Mărimea decupajului"],
        ["Latest"] = ["LATEST", "NEUESTER", "PIÙ RECENTE", "LE PLUS RÉCENT", "MÁS RECIENTE", "ΝΕΟΤΕΡΟ", "EN YENİ", "ПОСЛЕДНЯЯ", "CEA MAI NOUĂ"],
        ["History"] = ["HISTORY", "VERLAUF", "STORICO", "HISTORIQUE", "HISTORIAL", "ΙΣΤΟΡΙΚΟ", "GEÇMİŞ", "ИСТОРИЯ", "ISTORIC"],
        ["Today"] = ["Today", "Heute", "Oggi", "Aujourd'hui", "Hoy", "Σήμερα", "Bugün", "Сегодня", "Azi"],
        ["Images"] = ["Images", "Bilder", "Immagini", "Images", "Imágenes", "Εικόνες", "Görseller", "Изображения", "Imagini"],
        ["Garajul tău"] = ["Your garage", "Deine Garage", "Il tuo garage", "Votre garage", "Tu garaje", "Το γκαράζ σας", "Garajınız", "Ваш гараж", "Garajul tău"],
        ["Țară sau regiune"] = ["Country or region", "Land oder Region", "Paese o regione", "Pays ou région", "País o región", "Χώρα ή περιοχή", "Ülke veya bölge", "Страна или регион", "Țară sau regiune"],
        ["Adaugă mașină"] = ["Add car", "Auto hinzufügen", "Aggiungi auto", "Ajouter une voiture", "Añadir coche", "Προσθήκη αυτοκινήτου", "Araç ekle", "Добавить автомобиль", "Adaugă mașină"],
        ["Deschide mașina  →"] = ["Open car  →", "Auto öffnen  →", "Apri auto  →", "Ouvrir la voiture  →", "Abrir coche  →", "Άνοιγμα αυτοκινήτου  →", "Aracı aç  →", "Открыть автомобиль  →", "Deschide mașina  →"],
        ["Mașină fără denumire"] = ["Unnamed car", "Auto ohne Namen", "Auto senza nome", "Voiture sans nom", "Coche sin nombre", "Αυτοκίνητο χωρίς όνομα", "İsimsiz araç", "Автомобиль без названия", "Mașină fără denumire"],
        ["FĂRĂ NUMĂR"] = ["NO PLATE", "OHNE KENNZEICHEN", "SENZA TARGA", "SANS PLAQUE", "SIN MATRÍCULA", "ΧΩΡΙΣ ΠΙΝΑΚΙΔΑ", "PLAKA YOK", "БЕЗ НОМЕРА", "FĂRĂ NUMĂR"],
        ["Necompletat"] = ["Not entered", "Nicht angegeben", "Non inserito", "Non renseigné", "No indicado", "Δεν έχει συμπληρωθεί", "Belirtilmedi", "Не указано", "Necompletat"],
        ["Fotografia mașinii"] = ["Car photo", "Fahrzeugfoto", "Foto auto", "Photo du véhicule", "Foto del coche", "Φωτογραφία αυτοκινήτου", "Araç fotoğrafı", "Фото автомобиля", "Fotografia mașinii"],
        ["Alege fotografia"] = ["Choose photo", "Foto auswählen", "Scegli foto", "Choisir une photo", "Elegir foto", "Επιλογή φωτογραφίας", "Fotoğraf seç", "Выбрать фото", "Alege fotografia"],
        ["Elimină fotografia"] = ["Remove photo", "Foto entfernen", "Rimuovi foto", "Supprimer la photo", "Quitar foto", "Αφαίρεση φωτογραφίας", "Fotoğrafı kaldır", "Удалить фото", "Elimină fotografia"],
        ["Anulează"] = ["Cancel", "Abbrechen", "Annulla", "Annuler", "Cancelar", "Ακύρωση", "İptal", "Отмена", "Anulează"],
        ["Salvează"] = ["Save", "Speichern", "Salva", "Enregistrer", "Guardar", "Αποθήκευση", "Kaydet", "Сохранить", "Salvează"],
        ["Șterge mașina"] = ["Delete car", "Auto löschen", "Elimina auto", "Supprimer la voiture", "Eliminar coche", "Διαγραφή αυτοκινήτου", "Aracı sil", "Удалить автомобиль", "Șterge mașina"],
        ["Șterge intrarea"] = ["Delete record", "Eintrag löschen", "Elimina voce", "Supprimer l'entrée", "Eliminar registro", "Διαγραφή καταχώρισης", "Kaydı sil", "Удалить запись", "Șterge intrarea"],
        ["Preț în lei (opțional)"] = ["Price (optional)", "Preis (optional)", "Prezzo (opzionale)", "Prix (facultatif)", "Precio (opcional)", "Τιμή (προαιρετικά)", "Fiyat (isteğe bağlı)", "Цена (необязательно)", "Preț (opțional)"],
        ["Firma de asigurare (opțional)"] = ["Insurance company (optional)", "Versicherungsgesellschaft (optional)", "Compagnia assicurativa (facoltativa)", "Compagnie d'assurance (facultatif)", "Aseguradora (opcional)", "Ασφαλιστική εταιρεία (προαιρετικά)", "Sigorta şirketi (isteğe bağlı)", "Страховая компания (необязательно)", "Firma de asigurare (opțional)"],
        ["Data expirării"] = ["Expiry date", "Ablaufdatum", "Data di scadenza", "Date d'expiration", "Fecha de caducidad", "Ημερομηνία λήξης", "Son kullanma tarihi", "Дата окончания", "Data expirării"],
        ["Data cumpărării (opțional)"] = ["Purchase date (optional)", "Kaufdatum (optional)", "Data di acquisto (facoltativa)", "Date d'achat (facultatif)", "Fecha de compra (opcional)", "Ημερομηνία αγοράς (προαιρετικά)", "Satın alma tarihi (isteğe bağlı)", "Дата покупки (необязательно)", "Data cumpărării (opțional)"],
        ["Data intrării în vigoare (opțional)"] = ["Effective date (optional)", "Gültig ab (optional)", "Decorrenza (facoltativa)", "Date d'effet (facultatif)", "Fecha de inicio (opcional)", "Ημερομηνία έναρξης (προαιρετικά)", "Başlangıç tarihi (isteğe bağlı)", "Дата начала действия (необязательно)", "Data intrării în vigoare (opțional)"],
        ["Adaugă data cumpărării"] = ["Add purchase date", "Kaufdatum hinzufügen", "Aggiungi data di acquisto", "Ajouter la date d'achat", "Añadir fecha de compra", "Προσθήκη ημερομηνίας αγοράς", "Satın alma tarihi ekle", "Добавить дату покупки", "Adaugă data cumpărării"],
        ["Adaugă data intrării în vigoare (opțional)"] = ["Add effective date (optional)", "Gültigkeitsdatum hinzufügen (optional)", "Aggiungi decorrenza (facoltativo)", "Ajouter la date d'effet (facultatif)", "Añadir fecha de inicio (opcional)", "Προσθήκη ημερομηνίας έναρξης (προαιρετικά)", "Başlangıç tarihi ekle (isteğe bağlı)", "Добавить дату начала действия (необязательно)", "Adaugă data intrării în vigoare (opțional)"],
        ["Alege data expirării."] = ["Choose an expiry date.", "Wählen Sie ein Ablaufdatum.", "Scegli una data di scadenza.", "Choisissez une date d'expiration.", "Elige una fecha de caducidad.", "Επιλέξτε ημερομηνία λήξης.", "Bir son kullanma tarihi seçin.", "Выберите дату окончания.", "Alege data expirării."],
        ["Intră în vigoare"] = ["Effective from", "Gültig ab", "Decorrenza", "En vigueur le", "En vigor desde", "Ισχύει από", "Başlangıç tarihi", "Действует с", "Intră în vigoare"],
        ["În vigoare din"] = ["Effective from", "Gültig ab", "Decorrenza", "En vigueur le", "En vigor desde", "Ισχύει από", "Başlangıç tarihi", "Действует с", "În vigoare din"],
        ["Expiră"] = ["Expires", "Läuft ab", "Scadenza", "Expire", "Caduca", "Λήγει", "Sona erer", "Истекает", "Expiră"],
        ["Cumpărată"] = ["Purchased", "Gekauft", "Acquistata", "Acheté", "Comprado", "Αγοράστηκε", "Satın alındı", "Куплено", "Cumpărată"],
        ["Începe la"] = ["Starts on", "Beginnt am", "Inizia il", "Commence le", "Empieza el", "Ξεκινά στις", "Başlangıç", "Начинается", "Începe la"],
        ["Expiră la"] = ["Expires on", "Läuft ab am", "Scade il", "Expire le", "Caduca el", "Λήγει στις", "Bitiş", "Истекает", "Expiră la"],
        ["Preț:"] = ["Price:", "Preis:", "Prezzo:", "Prix :", "Precio:", "Τιμή:", "Fiyat:", "Цена:", "Preț:"],
        ["Fără date"] = ["No data", "Keine Daten", "Nessun dato", "Aucune donnée", "Sin datos", "Χωρίς στοιχεία", "Veri yok", "Нет данных", "Fără date"],
        ["Valabil"] = ["Valid", "Gültig", "Valido", "Valide", "Válido", "Σε ισχύ", "Geçerli", "Действителен", "Valabil"],
        ["Expirat"] = ["Expired", "Abgelaufen", "Scaduto", "Expiré", "Caducado", "Έληξε", "Süresi doldu", "Истёк", "Expirat"],
        ["Expiră astăzi"] = ["Expires today", "Läuft heute ab", "Scade oggi", "Expire aujourd'hui", "Caduca hoy", "Λήγει σήμερα", "Bugün sona eriyor", "Истекает сегодня", "Expiră astăzi"],
        ["Data lipsă"] = ["Missing information", "Fehlende Angaben", "Dati mancanti", "Informations manquantes", "Faltan datos", "Λείπουν στοιχεία", "Eksik bilgi", "Не хватает данных", "Date lipsă"],
        ["Date lipsă"] = ["Missing information", "Fehlende Angaben", "Dati mancanti", "Informations manquantes", "Faltan datos", "Λείπουν στοιχεία", "Eksik bilgi", "Не хватает данных", "Date lipsă"],
        ["Confirmă ștergerea"] = ["Confirm deletion", "Löschen bestätigen", "Conferma eliminazione", "Confirmer la suppression", "Confirmar eliminación", "Επιβεβαίωση διαγραφής", "Silmeyi onayla", "Подтвердите удаление", "Confirmă ștergerea"],
        ["Ștergi această intrare din istoric?"] = ["Delete this history entry?", "Diesen Verlaufseintrag löschen?", "Eliminare questa voce dello storico?", "Supprimer cette entrée de l'historique ?", "¿Eliminar este registro del historial?", "Διαγραφή αυτής της καταχώρισης ιστορικού;", "Bu geçmiş kaydı silinsin mi?", "Удалить эту запись из истории?", "Ștergi această intrare din istoric?"],
        ["Ștergi mașina și toate intrările ei din istoric?"] = ["Delete this car and all its history entries?", "Dieses Auto und alle Verlaufseinträge löschen?", "Eliminare questa auto e tutte le voci dello storico?", "Supprimer cette voiture et tout son historique ?", "¿Eliminar este coche y todo su historial?", "Διαγραφή αυτοκινήτου και όλου του ιστορικού του;", "Bu araç ve tüm geçmiş kayıtları silinsin mi?", "Удалить автомобиль и всю его историю?", "Ștergi mașina și toate intrările ei din istoric?"],
        ["Completează denumirea sau numărul de înmatriculare."] = ["Enter a name or registration number.", "Geben Sie einen Namen oder ein Kennzeichen ein.", "Inserisci un nome o una targa.", "Saisissez un nom ou une immatriculation.", "Escribe un nombre o una matrícula.", "Συμπληρώστε όνομα ή αριθμό πινακίδας.", "Bir araç adı veya plaka girin.", "Введите название или регистрационный номер.", "Completează denumirea sau numărul de înmatriculare."],
        ["Imaginea nu a putut fi deschisă."] = ["The image could not be opened.", "Das Bild konnte nicht geöffnet werden.", "Impossibile aprire l'immagine.", "Impossible d'ouvrir l'image.", "No se pudo abrir la imagen.", "Δεν ήταν δυνατό το άνοιγμα της εικόνας.", "Görsel açılamadı.", "Не удалось открыть изображение.", "Imaginea nu a putut fi deschisă."],
        ["Fotografie"] = ["Photo", "Foto", "Foto", "Photo", "Foto", "Φωτογραφία", "Fotoğraf", "Фото", "Fotografie"],
        ["Decuparea nu a putut fi salvată."] = ["The crop could not be saved.", "Der Zuschnitt konnte nicht gespeichert werden.", "Impossibile salvare il ritaglio.", "Impossible d'enregistrer le recadrage.", "No se pudo guardar el recorte.", "Δεν ήταν δυνατή η αποθήκευση της περικοπής.", "Kırpma kaydedilemedi.", "Не удалось сохранить обрезку.", "Decuparea nu a putut fi salvată."],
        ["Alege data intrării în vigoare sau debifează opțiunea pentru a o lăsa necompletată."] = ["Choose an effective date or clear the option to leave it blank.", "Wählen Sie ein Gültigkeitsdatum oder deaktivieren Sie die Option.", "Scegli la decorrenza o deseleziona l'opzione per lasciarla vuota.", "Choisissez une date d'effet ou décochez l'option pour la laisser vide.", "Elige una fecha de inicio o desmarca la opción para dejarla vacía.", "Επιλέξτε ημερομηνία έναρξης ή αποεπιλέξτε την επιλογή.", "Başlangıç tarihi seçin veya boş bırakmak için seçimi kaldırın.", "Выберите дату начала или снимите отметку, чтобы оставить поле пустым.", "Alege data intrării în vigoare sau debifează opțiunea pentru a o lăsa necompletată."],
        ["Data cumpărării nu poate fi după data expirării."] = ["The purchase date cannot be after the expiry date.", "Das Kaufdatum darf nicht nach dem Ablaufdatum liegen.", "La data di acquisto non può essere successiva alla scadenza.", "La date d'achat ne peut pas être postérieure à l'expiration.", "La fecha de compra no puede ser posterior a la fecha de caducidad.", "Η ημερομηνία αγοράς δεν μπορεί να είναι μετά τη λήξη.", "Satın alma tarihi, son kullanma tarihinden sonra olamaz.", "Дата покупки не может быть позже даты окончания.", "Data cumpărării nu poate fi după data expirării."],
        ["Data intrării în vigoare nu poate fi după data expirării."] = ["The effective date cannot be after the expiry date.", "Das Gültigkeitsdatum darf nicht nach dem Ablaufdatum liegen.", "La decorrenza non può essere successiva alla scadenza.", "La date d'effet ne peut pas être postérieure à l'expiration.", "La fecha de inicio no puede ser posterior a la fecha de caducidad.", "Η ημερομηνία έναρξης δεν μπορεί να είναι μετά τη λήξη.", "Başlangıç tarihi, son kullanma tarihinden sonra olamaz.", "Дата начала действия не может быть позже даты окончания.", "Data intrării în vigoare nu poate fi după data expirării."],
        ["Scrie un preț valid, de exemplu 350 sau 350,50."] = ["Enter a valid amount, for example 350 or 350.50.", "Geben Sie einen gültigen Betrag ein, z. B. 350 oder 350,50.", "Inserisci un importo valido, ad esempio 350 o 350,50.", "Saisissez un montant valide, par exemple 350 ou 350,50.", "Escribe un importe válido, por ejemplo 350 o 350,50.", "Εισαγάγετε έγκυρο ποσό, π.χ. 350 ή 350,50.", "350 veya 350,50 gibi geçerli bir tutar girin.", "Введите корректную сумму, например 350 или 350,50.", "Scrie un preț valid, de exemplu 350 sau 350,50."],
        ["GARAJ"] = ["GARAGE", "GARAGE", "GARAGE", "GARAGE", "GARAJE", "ΓΚΑΡΑΖ", "GARAJ", "ГАРАЖ", "GARAJ"],
        ["Cumpărată la"] = ["Purchased on", "Gekauft am", "Acquistata il", "Acheté le", "Comprado el", "Αγοράστηκε στις", "Satın alındı", "Куплено", "Cumpărată la"],
        ["Firma de asigurare"] = ["Insurance company", "Versicherungsgesellschaft", "Compagnia assicurativa", "Compagnie d'assurance", "Aseguradora", "Ασφαλιστική εταιρεία", "Sigorta şirketi", "Страховая компания", "Firma de asigurare"],
        ["ITP"] = ["Inspection", "Hauptuntersuchung", "Revisione", "Contrôle technique", "ITV", "Τεχνικός έλεγχος", "Araç muayenesi", "Техосмотр", "ITP"],
        ["Denumire"] = ["Name", "Name", "Nome", "Nom", "Nombre", "Όνομα", "Ad", "Название", "Denumire"],
        ["Număr de înmatriculare"] = ["Registration number", "Kennzeichen", "Targa", "Immatriculation", "Matrícula", "Αριθμός πινακίδας", "Plaka", "Регистрационный номер", "Număr de înmatriculare"],
        ["Seria de șasiu"] = ["Chassis number", "Fahrgestellnummer", "Numero di telaio", "Numéro de châssis", "Número de chasis", "Αριθμός πλαισίου", "Şasi numarası", "Номер шасси", "Seria de șasiu"],
        ["Preț (opțional)"] = ["Amount (optional)", "Betrag (optional)", "Importo (facoltativo)", "Montant (facultatif)", "Importe (opcional)", "Ποσό (προαιρετικά)", "Tutar (isteğe bağlı)", "Сумма (необязательно)", "Preț (opțional)"],
        ["Ex.: 350 sau 350,50"] = ["e.g. 350 or 350.50", "z. B. 350 oder 350,50", "es. 350 o 350,50", "ex. : 350 ou 350,50", "p. ej., 350 o 350,50", "π.χ. 350 ή 350,50", "örn. 350 veya 350,50", "например, 350 или 350,50", "Ex.: 350 sau 350,50"],
        ["Ex.: Allianz-Țiriac, Groupama"] = ["e.g. Allianz-Țiriac, Groupama", "z. B. Allianz-Țiriac, Groupama", "es. Allianz-Țiriac, Groupama", "ex. : Allianz-Țiriac, Groupama", "p. ej., Allianz-Țiriac, Groupama", "π.χ. Allianz-Țiriac, Groupama", "örn. Allianz-Țiriac, Groupama", "например, Allianz-Țiriac, Groupama", "Ex.: Allianz-Țiriac, Groupama"],
        ["Ex.: B 123 ABC"] = ["e.g. B 123 ABC", "z. B. B 123 ABC", "es. B 123 ABC", "ex. : B 123 ABC", "p. ej., B 123 ABC", "π.χ. B 123 ABC", "örn. B 123 ABC", "например, B 123 ABC", "Ex.: B 123 ABC"],
        ["Ex.: Toyota RAV4 2022"] = ["e.g. Toyota RAV4 2022", "z. B. Toyota RAV4 2022", "es. Toyota RAV4 2022", "ex. : Toyota RAV4 2022", "p. ej., Toyota RAV4 2022", "π.χ. Toyota RAV4 2022", "örn. Toyota RAV4 2022", "например, Toyota RAV4 2022", "Ex.: Toyota RAV4 2022"],
        ["Alege fotografia mașinii"] = ["Choose a car photo", "Fahrzeugfoto auswählen", "Scegli la foto dell'auto", "Choisir une photo du véhicule", "Elegir una foto del coche", "Επιλογή φωτογραφίας αυτοκινήτου", "Araç fotoğrafı seç", "Выберите фото автомобиля", "Alege fotografia mașinii"],
        ["Trage zona de decupare peste mașină. Reglează mărimea cu cursorul."] = ["Drag the crop area over the car. Adjust its size with the slider.", "Ziehen Sie den Ausschnitt über das Auto. Passen Sie die Größe mit dem Schieberegler an.", "Trascina l'area di ritaglio sull'auto. Regola la dimensione con il cursore.", "Faites glisser la zone de recadrage sur la voiture. Ajustez sa taille avec le curseur.", "Arrastra el área de recorte sobre el coche. Ajusta el tamaño con el control deslizante.", "Σύρετε την περιοχή περικοπής πάνω στο αυτοκίνητο. Ρυθμίστε το μέγεθος με το ρυθμιστικό.", "Kırpma alanını aracın üzerine sürükleyin. Boyutunu kaydırıcıyla ayarlayın.", "Перетащите область обрезки на автомобиль. Настройте размер ползунком.", "Trage zona de decupare peste mașină. Reglează mărimea cu cursorul."],
        ["Previzualizarea fotografiei"] = ["Photo preview", "Fotovorschau", "Anteprima foto", "Aperçu photo", "Vista previa de la foto", "Προεπισκόπηση φωτογραφίας", "Fotoğraf önizlemesi", "Предпросмотр фото", "Previzualizarea fotografiei"],
        ["FOTOGRAFIE"] = ["PHOTO", "FOTO", "FOTO", "PHOTO", "FOTO", "ΦΩΤΟΓΡΑΦΙΑ", "FOTOĞRAF", "ФОТО", "FOTOGRAFIE"],
        ["VIN necompletat"] = ["VIN not entered", "FIN nicht angegeben", "VIN non inserito", "VIN non renseigné", "VIN no indicado", "Δεν έχει συμπληρωθεί VIN", "Şasi numarası belirtilmedi", "VIN не указан", "VIN necompletat"],
        ["NUMĂR DE ÎNMATRICULARE"] = ["LICENSE PLATE", "KENNZEICHEN", "TARGA", "IMMATRICULATION", "MATRÍCULA", "ΠΙΝΑΚΙΔΑ", "PLAKA", "ГОСНОМЕР", "NUMĂR DE ÎNMATRICULARE"],
        ["Asigurator"] = ["Insurer", "Versicherer", "Assicuratore", "Assureur", "Aseguradora", "Ασφαλιστής", "Sigortacı", "Страховщик", "Asigurator"],
        ["Asigurator:"] = ["Insurer:", "Versicherer:", "Assicuratore:", "Assureur :", "Aseguradora:", "Ασφαλιστής:", "Sigortacı:", "Страховщик:", "Asigurator:"],
        ["Fără istoric"] = ["No history", "Kein Verlauf", "Nessuno storico", "Aucun historique", "Sin historial", "Δεν υπάρχει ιστορικό", "Geçmiş yok", "История отсутствует", "Fără istoric"],
        ["Este valabil"] = ["Valid", "Gültig", "Valido", "Valide", "Válido", "Σε ισχύ", "Geçerli", "Действителен", "Este valabil"],
        ["Viitor"] = ["Upcoming", "Bevorstehend", "In arrivo", "À venir", "Próximo", "Προσεχώς", "Yaklaşan", "Предстоит", "Viitor"],
        ["Expiră azi"] = ["Expires today", "Läuft heute ab", "Scade oggi", "Expire aujourd'hui", "Caduca hoy", "Λήγει σήμερα", "Bugün sona eriyor", "Истекает сегодня", "Expiră azi"],
        ["CEA MAI NOUĂ"] = ["LATEST", "NEUESTER", "PIÙ RECENTE", "LE PLUS RÉCENT", "MÁS RECIENTE", "ΝΕΟΤΕΡΟ", "EN YENİ", "ПОСЛЕДНЯЯ", "CEA MAI NOUĂ"],
        ["ISTORIC"] = ["HISTORY", "VERLAUF", "STORICO", "HISTORIQUE", "HISTORIAL", "ΙΣΤΟΡΙΚΟ", "GEÇMİŞ", "ИСТОРИЯ", "ISTORIC"],
        ["Decupează fotografia"] = ["Crop photo", "Foto zuschneiden", "Ritaglia foto", "Recadrer la photo", "Recortar foto", "Περικοπή φωτογραφίας", "Fotoğrafı kırp", "Обрезать фото", "Decupează fotografia"],
        ["Mărimea decupajului"] = ["Crop size", "Zuschnittgröße", "Dimensione ritaglio", "Taille du recadrage", "Tamaño del recorte", "Μέγεθος περικοπής", "Kırpma boyutu", "Размер обрезки", "Mărimea decupajului"],
        ["Folosește decupajul"] = ["Use crop", "Zuschnitt verwenden", "Usa ritaglio", "Utiliser le recadrage", "Usar recorte", "Χρήση περικοπής", "Kırpmayı kullan", "Использовать обрезку", "Folosește decupajul"],
        ["Salvează intrarea"] = ["Save record", "Eintrag speichern", "Salva voce", "Enregistrer l'entrée", "Guardar registro", "Αποθήκευση καταχώρισης", "Kaydı kaydet", "Сохранить запись", "Salvează intrarea"],
        ["N2"] = ["N2", "N2", "N2", "N2", "N2", "N2", "N2", "N2", "N2"],
        ["Istoricul este gol. Folosește butonul de mai sus pentru a adăuga prima intrare."] = ["No history yet. Use the button above to add the first record.", "Noch kein Verlauf. Fügen Sie über die Schaltfläche oben den ersten Eintrag hinzu.", "Nessuno storico. Usa il pulsante sopra per aggiungere il primo elemento.", "Aucun historique. Utilisez le bouton ci-dessus pour ajouter le premier document.", "Aún no hay historial. Usa el botón de arriba para añadir el primer registro.", "Δεν υπάρχει ιστορικό. Χρησιμοποιήστε το παραπάνω κουμπί για να προσθέσετε την πρώτη καταχώριση.", "Henüz geçmiş yok. İlk kaydı eklemek için yukarıdaki düğmeyi kullanın.", "История пуста. Добавьте первую запись с помощью кнопки выше.", "Istoricul este gol. Folosește butonul de mai sus pentru a adăuga prima intrare."],
    };

    public static string F(string key, params object?[] values)
    {
        var template = Resolve(key);
        return string.Format(AppSettings.Culture, template, values);
    }

    private static string Resolve(string key)
    {
        var code = AppSettings.Current.Language;
        if (CuratedLanguages.Contains(code) && Rows.TryGetValue(key, out var curated)) return curated[UiLanguageIndex(code)];
        if (GeneratedRows.TryGetValue(code, out var translations) && translations.TryGetValue(key, out var generated)) return generated;
        if (Rows.TryGetValue(key, out var fallback)) return fallback[0];
        return key;
    }

    public static string Days(int count)
    {
        var language = AppSettings.Current.Language;
        if (CuratedLanguages.Contains(language) && language == "ru")
        {
            var last = count % 10;
            var lastTwo = count % 100;
            var unit = last == 1 && lastTwo != 11 ? "день" : last is >= 2 and <= 4 && lastTwo is not (>= 12 and <= 14) ? "дня" : "дней";
            return $"{count} {unit}";
        }
        if (CuratedLanguages.Contains(language)) return language switch
        {
            "de" => $"{count} {(count == 1 ? "Tag" : "Tage")}",
            "it" => $"{count} {(count == 1 ? "giorno" : "giorni")}",
            "fr" => $"{count} {(count == 1 ? "jour" : "jours")}",
            "es" => $"{count} {(count == 1 ? "día" : "días")}",
            "el" => $"{count} {(count == 1 ? "ημέρα" : "ημέρες")}",
            "tr" => $"{count} gün",
            "ro" => $"{count} {(count == 1 ? "zi" : "zile")}",
            _ => $"{count} {(count == 1 ? "day" : "days")}"
        };
        return F(count == 1 ? "ZXQ_UNIT_DAY_ONE" : "ZXQ_UNIT_DAY_MANY", count);
    }

    public static string T(string text)
    {
        var profile = CountryCatalog.Find(AppSettings.Current.Country);
        if (profile is not null && (text == profile.RoadToll || text == profile.Inspection || text == profile.Insurance)) return text;
        foreach (var pair in Rows)
            if (string.Equals(text, pair.Key, StringComparison.Ordinal)) return Resolve(pair.Key);
        var days = Regex.Match(text, "^Expiră în (\\d+) zile$");
        if (days.Success)
        {
            var count = int.Parse(days.Groups[1].Value, CultureInfo.InvariantCulture);
            return F("Expires in {0}", Days(count));
        }
        var startDays = Regex.Match(text, "^Intră în vigoare în (\\d+) zile$");
        if (startDays.Success)
        {
            var count = int.Parse(startDays.Groups[1].Value, CultureInfo.InvariantCulture);
            return F("Starts in {0}", Days(count));
        }
        var dayCount = Regex.Match(text, "^(\\d+) zile$");
        if (dayCount.Success)
        {
            var count = int.Parse(dayCount.Groups[1].Value, CultureInfo.InvariantCulture);
            return Days(count);
        }
        var expired = Regex.Match(text, "^A expirat acum (\\d+) zile · (.+)$");
        if (expired.Success)
        {
            var n = expired.Groups[1].Value; var date = expired.Groups[2].Value;
            return F("Expired: {0} ago · {1}", Days(int.Parse(n, CultureInfo.InvariantCulture)), date);
        }
        var future = Regex.Match(text, "^Începe la (.+) · Expiră la (.+)$");
        if (future.Success) return $"{T("Începe la")} {future.Groups[1].Value} · {T("Expiră la")} {future.Groups[2].Value}";
        if (text.StartsWith("＋   ", StringComparison.Ordinal)) return "＋   " + T(text[4..]);
        if (text.StartsWith("＋  ", StringComparison.Ordinal)) return "＋  " + T(text[3..]);
        if (text.StartsWith("Adaugă ", StringComparison.Ordinal)) return T("Add") + " " + text[7..];
        if (text.StartsWith("Editează ", StringComparison.Ordinal)) return T("Edit") + " " + text[9..];
        return text;
    }

    private static int UiLanguageIndex(string language) => language switch
    {
        "de" => 1, "it" => 2, "fr" => 3, "es" => 4, "el" => 5, "tr" => 6, "ru" => 7, "ro" => 8, _ => 0
    };

    public static string CarCount(int count)
    {
        if (count == 1) return T("Car count one");
        if (AppSettings.Current.Language == "ru")
        {
            var last = count % 10; var lastTwo = count % 100;
            var noun = last is >= 2 and <= 4 && lastTwo is not (>= 12 and <= 14) ? "автомобиля" : "автомобилей";
            return $"({count} {noun})";
        }
        return string.Format(AppSettings.Culture, T("Car count many"), count);
    }
}


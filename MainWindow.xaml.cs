using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HudhudUnlocker;

public partial class MainWindow : Window
{
    private const string SURUM = "1.0.3";
    private const string SATIN_AL_URL = "https://ahmetrauf.gumroad.com/l/kstgvb";
    private const string SATIN_AL_FIYAT = "$5";
    private const int DENEME_GUN = 30;
    private const string ADMIN_HASH = "4bf4d546cd281040f63c49996a03d617209d0057c859794fc6320fe853367afd";
    private const string SURUM_BILGI_URL = "https://www.hudhudapps.com/hudhud-unlocker-surum.json";
    private const string INDIRME_SAYFASI_URL = "https://www.hudhudapps.com/hudhud-unlocker.html";

    private readonly AppSettings _s = AppSettings.Load();
    private string _lang = "en";
    private string _target = "";
    private string _remoteVer = "";
    private readonly ObservableCollection<LockInfo> _items = new();

    public MainWindow()
    {
        InitializeComponent();
        Title = "Hudhud Unlocker " + SURUM + " — Hudhud Apps";
        GridLockers.ItemsSource = _items;

        _lang = _s.Language;
        if (Array.IndexOf(Strings.Langs, _lang) < 0)
        {
            var ui = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            _lang = Array.IndexOf(Strings.Langs, ui) >= 0 ? ui : "en";
        }
        foreach (ComboBoxItem it in CmbLang.Items)
            if ((string)it.Tag == _lang) { CmbLang.SelectedItem = it; break; }
        ApplyLanguage();

        Loaded += async (_, __) =>
        {
            if (!string.IsNullOrWhiteSpace(App.StartPath)) SetTarget(App.StartPath);
            if (!IsLicensed)
                Dispatcher.BeginInvoke(new Action(ShowTrialWindow), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            await CheckUpdateAsync();
        };
    }

    // ---------- i18n ----------
    private string L(string key)
    {
        if (!Strings.T.TryGetValue(key, out var arr)) return key;
        int i = Array.IndexOf(Strings.Langs, _lang);
        return arr[i < 0 ? 1 : i];
    }

    private void ApplyLanguage()
    {
        LblTagline.Text = L("Tagline");
        BtnFile.Content = L("File"); BtnFolder.Content = L("Folder");
        LblLockers.Text = L("Lockers");
        ColName.Header = L("ColProcess"); ColPath.Header = L("ColPath");
        BtnScan.Content = L("Scan"); BtnUnlockAll.Content = L("UnlockAll"); BtnKill.Content = L("KillSel");
        BtnDelete.Content = L("Delete"); BtnRename.Content = L("Rename"); BtnMove.Content = L("Move");
        BtnBuy.Content = "💳 " + L("Buy");
        MenuHelp.Header = L("MenuHelp"); MenuBuy.Header = "💳 " + L("Buy"); MenuAbout.Header = L("About");
        LblDrop.Text = L("DropHint");
        LblDrop.Visibility = string.IsNullOrEmpty(_target) ? Visibility.Visible : Visibility.Collapsed;
        UpdateLicenseLabel();
        if (_remoteVer != "") { BtnUpdate.Content = string.Format(L("UpdateAvailable"), _remoteVer); BtnUpdate.Visibility = Visibility.Visible; }
        if (string.IsNullOrEmpty(_target)) LblStatus.Text = L("Ready");
    }

    private void CmbLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || CmbLang.SelectedItem is not ComboBoxItem it) return;
        _lang = (string)it.Tag;
        _s.Language = _lang; _s.Save();
        ApplyLanguage();
    }

    // ---------- lisans ----------
    private static string Sha(string s) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(s.Trim()))).ToLowerInvariant();

    private bool IsAdmin => !string.IsNullOrWhiteSpace(_s.RegCode) && Sha(_s.RegCode) == ADMIN_HASH;
    private bool IsLicensed => !string.IsNullOrWhiteSpace(_s.RegCode);

    private int TrialDaysLeft()
    {
        if (!DateTime.TryParse(_s.FirstRunDate, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var first))
        {
            first = DateTime.Today;
            _s.FirstRunDate = first.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            _s.Save();
        }
        return Math.Max(0, DENEME_GUN - (int)(DateTime.Today - first).TotalDays);
    }

    private string LicenseText() =>
        IsAdmin ? L("StatusAdmin") : IsLicensed ? string.Format(L("StatusReg"), _s.RegName) : string.Format(L("StatusTrial"), TrialDaysLeft());

    private void UpdateLicenseLabel() => LblLicense.Text = LicenseText();

    /// <summary>Kilit açma/silme/taşıma gibi işlemlere izin verir: kayıtlı veya deneme süresi içinde.</summary>
    private bool ActionAllowed()
    {
        if (IsLicensed || TrialDaysLeft() > 0) return true;
        if (System.Windows.MessageBox.Show(L("PaidOnly"), L("Title"), MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
            ShowBuyWindow();
        return false;
    }

    // ---------- hedef seçimi ----------
    private void BtnFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { CheckFileExists = true };
        if (dlg.ShowDialog(this) == true) SetTarget(dlg.FileName);
    }

    private void BtnFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog();
        if (dlg.ShowDialog(this) == true) SetTarget(dlg.FolderName);
    }

    private void Window_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is string[] files && files.Length > 0)
            SetTarget(files[0]);
    }

    private void SetTarget(string path)
    {
        path = path.Trim().Trim('"');
        if (!File.Exists(path) && !Directory.Exists(path)) { LblStatus.Text = L("NoItem"); return; }
        _target = path;
        TxtPath.Text = path;
        LblDrop.Visibility = Visibility.Collapsed;
        Scan();
    }

    private bool HasTarget()
    {
        if (!string.IsNullOrEmpty(_target) && (File.Exists(_target) || Directory.Exists(_target))) return true;
        LblStatus.Text = L("NoItem");
        System.Windows.MessageBox.Show(L("NoItem"), L("Title"), MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private void Scan()
    {
        if (string.IsNullOrEmpty(_target)) return;
        LblStatus.Text = L("Scanning");
        _items.Clear();
        foreach (var li in LockFinder.Find(_target)) _items.Add(li);
        LblStatus.Text = _items.Count == 0 ? L("NoLock") : string.Format(L("Found"), _items.Count);
    }

    private void BtnScan_Click(object sender, RoutedEventArgs e) { if (HasTarget()) Scan(); }

    // ---------- işlemler ----------
    private bool KillList(List<LockInfo> list)
    {
        if (list.Count == 0) return true;
        var names = string.Join("\n", list.Select(x => $"{x.Name} (PID {x.Pid})"));
        if (System.Windows.MessageBox.Show(string.Format(L("ConfirmKill"), names), L("Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return false;
        int n = 0;
        foreach (var li in list) if (LockFinder.Kill(li.Pid)) n++;
        LblStatus.Text = string.Format(L("Unlocked"), n);
        return true;
    }

    private void BtnUnlockAll_Click(object sender, RoutedEventArgs e)
    {
        if (!HasTarget() || !ActionAllowed()) return;
        Scan();
        if (_items.Count == 0) return;
        KillList(_items.ToList());
        Scan();
    }

    private void BtnKill_Click(object sender, RoutedEventArgs e)
    {
        if (!HasTarget() || !ActionAllowed()) return;
        var sel = GridLockers.SelectedItems.Cast<LockInfo>().ToList();
        if (sel.Count == 0) return;
        KillList(sel);
        Scan();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (!HasTarget() || !ActionAllowed()) return;
        if (System.Windows.MessageBox.Show(string.Format(L("ConfirmDelete"), _target), L("Title"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;
        if (!TryAction(() => DeleteTarget()))
        {
            // Kilitleyen işlemleri kapatmayı öner, sonra yeniden dene.
            Scan();
            if (_items.Count > 0 && KillList(_items.ToList()) && TryAction(() => DeleteTarget())) { AfterDelete(); return; }
            if (LockFinder.DeleteOnReboot(_target)) { LblStatus.Text = L("DeleteLater"); System.Windows.MessageBox.Show(L("DeleteLater"), L("Title")); }
            else LblStatus.Text = string.Format(L("Failed"), _target);
            return;
        }
        AfterDelete();
    }

    private void DeleteTarget()
    {
        if (Directory.Exists(_target)) Directory.Delete(_target, true);
        else File.Delete(_target);
    }

    private void AfterDelete()
    {
        LblStatus.Text = L("Deleted");
        _target = ""; TxtPath.Text = ""; _items.Clear();
        LblDrop.Visibility = Visibility.Visible;
    }

    private void BtnRename_Click(object sender, RoutedEventArgs e)
    {
        if (!HasTarget() || !ActionAllowed()) return;
        var oldName = System.IO.Path.GetFileName(_target.TrimEnd('\\', '/'));
        var name = Ask(L("Rename"), L("RenamePrompt"), oldName);
        if (string.IsNullOrWhiteSpace(name) || name == oldName) return;
        var dest = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_target.TrimEnd('\\', '/')) ?? "", name);
        if (!MoveWithRetry(_target, dest)) return;
        LblStatus.Text = L("Renamed");
        SetTarget(dest);
    }

    private void BtnMove_Click(object sender, RoutedEventArgs e)
    {
        if (!HasTarget() || !ActionAllowed()) return;
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = L("SelectNew") };
        if (dlg.ShowDialog(this) != true) return;
        var dest = System.IO.Path.Combine(dlg.FolderName, System.IO.Path.GetFileName(_target.TrimEnd('\\', '/')));
        if (!MoveWithRetry(_target, dest)) return;
        LblStatus.Text = string.Format(L("Moved"), dest);
        SetTarget(dest);
    }

    private bool MoveWithRetry(string from, string to)
    {
        void Do()
        {
            if (Directory.Exists(from)) Directory.Move(from, to);
            else File.Move(from, to);
        }
        if (TryAction(Do)) return true;
        Scan();
        if (_items.Count > 0 && KillList(_items.ToList()) && TryAction(Do)) return true;
        return false;
    }

    private bool TryAction(Action a)
    {
        try { a(); return true; }
        catch (Exception ex) { LblStatus.Text = string.Format(L("Failed"), ex.Message); return false; }
    }

    private string? Ask(string title, string label, string def)
    {
        string? result = null;
        var win = new Window { Title = title, Width = 380, SizeToContent = SizeToContent.Height, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        var sp = new StackPanel { Margin = new Thickness(18) };
        sp.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 6) });
        var tb = new System.Windows.Controls.TextBox { Text = def, Padding = new Thickness(6, 5, 6, 5) };
        sp.Children.Add(tb);
        var ok = new System.Windows.Controls.Button { Content = L("OK"), Style = (Style)FindResource("Btn"), Margin = new Thickness(0, 14, 0, 0), HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        ok.Click += (_, __) => { result = tb.Text; win.Close(); };
        sp.Children.Add(ok);
        win.Content = sp;
        tb.SelectAll(); tb.Focus();
        win.ShowDialog();
        return result;
    }

    // ---------- satın alma / deneme / hakkında ----------
    private void BtnUpdate_Click(object sender, RoutedEventArgs e) => OpenUrl(INDIRME_SAYFASI_URL);

    private void BtnBuy_Click(object sender, RoutedEventArgs e) => ShowBuyWindow();

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    private static System.Windows.Documents.Hyperlink MakeLink(string text, Action click)
    {
        var h = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(text));
        h.Click += (_, __) => click();
        return h;
    }

    private void ShowBuyWindow()
    {
        var yesil = new SolidColorBrush(Color.FromRgb(0x00, 0xA3, 0x00));
        var gri = Brushes.DimGray;
        var win = new Window
        {
            Title = L("BuyWinTitle"), Width = 440, SizeToContent = SizeToContent.Height, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0))
        };
        var sp = new StackPanel { Margin = new Thickness(24) };

        var fiyat = new Border { Background = Brushes.White, Padding = new Thickness(16), Margin = new Thickness(0, 0, 0, 10) };
        var fs = new StackPanel();
        fs.Children.Add(new TextBlock { Text = SATIN_AL_FIYAT, FontSize = 36, FontWeight = FontWeights.Bold, Foreground = yesil, HorizontalAlignment = HorizontalAlignment.Center });
        fs.Children.Add(new TextBlock { Text = L("BuyTL"), FontSize = 12, Foreground = gri, HorizontalAlignment = HorizontalAlignment.Center });
        fs.Children.Add(new TextBlock { Text = L("BuyLife"), FontSize = 11, FontStyle = FontStyles.Italic, Foreground = gri, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 4, 0, 0) });
        fiyat.Child = fs; sp.Children.Add(fiyat);

        var ozel = new Border { Background = Brushes.White, Padding = new Thickness(16, 12, 16, 8), Margin = new Thickness(0, 0, 0, 14) };
        var os = new StackPanel();
        foreach (var k in new[] { "BuyF1", "BuyF2", "BuyF3", "BuyF4" })
        {
            var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            row.Children.Add(new TextBlock { Text = "✓ ", Foreground = yesil, FontWeight = FontWeights.Bold });
            row.Children.Add(new TextBlock { Text = L(k), Foreground = gri });
            os.Children.Add(row);
        }
        ozel.Child = os; sp.Children.Add(ozel);

        var al = new System.Windows.Controls.Button { Content = "🔑  " + L("BuyLicense"), Style = (Style)FindResource("Btn"), Background = yesil, Padding = new Thickness(0, 12, 0, 12), Margin = new Thickness(0), FontSize = 14 };
        al.Click += (_, __) => OpenUrl(SATIN_AL_URL);
        sp.Children.Add(al);
        sp.Children.Add(new TextBlock { Text = "🔒 " + L("BuySecure"), FontSize = 11, Foreground = gri, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 12, 0, 12) });

        var alt = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        var vaz = new TextBlock(); vaz.Inlines.Add(MakeLink(L("Cancel"), () => win.Close()));
        DockPanel.SetDock(vaz, Dock.Right); alt.Children.Add(vaz);
        var kodT = new TextBlock(); kodT.Inlines.Add(MakeLink(L("BuyHaveCode"), () => { win.Close(); ShowRegisterWindow(); }));
        alt.Children.Add(kodT);
        sp.Children.Add(alt);

        sp.Children.Add(new TextBlock { Text = LicenseText(), FontSize = 11, Foreground = gri, HorizontalAlignment = HorizontalAlignment.Center });
        win.Content = sp;
        win.ShowDialog();
    }

    private void ShowRegisterWindow()
    {
        var win = new Window
        {
            Title = L("RegHeader"), Width = 380, SizeToContent = SizeToContent.Height, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize
        };
        var sp = new StackPanel { Margin = new Thickness(20) };
        var ad = new System.Windows.Controls.TextBox { Text = _s.RegName, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 0, 0, 8) };
        var kod = new System.Windows.Controls.TextBox { Text = _s.RegCode, Padding = new Thickness(6, 4, 6, 4), Margin = new Thickness(0, 0, 0, 12) };
        sp.Children.Add(new TextBlock { Text = L("RegName") }); sp.Children.Add(ad);
        sp.Children.Add(new TextBlock { Text = L("RegCode") }); sp.Children.Add(kod);
        var kaydet = new System.Windows.Controls.Button { Content = L("RegSave"), Style = (Style)FindResource("Btn"), Margin = new Thickness(0) };
        kaydet.Click += (_, __) =>
        {
            if (string.IsNullOrWhiteSpace(kod.Text) || (string.IsNullOrWhiteSpace(ad.Text) && Sha(kod.Text) != ADMIN_HASH))
            { System.Windows.MessageBox.Show(L("RegMissing"), L("RegHeader"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            _s.RegName = ad.Text.Trim(); _s.RegCode = kod.Text.Trim();
            if (Sha(_s.RegCode) == ADMIN_HASH && string.IsNullOrWhiteSpace(_s.RegName)) _s.RegName = "CAHİT KÜÇÜK";
            _s.Save();
            UpdateLicenseLabel();
            System.Windows.MessageBox.Show(string.Format(L("RegThanks"), _s.RegName), L("RegHeader"), MessageBoxButton.OK, MessageBoxImage.Information);
            win.Close();
        };
        sp.Children.Add(kaydet);
        win.Content = sp;
        win.ShowDialog();
    }

    private void ShowTrialWindow()
    {
        int kalan = TrialDaysLeft();
        var yesil = new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A));
        var win = new Window
        {
            Title = "Hudhud Unlocker - " + L("TrialTitle"), Width = 460, SizeToContent = SizeToContent.Height, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0))
        };
        var sp = new StackPanel();
        var banner = new Border { Background = yesil, Padding = new Thickness(18, 14, 18, 14) };
        var bs = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        bs.Children.Add(new TextBlock { Text = "💡", FontSize = 24, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center });
        bs.Children.Add(new TextBlock { Text = kalan > 0 ? string.Format(L("TrialMsg"), DENEME_GUN) : L("TrialMsgEnded"), Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap, Width = 340, VerticalAlignment = VerticalAlignment.Center });
        banner.Child = bs; sp.Children.Add(banner);
        var body = new StackPanel { Margin = new Thickness(18) };
        body.Children.Add(new TextBlock { Text = kalan > 0 ? string.Format(L("StatusTrial"), kalan) : L("TrialEndedLabel"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        body.Children.Add(new ProgressBar { Minimum = 0, Maximum = DENEME_GUN, Value = DENEME_GUN - kalan, Height = 14,
            Foreground = kalan > 10 ? Brushes.Green : kalan > 3 ? Brushes.Orange : Brushes.Firebrick });
        var row = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 18, 0, 0) };
        System.Windows.Controls.Button B(string t, Action a, Brush? bg = null)
        {
            var b = new System.Windows.Controls.Button { Content = t, Style = (Style)FindResource("Btn"), Margin = new Thickness(6, 0, 0, 0), Background = bg ?? new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B)) };
            b.Click += (_, __) => a();
            return b;
        }
        row.Children.Add(B(L("BuyNow"), () => { win.Close(); ShowBuyWindow(); }, yesil));
        row.Children.Add(B(L("TrialCode"), () => { win.Close(); ShowRegisterWindow(); }));
        row.Children.Add(B(L("TrialClose"), () => win.Close()));
        body.Children.Add(row);
        var lt = new TextBlock { Margin = new Thickness(0, 16, 0, 0) };
        lt.Inlines.Add(MakeLink("www.hudhudapps.com", () => OpenUrl("https://www.hudhudapps.com")));
        body.Children.Add(lt);
        sp.Children.Add(body);
        win.Content = sp;
        win.ShowDialog();
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
    {
        var win = new Window
        {
            Title = L("About"), Width = 420, SizeToContent = SizeToContent.Height, Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize, Background = Brushes.White
        };
        var sp = new StackPanel { Margin = new Thickness(24) };
        sp.Children.Add(new TextBlock { Text = "Hudhud Unlocker " + SURUM, FontSize = 20, FontWeight = FontWeights.Bold });
        sp.Children.Add(new TextBlock { Text = "© 2026 Hudhud Apps", FontSize = 12, Margin = new Thickness(0, 2, 0, 14) });
        var links = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
        void Link(string text, string url)
        {
            var tb = new TextBlock { Margin = new Thickness(0, 0, 20, 6) };
            tb.Inlines.Add(MakeLink(text, () => OpenUrl(url)));
            links.Children.Add(tb);
        }
        Link(L("LinkHome"), "https://www.hudhudapps.com");
        Link(L("LinkSupport"), "mailto:destek@hudhudapps.com");
        Link(L("LinkTerms"), "https://www.hudhudapps.com/hudhud-unlocker-kullanim-sartlari.html");
        Link(L("LinkPrivacy"), "https://www.hudhudapps.com/hudhud-unlocker-gizlilik.html");
        sp.Children.Add(links);
        sp.Children.Add(new TextBlock { Text = L("RegisteredPerson"), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 4) });
        string kisi = IsLicensed ? (string.IsNullOrWhiteSpace(_s.RegName) ? L("RegisteredUser") : _s.RegName.ToUpper(new System.Globalization.CultureInfo("tr-TR"))) : L("Unregistered");
        sp.Children.Add(new System.Windows.Controls.TextBox { Text = kisi, IsReadOnly = true, Padding = new Thickness(6), Foreground = IsLicensed ? Brushes.Green : Brushes.Gray });
        win.Content = sp;
        win.ShowDialog();
    }

    // ---------- güncelleme denetimi ----------
    private async Task CheckUpdateAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            var json = await http.GetStringAsync(SURUM_BILGI_URL + "?t=" + DateTime.UtcNow.Ticks);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var uzak = doc.RootElement.GetProperty("surum").GetString() ?? "";
            if (Version.TryParse(uzak, out var rv) && Version.TryParse(SURUM, out var lv) && rv > lv)
            {
                _remoteVer = uzak;
                BtnUpdate.Content = string.Format(L("UpdateAvailable"), uzak);
                BtnUpdate.Visibility = Visibility.Visible;
            }
        }
        catch { }
    }
}

using System.ServiceModel.Syndication;
using System.Timers;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

class Program
{
    private static readonly string BotToken = "7669818545:AAGBghPPcpAnkO-WmfHw8mwbOH_Zh3E9lxU";
    private static readonly TelegramBotClient BotClient = new TelegramBotClient(BotToken);
    private static Dictionary<long, string> UserCategories = new Dictionary<long, string>();

    // RSS kaynakları  
    private static readonly Dictionary<string, string> RssFeeds = new Dictionary<string, string>
    {
        { "haberturk", "https://www.haberturk.com/rss/manset.xml" },
        { "ntv", "https://www.ntv.com.tr/son-dakika.rss" },
        { "hurriyet", "https://www.hurriyet.com.tr/rss/ekonomi" }
    };

    private static System.Timers.Timer NewsTimer;

    static async Task Main(string[] args)
    {
        Console.WriteLine("Bot çalışıyor...");
        BotClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
        StartNewsTimer();
        Thread.Sleep(Timeout.Infinite); //free azure maks 60dk tutacak
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            var message = update.Message;
            var chatId = message.Chat.Id;

            if (message.Text.ToLower() == "/start")
            {
                string welcomeMessage = "Merhaba! Haber botuna hoş geldiniz.\n" +
                                        "Lütfen bir kategori seçin:\n" +
                                        "/haberturk - Teknoloji Haberleri\n" +
                                        "/ntv - Spor Haberleri\n" +
                                        "/hurriyet - Ekonomi Haberleri";
                await botClient.SendTextMessageAsync(chatId, welcomeMessage);
            }
            else if (RssFeeds.ContainsKey(message.Text.ToLower().TrimStart('/')))
            {
                string category = message.Text.ToLower().TrimStart('/');
                UserCategories[chatId] = category;
                await botClient.SendTextMessageAsync(chatId, $"Kategori seçildi: {category}. Haberler düzenli olarak gönderilecektir.");
                Console.WriteLine($"Kategori seçildi: {category}. Haberler düzenli olarak gönderilecektir.");
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Geçersiz komut. Lütfen bir kategori seçmek için /start yazın.");
                Console.WriteLine($"Geçersiz komut. Lütfen bir kategori seçmek için /start yazın.");
            }
        }
    }

    // Hataları işleyen metot  
    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Hata: {exception.Message}");
        return Task.CompletedTask;
    }

    private static async void SendNewsToUsers(object sender, ElapsedEventArgs e)
    {
        foreach (var user in UserCategories)
        {
            long chatId = user.Key;
            string category = user.Value;

            if (RssFeeds.ContainsKey(category))
            {
                string rssUrl = RssFeeds[category];
                string news = GetLatestNews(rssUrl);

                await BotClient.SendTextMessageAsync(chatId, $"Son {category} haberleri:\n\n{news}");
            }
        }
    }

    // RSS'den son haberleri çek
    private static string GetLatestNews(string rssUrl)
    {
        try
        {
            using (XmlReader reader = XmlReader.Create(rssUrl))
            {
                SyndicationFeed feed = SyndicationFeed.Load(reader);
                reader.Close();

                //15 haberi al
                var latestNews = feed.Items.Take(15).Select(item => $"- {item.Title.Text} ({item.Links[0].Uri})");
                return string.Join("\n", latestNews);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RSS okuma hatası: {ex.Message}");
            return "Haberler alınamadı.";
        }
    }

    // Zamanlayıcıyı başlatan metot  
    private static void StartNewsTimer()
    {
        // 1 saat (3600000 ms)  
        // 10 dk (60000 ms)
        // 5 dakika (30000 ms)
        NewsTimer = new System.Timers.Timer(600000); 
        NewsTimer.Elapsed += SendNewsToUsers;
        NewsTimer.AutoReset = true;
        NewsTimer.Enabled = true;

        Console.WriteLine("Haber gönderim zamanlayıcısı başlatıldı.");
    }
}
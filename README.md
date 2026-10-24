# Subscription Tracker

Кросплатформний застосунок (Windows + Android) для обліку платних підписок: Spotify, Netflix, Game Pass тощо.
Показує, скільки коштують підписки на місяць і рік, нагадує про найближчі списання й будує статистику.

## Завантаження та встановлення

Готові інсталятори — на сторінці **[Releases](https://github.com/KaByH-xd/Subscription_Tracking-Analysis_app/releases/latest)**:

| Платформа | Файл | Як встановити |
|---|---|---|
| **Windows 10/11** (x64) | `SubscriptionTracker-Setup-x.y.z.exe` | Запустіть інсталятор (права адміністратора не потрібні). Якщо з'явиться SmartScreen — «Докладніше» → «Виконати все одно». |
| **Android 5+** | `SubscriptionTracker-x.y.z.apk` | Відкрийте файл на телефоні та дозвольте встановлення з цього джерела. |

На Windows застосунок за замовчуванням запускається у фоновому режимі під час входу в систему (без вікна),
тому нагадування приходять вчасно навіть із закритим вікном. Вимкнути: **Settings → Run in background at Windows startup**.

### Нагадування про оплату

Сповіщення приходить **щодня о 10:00** протягом вибраного періоду перед списанням.
Наприклад, при «3 days»: за 3 дні, за 2 дні та за 1 день до оплати.
Якщо о 10:00 комп'ютер був вимкнений, нагадування з'явиться одразу після ввімкнення.

### Як випустити нову версію

```bash
git tag v1.2.1
git push origin v1.2.1
```

GitHub Actions (`.github/workflows/release.yml`) збере Windows-інсталятор (Inno Setup) та Android APK і опублікує їх у Releases.

Для стабільного підпису Android-застосунку (щоб оновлення встановлювалися поверх старої версії) додайте в
**Settings → Secrets and variables → Actions** репозиторію секрети `ANDROID_KEYSTORE_BASE64` і `ANDROID_KEYSTORE_PASSWORD`
(alias ключа — `subscriptiontracker`). Без них кожна збірка підписується тимчасовим ключем.

## Стек технологій

| Технологія | Призначення |
|---|---|
| C# | Основна мова програмування |
| .NET MAUI | Кросплатформний застосунок для Windows та Android |
| XAML | Графічний інтерфейс |
| SQLite | Локальне зберігання підписок |
| sqlite-net-pcl | Робота з SQLite із C# |
| MVVM | Розділення інтерфейсу та логіки |
| CommunityToolkit.Mvvm | `[ObservableProperty]`, `[RelayCommand]` |
| Plugin.LocalNotification | Системні сповіщення на Android |
| Microsoft.Toolkit.Uwp.Notifications | Системні сповіщення (toast) на Windows |
| Visual Studio Code | Редактор коду |
| .NET SDK | Збирання та запуск |
| Git та GitHub | Контроль версій |

## Інтерфейс

- Мова інтерфейсу — **англійська**.
- Шрифт — **Poppins** (Google Fonts, ліцензія OFL, файли в `Resources/Fonts`): геометричний шрифт із чіткими цифрами, добре підходить для фінансових застосунків.

## Функціонал

- **Головна (Dashboard):** витрати на місяць/рік, вартість за день, найближче списання, 5 найближчих платежів.
- **Бюджет:** місячний ліміт із прогрес-баром (🟢 в межах / 🔴 перевищено).
- **Нагадування:** блок із платежами, до яких залишилось 1/3/7 днів (налаштовується).
- **Підписки:** пошук, фільтр за статусом і категорією, сортування; натискання відкриває редагування.
- **Додавання / редагування / видалення / призупинення** підписки; підтримка місячних і річних підписок.
- **Календар платежів:** місячна сітка з позначками днів списань і списком платежів місяця.
- **Статистика:** витрати за категоріями (%), найдорожчі підписки, аналітичний висновок.
- **Налаштування:** бюджет, нагадування, демо-дані, очищення бази.
- Дати оплати, що минули, автоматично переносяться на наступний період.
- **Логотипи сервісів:** підбираються автоматично за назвою (Spotify → spotify.com) або за вказаним сайтом; кешуються локально й працюють офлайн. Якщо логотип не знайдено — показується емодзі категорії.
- **Системні сповіщення:** нагадування за 1/3/7 днів до списання о 10:00, навіть коли застосунок закритий (Android — Plugin.LocalNotification, Windows — toast-сповіщення). Кнопка «Send test notification» у Settings.
- **Кілька валют:** UAH, USD, EUR, GBP, PLN. Усі підсумки перераховуються в гривні за офіційним курсом НБУ (оновлюється автоматично двічі на добу або кнопкою «Update rates»; без інтернету використовуються збережені курси).
- **Темна тема:** перемикач System / Light / Dark у Settings, вибір зберігається.

## Структура

```
SubscriptionTracker/
├── Models/        Subscription.cs, Categories.cs
├── Services/      IDatabaseService.cs, DatabaseService.cs (sqlite-net-pcl), SettingsService.cs
├── ViewModels/    Dashboard, Subscriptions, EditSubscription, Calendar, Statistics, Settings
├── Views/         XAML-сторінки
├── Resources/     стилі, кольори, іконка, splash
├── Platforms/     Android, Windows
├── App.xaml, AppShell.xaml, MauiProgram.cs (DI)
```

Архітектура: `View (XAML) → ViewModel (CommunityToolkit.Mvvm) → DatabaseService (sqlite-net-pcl) → SQLite (subscriptions.db)`.

## Запуск

Потрібно: .NET SDK 9 (закріплено в `global.json`) та MAUI workload:

```bash
dotnet workload install maui
```

**Windows:** двічі клацнути `run-windows.cmd` або виконати:

```bash
cd SubscriptionTracker
dotnet build -t:Run -f net9.0-windows10.0.19041.0
```

**Android:** запустіть емулятор або підключіть телефон, потім `run-android.cmd` (скрипт також переносить TEMP у `C:\3kurs\tmp`, бо Android-інструменти не працюють із кирилицею в імені користувача) або:

```bash
cd SubscriptionTracker
dotnet build -t:Run -f net9.0-android
```


Щоб швидко побачити застосунок із даними: **Settings → Add demo subscriptions**.

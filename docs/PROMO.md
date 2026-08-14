# Promo posts (draft)

Ready-to-post announcements. Replace `[LINK]` with the repo/release URL:
https://github.com/Hehehers1488/yandex-music-gamebar-widget

---

## EN — short (Reddit r/XboxGameBar, r/Windows, r/Music)

> I made a free open-source widget that puts **Yandex Music** into the Xbox Game Bar
> overlay (Win + G). See the track, album art and progress bar right over your game,
> control playback without tabbing out.
>
> - Track info + progress with seek (click/drag, ±5s buttons)
> - Play/pause, next, previous
> - UI scales proportionally with the window
> - Windows 10 2004+ / 11, x64 & ARM64, signed MSIX
> - No tokens or accounts — it uses the system media session (SMTC)
>
> **[LINK]** — install with `install.bat` (double-click, accept the UAC prompt).
> It's MIT-licensed, feedback and PRs welcome.

## EN — long (blog / Hacker News)

> I built a small UWP widget that brings Yandex Music into the Xbox Game Bar overlay.
> While gaming fullscreen you often want to see what's playing and switch tracks without
> alt-tabbing — and Yandex Music has no Game Bar widget, unlike Spotify.
>
> The widget reads the track through the SystemMediaTransportControls session that the
> desktop Yandex Music app publishes, so there are no tokens, no API keys, no account
> login — it just works when the app is playing. It auto-selects the Yandex session and
> ignores Spotify/Groove/other sessions.
>
> What's inside: title/artist/album art, a seekable progress bar (click or drag, plus
> ±5s buttons), play/pause/next/previous. The window resizes freely and the whole UI
> scales proportionally (a Viewbox). Ships as a signed MSIX, x64 and ARM64, with a
> `install.bat` (double-click, accept the UAC prompt).
>
> Some things worth knowing if you plan something similar:
> - The Windows 10 SDK (19041) doesn't expose shuffle/like via SMTC, and the Yandex app
>   ignores repeat commands entirely — those buttons are intentionally absent.
> - SMTC playback-position events are unreliable; I poll + interpolate instead.
> - Some tracks report `TimeSpan.MaxValue` positions — those have to be filtered out.
>
> Open source, MIT: **[LINK]**
> Would appreciate a star if it's useful, and happy to answer questions.

## RU — short (r/Pikabu, r/rusAskReddit, r/ru_anime? no — better: Telegram, ВК)

> Сделал бесплатный open-source виджет, который выводит **Яндекс Музыку** прямо в оверлей
> Xbox Game Bar (Win + G). Видно трек, обложку и прогресс поверх игры, можно листать и
> ставить на паузу, не выходя из игры.
>
> - Трек, обложка, прогресс с перемоткой (клик/перетаскивание, кнопки ±5 сек)
> - Play/pause, следующий, предыдущий
> - Окно можно менять в размере — интерфейс масштабируется
> - Windows 10 2004+ / 11, x64 и ARM64, подписанный MSIX
> - Без токенов и аккаунтов — работает через системную медиа-сессию (SMTC)
>
> **[LINK]** — ставится двойным кликом по `install.bat` (подтвердить UAC).
> MIT-лицензия, фидбек и пул-реквесты приветствуются.

## RU — long (habr-style)

> Сделал UWP-виджет, который встраивает Яндекс Музыку в оверлей Xbox Game Bar.
> В полноэкранной игре хочется видеть, что играет, и переключать треки без alt-tab —
> а у Яндекса, в отличие от Spotify, готового виджета для Game Bar нет.
>
> Виджет читает трек через системную медиа-сессию (SystemMediaTransportControls),
> которую публикует десктопное приложение Яндекс Музыки: никаких токенов, ключей API
> и входа в аккаунт. Автоматически выбирает именно яндексовскую сессию и игнорирует
> Spotify/Groove и прочие.
>
> Что внутри: название/исполнитель/обложка, прогресс-бар с перемоткой (клик или drag,
> плюс кнопки ±5 секунд), play/pause/следующий/предыдущий. Окно свободно меняет размер,
> а интерфейс масштабируется целиком (через Viewbox). Поставляется как подписанный MSIX,
> x64 и ARM64, с установщиком `install.bat` (двойной клик + подтвердить UAC).
>
> Пара технических граблей, если захотите сделать похожее:
> - В SDK 19041 нет shuffle/like через SMTC, а команду повтора Яндекс вообще игнорирует —
>   поэтому этих кнопок нет сознательно.
> - События позиции SMTC ненадёжны — пришлось делать поллинг с интерполяцией.
> - У части треков позиция приходит как `TimeSpan.MaxValue` — такие значения отбрасываются.
>
> Open source, MIT: **[LINK]**
> Звезда на GitHub очень поможет проекту жить, вопросы и пул-реквесты приветствуются.

# Ferrum

> Рефакторинг структуры и прямое логирование ServerConsole добавлены без запуска сборки и тестов по просьбе пользователя. Существующие бинарные артефакты относятся к предыдущей редакции.

Экспериментальный Rust-фреймворк для SCP: Secret Laboratory.

Первый PoC: **LabAPI → Ferrum.Loader → Ferrum.Interop → Rust → C# logging callback**.

- `Ferrum.Loader` — минимальный LabAPI-плагин и запуск проверки.
- `Ferrum.Interop` — C ABI, загрузка native-модуля, UTF-8 и владение памятью.
- `native/ferrum-native` — Rust cdylib для Windows/Linux x64.
- `tests` — межъязыковые smoke-проверки без игрового сервера.

Общие managed-настройки вынесены в Directory.Build.props; решение — Ferrum.slnx.
Архитектурное разделение ориентировано на EXILED; зависимости от EXILED нет.

Рабочие каталоги Ferrum создаются в `~/.config/Ferrum` на Windows и Linux:

```text
.config/Ferrum/
  Configs/
  Logs/
  Plugins/
    Dependencies/
```

Основной Rust-мост хранится в `Plugins/Dependencies`. Для изолированного экземпляра
корень можно переопределить через `FERRUM_HOME`. Запись логов в файлы и загрузка
пользовательских Rust-плагинов относятся к следующим этапам.

```powershell
./scripts/test.ps1 -Configuration Debug
./scripts/build.ps1 -Configuration Release
```

Документация: [структура и логирование](docs/architecture.md), [разработка и установка](docs/development.md),
[контракт ABI](docs/abi.md), [фактические проверки](docs/verification.md).

Горячая замена и полноценный Rust plugin SDK появятся на следующих этапах.

## Приветственный баннер

После успешной инициализации моста и события LabAPI PluginsEnabled выводится
FERRUM, благодарность, ветка, версия с префиксом v и список участников. Баннер использует ConsoleColor.DarkYellow — ближайший к оранжевому стандартный цвет.
Обычные сообщения Received UTF-8 и PoC passed при старте убраны; диагностика
FFI остаётся внутренней, ошибки инициализации по-прежнему видимы.

Ветка по умолчанию — alpha; допустимые значения MSBuild-свойства
FerrumReleaseBranch: alpha, beta, stable. Версия берётся из assembly.
Имена участников редактируются в CONTRIBUTORS.txt в корне проекта; файл
встраивается в Ferrum.Loader.dll при сборке. Текущий участник: Narin.

## Праздничные поздравления

Перед строкой Thank you for using and supporting the Ferrum framework! выводится:
- 25–31 октября включительно — Spooky scary skeletons! Happy Halloween!
- 25 декабря–7 января включительно — Happy New Year!
- В остальные даты дополнительная строка отсутствует.

Дата сервера берётся один раз через DateTime.Today при выводе баннера.
Выбор строки вынесен в публичный HolidayGreeting.GetMessage(DateTime date).
Цвет DarkYellow, логотип, благодарность, ветка, версия с v и участники сохраняются.
Период Нового года охватывает конец декабря и начало января независимо от года.

<div align="center">
  <!-- Иконка приложения: добавьте прямую ссылку на фото ниже -->
  <img src="https://raw.githubusercontent.com/laguser/autorun/main/icon.png" width="128" height="128" alt="Windows Autorun Manager Icon">

  <h1>Windows Autorun Manager</h1>
  <p><strong>Элегантный и быстрый менеджер автозагрузки для Windows, написанный на WPF (.NET 4.0).</strong></p>
  
  <p>
    <a href="#-особенности">🇷🇺 Русский</a> | <a href="#-features">🇬🇧 English</a>
  </p>
</div>

---

# 🇷🇺 Русский

## 🚀 Особенности

*   **Интерфейс**: Современный, минималистичный и приятный глазу интерфейс.
*   **Управление реестром и папкой Startup**: Включение и отключение автозагрузки как в стандартном Диспетчере задач.
*   **Поддержка Планировщика Задач**: Отслеживает и позволяет управлять задачами, запускаемыми при входе в систему.
*   **Drag & Drop**: Просто перетащите любой файл (`.exe`, `.bat`, `.cmd` или ярлык) в окно приложения, чтобы мгновенно добавить его в автозагрузку.
*   **Живой поиск**: Быстрая фильтрация списка программ.
*   **Импорт/Экспорт**: Резервное копирование и перенос настроек автозагрузки в формате JSON.
*   **Портативность**: Работает без установки (единый `.exe` файл).

<img src="https://i.yapx.ru/dj2Pa.png">

## 🛠 Установка

Программа не требует установки. Просто скачайте `autorun.exe` из раздела [Releases](../../releases) и запустите!
> **Требования:** Windows 7/8/10/11 (необходим .NET Framework 4.0 или новее, который встроен во все современные версии Windows).

## 🖱 Использование

1. Запустите приложение.
2. Используйте тумблеры для **включения/выключения** автозагрузки программ.
3. Нажмите крестик (`✕`) для полного **удаления** программы из автозагрузки.
4. Нажмите на заголовок категории (например, `РЕЕСТР`), чтобы включить/выключить все элементы в этой категории.
5. Для добавления новой программы — просто **перетащите** её файл в окно приложения.

## 👨‍💻 Сборка из исходников

Если вы хотите собрать приложение самостоятельно:

1. Склонируйте репозиторий.
2. Запустите `build.bat` в папке проекта.
3. Скомпилированный файл появится в `bin\Release\autorun.exe`.

*Примечание: Сборка использует стандартный MSBuild, поставляемый вместе с Windows, поэтому установка Visual Studio не обязательна.*

---

# 🇬🇧 English

## 🚀 Features

*   **User Interface**: Modern, minimalist, and visually pleasing design.
*   **Registry & Startup Folder Management**: Enable and disable autorun entries from both the registry and Windows Startup folder.
*   **Task Scheduler Support**: Track and manage tasks that run at system login.
*   **Drag & Drop**: Simply drag any file (`.exe`, `.bat`, `.cmd`, or shortcut) into the application window to instantly add it to autorun.
*   **Live Search**: Quickly filter the list of programs.
*   **Import/Export**: Backup and transfer autorun settings in JSON format.
*   **Portability**: Works without installation (single `.exe` file).

<img src="https://i.yapx.ru/dj2Pa.png">

## 🛠 Installation

The application requires no installation. Simply download `autorun.exe` from the [Releases](../../releases) section and run it!
> **Requirements:** Windows 7/8/10/11 (.NET Framework 4.0 or newer is required, which is built-in to all modern Windows versions).

## 🖱 Usage

1. Launch the application.
2. Use the toggles to **enable/disable** autorun entries for programs.
3. Click the X button (`✕`) to completely **remove** a program from autorun.
4. Click on a category title (e.g., `REGISTRY`) to enable/disable all items in that category.
5. To add a new program — simply **drag and drop** its file into the application window.

## 👨‍💻 Building from Source

If you want to build the application yourself:

1. Clone the repository.
2. Run `build.bat` in the project folder.
3. The compiled file will appear in `bin\Release\autorun.exe`.

*Note: The build uses the standard MSBuild that comes with Windows, so Visual Studio installation is not required.*

---

<div align="center">
  <p>Made with ❤️ by laguser</p>
</div>

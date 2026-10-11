# Лабораторная работа №1 — Автоматизация сборки 2D-игры через командную строку (CLI)

**Выполнил:** matiksiatik
**Репозиторий:** <https://github.com/matiksiatik/lr1-unity-build>
**Проект:** 2D Platformer Microgame (официальный шаблон Unity `com.unity.template.platformer` 5.0.7)
**Редактор:** Unity 6000.6.0f1 (Windows, модуль сборки WebGL)
**Целевая платформа сборки:** WebGL

## Цель работы

Изучить автоматическую сборку проекта Unity из командной строки (batch mode): подготовить скрипт-сборщик,
выполнить сборку WebGL без открытия графического редактора, проанализировать лог, проверить результат
локально и организовать процесс по схеме CI/CD с публикацией в GitHub (ветки, Pull Request, ревью).

## Окружение

| Компонент | Значение |
| --- | --- |
| ОС | Windows 10 Pro (x64) |
| Unity Editor | 6000.6.0f1 + модуль WebGL |
| Проект | `PlatformerMicrogame` |
| Основная сцена | `Assets/Scenes/SampleScene.unity` |

---

## Шаг 1. Инициализация проекта

Проект создан на базе официального шаблона **2D Platformer Microgame** — классического 2D-платформера
с готовыми сценами, скриптами, ассетами и обучающими материалами. Основная сцена игры
`Assets/Scenes/SampleScene.unity` добавлена и активна в настройках сборки проекта.

![Редактор Unity с открытым проектом](screenshots/01_editor_scene.png)

## Шаг 2. Создание C# скрипта сборщика

В проекте создана служебная папка `Assets/Editor`, в ней — скрипт
[`BuildManager.cs`](Assets/Editor/BuildManager.cs). Скрипт собирает список активных сцен,
запускает компиляцию проекта через `BuildPipeline.BuildPlayer()` и сообщает результат в лог.

Сцена присутствует в списке сборки (Build Profiles), платформа **Web** отмечена как активная:

![Build Profiles: сцена в списке сборки](screenshots/02_build_profiles.png)

<details>
<summary>Листинг кода BuildManager.cs</summary>

```csharp
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildManager
{
    // Путь для сохранения WebGL-версии игры
    private static readonly string WebGLBuildPath = "Builds/WebGL";

    /// <summary>
    /// Автоматический метод сборки игры под платформу WebGL
    /// (также доступен из меню редактора: Tools -> Build WebGL)
    /// </summary>
    [MenuItem("Tools/Build WebGL")]
    public static void BuildWebGL()
    {
        Debug.Log("[CI/CD] Запущен автоматический процесс сборки WebGL...");

        // 1. Получаем список сцен, включенных в настройки проекта (Build Settings)
        string[] levels = GetScenes();

        if (levels.Length == 0)
        {
            Debug.LogError("[CI/CD] Ошибка: В Настройках Сборки (Build Settings) не найдено ни одной активной сцены!");
            ExitWithCode(1);
            return;
        }

        // 2. Конфигурируем параметры сборки
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = levels,
            locationPathName = WebGLBuildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None // Для отладочной сборки можно использовать BuildOptions.Development
        };

        // 3. Запускаем компиляцию
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        // 4. Анализируем результаты
        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[CI/CD] УСПЕХ! WebGL билд успешно создан.");
            Debug.Log($"[CI/CD] Время сборки: {summary.totalTime.TotalSeconds:F2} сек. Размер: {summary.totalSize} байт.");
            ExitWithCode(0);
        }
        else
        {
            Debug.LogError($"[CI/CD] ОШИБКА СБОРКИ! Количество ошибок: {summary.totalErrors}");
            ExitWithCode(1);
        }
    }

    /// <summary>
    /// Вспомогательный метод для сбора всех активных сцен из проекта
    /// </summary>
    private static string[] GetScenes()
    {
        var editorScenes = EditorBuildSettings.scenes;

        // Считаем только те сцены, у которых стоит галочка "активна"
        int activeCount = 0;
        foreach (var scene in editorScenes)
        {
            if (scene.enabled) activeCount++;
        }

        string[] scenePaths = new string[activeCount];
        int index = 0;
        foreach (var scene in editorScenes)
        {
            if (scene.enabled)
            {
                scenePaths[index] = scene.path;
                index++;
            }
        }

        return scenePaths;
    }

    /// <summary>
    /// Корректное завершение процесса Unity в зависимости от режима запуска
    /// </summary>
    private static void ExitWithCode(int code)
    {
        // Если Unity запущена в batchmode (из консоли) — принудительно закрываем редактор с кодом возврата
        if (Environment.CommandLine.Contains("-batchmode"))
        {
            EditorApplication.Exit(code);
        }
    }
}
```

</details>

Файл скрипта в репозитории GitHub (ветка LR1):

![BuildManager.cs в репозитории](screenshots/04_buildmanager_github.png)

## Шаг 3. Отключение сжатия WebGL

Для локального запуска собранной игры (через локальный веб-сервер, без настройки HTTP-заголовков)
в настройках **Edit → Project Settings → Player → WebGL → Publishing Settings** установлен
**Compression Format = Disabled**.

![Project Settings: Player / WebGL](screenshots/03_player_settings.png)

Значение зафиксировано в настройках проекта: `ProjectSettings.asset → webGLCompressionFormat: 2 (Disabled)`.

## Шаг 4. Проверка скрипта через интерфейс Unity

Скрипт компилируется без ошибок. Для появления пункта в верхнем меню редактора в код добавлен
атрибут `[MenuItem("Tools/Build WebGL")]` (в листинге методички он опущен, однако меню Unity
не генерируется для статических методов автоматически — это работает только при наличии атрибута).
Теперь сборку можно запустить и вручную: **Tools → Build WebGL**.

## Шаг 5. Локальная сборка через терминал (CLI)

Проект полностью закрыт в редакторе. Из корневой папки проекта выполнена команда:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe" -batchmode -nographics -executeMethod BuildManager.BuildWebGL -quit -logFile build_webgl.log
```

![Запуск сборки из командной строки](screenshots/05_cli_build_command.png)

Используемые флаги:

| Флаг | Назначение |
| --- | --- |
| `-batchmode` | запуск Unity в фоновом режиме, без окон редактора |
| `-nographics` | без инициализации графического движка (для серверов сборки) |
| `-executeMethod BuildManager.BuildWebGL` | выполнить статический метод сборки сразу после открытия проекта |
| `-quit` | закрыть Unity после завершения метода |
| `-logFile build_webgl.log` | перенаправить лог Unity в файл |

## Шаг 6. Анализ результатов и логов сборки

В корне проекта создан файл `build_webgl.log`. Ключевые строки в конце лога:

```
[CI/CD] Запущен автоматический процесс сборки WebGL...
[CI/CD] УСПЕХ! WebGL билд успешно создан.
[CI/CD] Время сборки: 108,58 сек. Размер: 58108393 байт.
```

![Результат сборки: анализ лога и содержимое Builds/WebGL](screenshots/06_cli_build_success.png)

Скомпилированные файлы игры появились в папке `Builds/WebGL/`:

- `index.html` — стартовая страница игры;
- `Build/` — `WebGL.data`, `WebGL.wasm`, `WebGL.framework.js`, `WebGL.loader.js`;
- `TemplateData/` — ресурсы шаблона страницы;
- `GUID.txt`, `ProjectVersion.txt`.

## Шаг 7. Проверка работоспособности (локальный запуск)

Двойной клик по `index.html` не работает из-за политик безопасности браузеров (CORS), поэтому
папка сборки запущена через локальный статический веб-сервер (аналог расширения Live Server).
Игра успешно загрузилась: главный экран микроигры отображается, персонаж управляется с клавиатуры.

![Игра запущена в браузере через локальный сервер](screenshots/07_live_server_game.png)

## Шаг 8. Настройка Git и публикация

Создан файл `.gitignore` (содержимое — по методичке; добавлена строка `*.slnx` для служебных
solution-файлов Unity 6):

```gitignore
[L|l]ibrary/
[Tt]emp/
[Oo]bj/
[L|l]ogs/
[U|u]sers/
[B|b]uild/
[B|b]uilds/
[Uu]ser[Ss]ettings/
[Pp]roject[Ss]ettings/ProjectVersion.txt
*.cs.md
*.csproj
*.unityproj
*.sln
*.suo
*.user
*.userprefs
*.pidb
*.booproj
*.svd
*.pdb
*.opendb
*.VC.db
build_webgl.log
*.slnx
```

> Каталог `Builds/` и файл `build_webgl.log` исключены из репозитория: результаты сборки
> воспроизводятся командой из Шага 5.

История коммитов:

```bash
git init
git branch -M main
git add .
git commit -m "chore: initializing a 2D Platformer Microgame project"   # чистый шаблон без скрипта сборщика

git checkout -b LR1
git add Assets/Editor/BuildManager.cs
git commit -m "feat: added BuildManager script for build automation"

git add README.md
git commit -m "docs: Lab report #1 added"

git remote add origin https://github.com/matiksiatik/lr1-unity-build.git
git push -u origin main
git push -u origin LR1
```

Ветки `main` и `LR1` в репозитории:

![Ветки репозитория на GitHub](screenshots/08_github_branches.png)

## Шаг 9. Pull Request и Peer Review

Создан Pull Request **LR1 → main** (base: `main`, compare: `LR1`). После этого к PR добавляются
ревьюеры-одногруппники: они проверяют код скрипта и отчёт во вкладке Files Changed, оставляют
комментарии и аппрувы, после чего выполняется слияние ветки в `main`.

**Pull Request #1:** <https://github.com/matiksiatik/lr1-unity-build/pull/1>

![Pull Request #1: LR1 → main](screenshots/09_pull_request.png)

---

## Вывод

В ходе лабораторной работы освоена автоматическая сборка проекта Unity из командной строки
в batch-режиме: подготовлен скрипт-сборщик `BuildManager.cs`, настроена платформа WebGL
(со отключённым сжатием для локального запуска), выполнена сборка без открытия редактора
с анализом лога и проверкой артефактов. Результат проверен локально через веб-сервер.
Также отработан процесс CI/CD-подобного воркфлоу в Git: ветки `main`/`LR1`, отдельные
логические коммиты, публикация на GitHub и подготовка Pull Request с ревью.

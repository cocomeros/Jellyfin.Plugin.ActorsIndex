# Actors Index — Plugin per Jellyfin

Sfoglia tutti gli attori della tua libreria Jellyfin in un'unica schermata: griglia completa con foto, numero di apparizioni e collegamento diretto alla loro biografia.

![Actors Index screenshot](Jellyfin.Plugin.ActorsIndex/Images/thumb.png)

## Funzionalità

- **Griglia attori** — tutti gli attori/attrici ordinabili per numero di apparizioni o nome A→Z
- **Ricerca in tempo reale** — filtra per nome mentre scrivi
- **Paginazione** — dimensione pagina configurabile (default 60 attori per pagina)
- **Pulsante flottante** — aggiungi un pulsante 🎬 persistente su tutte le pagine Jellyfin
- **Integrazione canale** — attori accessibili come canale Jellyfin nella home
- **Filtro per apparizioni** — nascondi attori con meno di N apparizioni
- **Aggiornamento automatico** — si aggiorna da solo dalla pagina impostazioni, senza compilazione manuale

## Requisiti

- Jellyfin **10.11.x** o successivo
- .NET 9 (incluso in Jellyfin 10.11+)

## Installazione

### Metodo A — Repository personalizzato (consigliato)

1. In Jellyfin vai su **Dashboard → Plugin → Repository**
2. Clicca **+** e aggiungi:
   - Nome: `Actors Index`
   - URL: `https://raw.githubusercontent.com/cocomeros/Jellyfin.Plugin.ActorsIndex/main/manifest.json`
3. Vai su **Dashboard → Plugin → Catalogo**, trova **Actors Index**, clicca **Installa**
4. Riavvia Jellyfin

### Metodo B — Installazione manuale

1. Scarica l'ultimo `Jellyfin.Plugin.ActorsIndex_x.x.x.x.zip` dalla pagina [Releases](https://github.com/cocomeros/Jellyfin.Plugin.ActorsIndex/releases)
2. Estrai e copia tutti i file nella cartella plugin di Jellyfin:
   - **Linux**: `/var/lib/jellyfin/plugins/ActorsIndex/`
   - **Windows**: `%LOCALAPPDATA%\jellyfin\plugins\ActorsIndex\`
3. Riavvia Jellyfin

## Configurazione iniziale

Dopo aver installato e riavviato Jellyfin:

1. Vai su **Dashboard → Plugin → Actors Index → Impostazioni**
2. Nella sezione **"Pulsante nella Home"** clicca **🎬 Inietta pulsante nella Home**
   — questo aggiunge un pulsante persistente su tutte le pagine Jellyfin

> **Solo Linux:** la funzione "Inietta" richiede il permesso di scrittura su `/usr/share/jellyfin/web/index.html`.
> Per concederlo in modo permanente, esegui questo comando **una volta sola**:
> ```bash
> sudo bash -c 'mkdir -p /etc/systemd/system/jellyfin.service.d && printf "[Service]\nExecStartPre=+/bin/chown jellyfin /usr/share/jellyfin/web/index.html\n" > /etc/systemd/system/jellyfin.service.d/fix-webroot-perms.conf && systemctl daemon-reload && systemctl restart jellyfin'
> ```
> Dopo un aggiornamento di Jellyfin, clicca di nuovo **"Inietta"** per ripristinare il pulsante.

---

## Cosa fare dopo un aggiornamento di Jellyfin

### Il pulsante 🎬 è sparito dalla Home

Jellyfin ha sovrascritto `index.html` durante l'aggiornamento.

**Soluzione:** Dashboard → Plugin → Actors Index → **"Inietta pulsante nella Home"**. Fine.

---

### Errore rosso al click su "Inietta" (o "Rimuovi")

Il plugin è compilato per la versione precedente di Jellyfin e non è più compatibile.

**Soluzione in 4 click:**

1. Dashboard → Plugin → Actors Index → **"Controlla aggiornamenti"**
2. Se appare una versione più recente → clicca **"Scarica e installa aggiornamento"**
3. Clicca **"Riavvia Jellyfin"**
4. Clicca **"Inietta pulsante nella Home"**

✅ Nessun terminale, nessuna compilazione.

---

### Errore anche dopo l'aggiornamento del plugin (solo Linux)

Dopo `apt upgrade jellyfin` su Debian/Ubuntu, `index.html` torna di proprietà di `root` e il plugin non ci può scrivere.

**Soluzione:** riesegui il comando una-tantum indicato nella sezione "Configurazione iniziale" qui sopra. Va rifatto solo se disinstalli e reinstalli Jellyfin completamente.

---

## Come funziona l'aggiornamento automatico

Il plugin si aggiorna automaticamente senza che tu debba compilare nulla:

1. Quando Jellyfin pubblica una nuova versione, **Renovate** apre automaticamente una Pull Request su questo repository per aggiornare le dipendenze
2. Alla chiusura della PR, **GitHub Actions** compila il plugin, crea uno ZIP firmato e pubblica una nuova [Release](https://github.com/cocomeros/Jellyfin.Plugin.ActorsIndex/releases)
3. Tu clicchi **"Controlla aggiornamenti"** nelle impostazioni del plugin — fa tutto da solo

## Compilazione dal sorgente (solo sviluppatori)

```bash
git clone https://github.com/cocomeros/Jellyfin.Plugin.ActorsIndex.git
cd Jellyfin.Plugin.ActorsIndex
dotnet publish --configuration=Release Jellyfin.Plugin.ActorsIndex.sln
```

Output: `Jellyfin.Plugin.ActorsIndex/bin/Release/net9.0/publish/`

## Licenza

[GPL-3.0](LICENSE)

This is the main class for your plugin and will reside in the root of your project. It will define your name, version and Id. It should inherit from `MediaBrowser.Common.Plugins.BasePlugin<PluginConfiguration>`

It should look something like the following:
```c#
    using MediaBrowser.Common.Plugins;
    using MyJellyfinPlugin.Configuration;

    namespace MyJellyfinPlugin;

    class Plugin : BasePlugin<PluginConfiguration>
    {

    }
```

Note: If you called your PluginConfiguration class something different, you need to put that between the <>

### Implement Required Properties

The Plugin class needs a few properties implemented before it can work correctly.

It needs an override on ID, an override on Name, and a constructor that follows a specific model. To get started you can use the following section.

```c#
public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer){}
public override string Name => throw new System.NotImplementedException();
public override Guid Id => Guid.Parse("");
```

## 3. Customize Plugin Information

You need to populate some of your plugin's information. Go ahead a put in a string of the Name you've overridden name, and generate a GUID

- **Windows Users**: you can use the Powershell command `New-Guid`, `[guid]::NewGuid()` or the Visual Studio GUID generator

- **Linux and OS X Users**: you can use the Powershell Core command `New-Guid` or this command from your shell of choice:

   ```bash
   od -x /dev/urandom | head -n1 | awk '{OFS="-"; srand($6); sub(/./,"4",$5); sub(/./,substr("89ab",1+rand()*4,1),$6); print $2$3,$4,$5,$6,$7$8$9}'
   ```

or

   ```bash
   uuidgen
   ```

- Place that guid inside the `Guid.Parse("")` quotes to define your plugin's ID.

## 4. Adding Functionality

Congratulations, you now have everything you need for a perfectly functional functionless Jellyfin plugin! You can try it out right now if you'd like by compiling it, then placing the dll you generate in a subfolder (named after your plugin for example) within the plugins folder under your Jellyfin directory (Normally C:\Users\{YourUserName}\AppData\Local\jellyfin\plugins). If you want to try and hook it up to a debugger make sure you copy the generated PDB file alongside it.

Most people aren't satisfied with just having an entry in a menu for their plugin, most people want to have some functionality, so lets look at how to add it.

### 4a. Implement Interfaces

If the functionality you are trying to add is functionality related to something that Jellyfin has an interface for you're in luck. Jellyfin uses some automatic discovery and injection to allow any interfaces you implement in your plugin to be available in Jellyfin.

Here's some interfaces you could implement for common use cases:

- **IAuthenticationProvider** - Allows you to add an authentication provider that can authenticate a user based on a name and a password, but that doesn't expect to deal with local users.
- **IBaseItemComparer** - Allows you to add sorting rules for dealing with media that will show up in sort menus
- **IIntroProvider** - Allows you to play a piece of media before another piece of media (i.e. a trailer before a movie, or a network bumper before an episode of a show)
- **IItemResolver** - Allows you to define custom media types
- **ILibraryPostScanTask** - Allows you to define a task that fires after scanning a library
- **IMetadataSaver** - Allows you to define a metadata standard that Jellyfin can use to write metadata
- **IResolverIgnoreRule** - Allows you to define subpaths that are ignored by media resolvers for use with another function (i.e. you wanted to have a theme song for each tv series stored in a subfolder that could be accessed by your plugin for playback in a menu).
- **IScheduledTask** - Allows you to create a scheduled task that will appear in the scheduled task lists on the dashboard.

There are loads of other interfaces that can be used, but you'll need to poke around the API to get some info. If you're an expert on a particular interface, you should help [contribute some documentation](https://docs.jellyfin.org/general/contributing/index.html)!

### 4b. Use plugin aimed interfaces to add custom functionality

If your plugin doesn't fit perfectly neatly into a predefined interface, never fear, there are a set of interfaces and classes that allow your plugin to extend Jellyfin any which way you please. Here's a quick overview on how to use them

- **IPluginConfigurationPage** - Allows you to have a plugin config page on the dashboard. If you used one of the quickstart example projects, a premade page with some useful components to work with has been created for you! If not you can check out this guide here for how to whip one up.

 **IPluginServiceRegistrator** - Will be located by Jellyfin at server startup and allows you to add services to the DI container to allow for injection in your plugin's classes later.

- **IHostedService** - Allows you to run code as a background task that will be started at program startup and will remain in memory. See [Microsoft's documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-8.0&tabs=visual-studio#ihostedservice-interface) for more information. You can make as many of these as you need; make Jellyfin aware of them with an `IPluginServiceRegistrator`. It is wildly useful for loading configs or persisting state. **Be aware that your main plugin class (IBasePlugin) cannot also be a IHostedService.**

- **ControllerBase** - Allows you to define custom REST-API endpoints. This is the default ASP.NET Web-API controller. You can use it exactly as you would in a normal Web-API project. Learn more about it [here](https://docs.microsoft.com/aspnet/core/web-api/?view=aspnetcore-5.0).

Likewise you might need to get data and services from the Jellyfin core, Jellyfin provides a number of interfaces you can add as parameters to your plugin constructor which are then made available in your project (you can see the 2 mandatory ones that are needed by the plugin system in the constructor as is).

- **IBlurayExaminer** - Allows you to examine blu-ray folders
- **IDtoService** - Allows you to create data transport objects, presumably to send to other plugins or to the core
- **ILibraryManager** - Allows you to directly access the media libraries without hopping through the API
- **ILocalizationManager** - Allows you tap into the main localization engine which governs translations, rating systems, units etc...
- **INetworkManager** - Allows you to get information about the server's networking status
- **IServerApplicationPaths** - Allows you to get the running server's paths
- **IServerConfigurationManager** - Allows you to write or read server configuration data into the application paths
- **ITaskManager** - Allows you to execute and manipulate scheduled tasks
- **IUserManager** - Allows you to retrieve user info and user library related info
- **IXmlSerializer** - Allows you to use the main xml serializer
- **IZipClient** - Allows you to use the core zip client for compressing and decompressing data

## 5. Create a Repository

- [See blog post](https://jellyfin.org/posts/plugin-updates/)

## 6. Set Up Debugging

Debugging can be set up by creating tasks which will be executed when running the plugin project. The specifics on setting up these tasks are not included as they may differ from IDE to IDE. The following list describes the general process:

- Compile the plugin in debug mode.
- Create the plugin directory if it doesn't exist.
- Copy the plugin into your server's plugin directory. The server will then execute it.
- Make sure to set the working directory of the program being debugged to the working directory of the Jellyfin Server.
- Start the server.

Some IDEs like Visual Studio Code may need the following compile flags to compile the plugin:

```shell
dotnet build Your-Plugin.sln /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

These flags generate the full paths for file names and **do not** generate a summary during the build process as this may lead to duplicate errors in the problem panel of your IDE.

### 6.a Set Up Debugging on Visual Studio

Visual Studio allows developers to connect to other processes and debug them, setting breakpoints and inspecting the variables of the program. We can set this up following this steps:
On this section we will explain how to set up our solution to enable debugging before the server starts.

1. Right-click on the solution, And click on Add -> Existing Project...
2. Locate Jellyfin executable in your installation folder and click on 'Open'. It is called `Jellyfin.exe`. Now The solution will have a new "Project" called Jellyfin. This is the executable, not the source code of Jellyfin.
3. Right-click on this new project and click on 'Set up as Startup Project'
4. Right-click on this new project and click on 'Properties'
5. Make sure that the 'Attach' parameter is set to 'No'

From now on, everytime you click on start from Visual Studio, it will start Jellyfin attached to the debugger!

The only thing left to do is to compile the project as it is specified a few lines above and you are done.

### 6.b Automate the Setup on Visual Studio Code

Visual Studio Code allows developers to automate the process of starting all necessary dependencies to start debugging the plugin. This guide assumes the reader is familiar with the [documentation on debugging in Visual Studio Code](https://code.visualstudio.com/docs/editor/debugging) and has read the documentation in this file. It is assumed that the Jellyfin Server has already been compiled once. However, should one desire to automatically compile the server before the start of the debugging session, this can be easily implemented, but is not further discussed here.

A full example, which aims to be portable may be found in this repo's `.vscode` folder.

This example expects you to clone `jellyfin`, `jellyfin-web` and `jellyfin-plugin-template` under the same parent directory, though you can customize this in `settings.json`

1. Create a `settings.json` file inside your `.vscode` folder, to specify common options specific to your local setup.
   ```jsonc
    {
        // jellyfinDir : The directory of the cloned jellyfin server project
        // This needs to be built once before it can be used
        "jellyfinDir"     : "${workspaceFolder}/../jellyfin/Jellyfin.Server",
        // jellyfinWebDir : The directory of the cloned jellyfin-web project
        // This needs to be built once before it can be used
        "jellyfinWebDir"  : "${workspaceFolder}/../jellyfin-web",
        // jellyfinDataDir : the root data directory for a running jellyfin instance
        // This is where jellyfin stores its configs, plugins, metadata etc
        // This is platform specific by default, but on Windows defaults to
        // ${env:LOCALAPPDATA}/jellyfin
        "jellyfinDataDir" : "${env:LOCALAPPDATA}/jellyfin",
        // The name of the plugin
        "pluginName" : "Jellyfin.Plugin.Template",
    }
   ```

1. To automate the launch process, create a new `launch.json` file for C# projects inside the `.vscode` folder. The example below shows only the relevant parts of the file. Adjustments to your specific setup and operating system may be required.

   ```jsonc
    {
        // Paths and plugin names are configured in settings.json
        "version": "0.2.0",
        "configurations": [
            {
                "type": "coreclr",
                "name": "Launch",
                "request": "launch",
                "preLaunchTask": "build-and-copy",
                "program": "${config:jellyfinDir}/bin/Debug/net8.0/jellyfin.dll",
                "args": [
                //"--nowebclient"
                "--webdir",
                "${config:jellyfinWebDir}/dist/"
                ],
                "cwd": "${config:jellyfinDir}",
            }
        ]
    }

   ```

   The `request` type is specified as `launch`, as this `launch.json` file will start the Jellyfin Server process. The `preLaunchTask` defines a task that will run before the Jellyfin Server starts. More on this later. It is important to set the `program` path to the Jellyin Server program and set the current working directory (`cwd`) to the working directory of the Jellyfin Server.
   The `args` option allows to specify arguments to be passed to the server, e.g. whether Jellyfin should start with the web-client or without it.

2. Create a `tasks.json` file inside your `.vscode` folder and specify a `build-and-copy` task that will run in `sequence` order. This tasks depends on multiple other tasks and all of those other tasks can be defined as simple `shell` tasks that run commands like the `cp` command to copy a file. The sequence to run those tasks in is given below. Please note that it might be necessary to adjust the examples for your specific setup and operating system.

   The full file is shown here - Specific sections will be discussed in depth
    ```jsonc
    {
        // Paths and plugin name are configured in settings.json
        "version": "2.0.0",
        "tasks": [
            {
            // A chain task - build the plugin, then copy it to your
            // jellyfin server's plugin directory
            "label": "build-and-copy",
            "dependsOrder": "sequence",
            "dependsOn": ["build", "make-plugin-dir", "copy-dll"]
            },
            {
            // Build the plugin
            "label": "build",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "publish",
                "${workspaceFolder}/${config:pluginName}.sln",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "group": "build",
            "presentation": {
                "reveal": "silent"
            },
            "problemMatcher": "$msCompile"
            },
            {
                // Ensure the plugin directory exists before trying to use it
                "label": "make-plugin-dir",
                "type": "shell",
                "command": "mkdir",
                "args": [
                "-Force",
                "-Path",
                "${config:jellyfinDataDir}/plugins/${config:pluginName}/"
                ]
            },
            {
                // Copy the plugin dll to the jellyfin plugin install path
                // This command copies every .dll from the build directory to the plugin dir
                // Usually, you probablly only need ${config:pluginName}.dll
                // But some plugins may bundle extra requirements
                "label": "copy-dll",
                "type": "shell",
                "command": "cp",
                "args": [
                "./${config:pluginName}/bin/Debug/net8.0/publish/*",
                "${config:jellyfinDataDir}/plugins/${config:pluginName}/"
                ]

            },
        ]
    }

    ```
    1.  The "build-and-copy" task which triggers all of the other tasks
    ```jsonc
        {
        // A chain task - build the plugin, then copy it to your
        // jellyfin server's plugin directory
        "label": "build-and-copy",
        "dependsOrder": "sequence",
        "dependsOn": ["build", "make-plugin-dir", "copy-dll"]
        },
    ```
    2.  A build task. This task builds the plugin without generating summary, but with full paths for file names enabled.

        ```jsonc
            {
            // Build the plugin
            "label": "build",
            "command": "dotnet",
            "type": "shell",
            "args": [
                "publish",
                "${workspaceFolder}/${config:pluginName}.sln",
                "/property:GenerateFullPaths=true",
                "/consoleloggerparameters:NoSummary"
            ],
            "group": "build",
            "presentation": {
                "reveal": "silent"
            },
            "problemMatcher": "$msCompile"
            },
        ```

    3.  A tasks which creates the necessary plugin directory and a sub-folder for the specific plugin. The plugin directory is located below the [data directory](https://jellyfin.org/docs/general/administration/configuration.html) of the Jellyfin Server. As an example, the following path can be used for the bookshelf plugin: `$HOME/.local/share/jellyfin/plugins/Bookshelf/`
        ```jsonc
            {
                // Ensure the plugin directory exists before trying to use it
                "label": "make-plugin-dir",
                "type": "shell",
                "command": "mkdir",
                "args": [
                "-Force",
                "-Path",
                "${config:jellyfinDataDir}/plugins/${config:pluginName}/"
                ]
            },
        ```

    4.  A tasks which copies the plugin dll which has been built in step 2.1. The file is copied into it's specific plugin directory within the server's plugin directory.

        ```jsonc
            {
                // Copy the plugin dll to the jellyfin plugin install path
                // This command copies every .dll from the build directory to the plugin dir
                // Usually, you probablly only need ${config:pluginName}.dll
                // But some plugins may bundle extra requirements
                "label": "copy-dll",
                "type": "shell",
                "command": "cp",
                "args": [
                "./${config:pluginName}/bin/Debug/net8.0/publish/*",
                "${config:jellyfinDataDir}/plugins/${config:pluginName}/"
                ]
            },
        ```

## Licensing

Licensing is a complex topic. This repository features a GPLv3 license template that can be used to provide a good default license for your plugin. You may alter this if you like, but if you do a permissive license must be chosen.

Due to how plugins in Jellyfin work, when your plugin is compiled into a binary, it will link against the various Jellyfin binary NuGet packages. These packages are licensed under the GPLv3. Thus, due to the nature and restrictions of the GPL, the binary plugin you get will also be licensed under the GPLv3.

If you accept the default GPLv3 license from this template, all will be good. However if you choose a different license, please keep this fact in mind, as it might not always be obvious that an, e.g. MIT-licensed plugin would become GPLv3 when compiled.

Please note that this also means making "proprietary", source-unavailable, or otherwise "hidden" plugins for public consumption is not permitted. To build a Jellyfin plugin for distribution to others, it must be under the GPLv3 or a permissive open-source license that can be linked against the GPLv3.

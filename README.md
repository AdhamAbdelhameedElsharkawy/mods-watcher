# ModsWatcher

**ModsWatcher** is a mod library manager and version tracker designed for any app or game but was inspired by simulation games like **American Truck Simulator (ATS)** and **Euro Truck Simulator 2 (ETS2)**. It uses a built-in browser engine to "watch" mod pages and notify you the moment a new version is available.

---

## 🚀 Getting Started

### Installation
1. Download the latest `ModsWatcher.zip` from the [Releases](https://github.com/AdhamAbdelhameedElsharkawy/mods-watcher/releases) section.
2. Extract the ZIP to a folder (e.g., `C:\Games\ModsWatcher`).
3. Launch `ModsWatcher.Desktop.exe`.

> **Initial Setup:** On the first launch, the app will download the Chromium engine (approx. 130 seconds). A setup window will track the progress.

---

## 🛠 How to Use

### 1. Register an Application
Before watching mods, you need to add the game or app they belong to.
* Go to the **App Selection** screen.
* Click **Add New App**.
* Enter the name (e.g., "ATS") and other information.

### 2. Add a Mod to Watch
Once an app is selected, you can start building your library:
* Click **Add Mod**.
* Provide the **Source URL** (e.g., a Nexus Mods or Forum page).
* Enter the **Mod Name** and your **Current Version**.

### 3. Setup the "Watcher" & Deep Crawler
Root URL & Watcher XPath: This is the primary mod page where you set a specific XPath to monitor the main version number.

Crawled Pages (Link Selector View): Users choose specific pages containing download links from the Link Selector View; the app will then crawl all these selected pages.

Deep Crawl XPaths: For each of the crawled pages, you set additional XPaths to extract specific mod information and metadata.

Example Workflow:

Root: https://nexusmods.com/ets2/mods/1 -> XPath: //div[@id='version'].

Link Selector: Choose the specific pages/links from the list that lead to mod downloads.

Deep Crawl: Use secondary XPaths on those pages to extract mod details or file info.

### 4. Tracking Updates
* The **Library View** displays all installed mods.
* App cards show a summary, including the **number of mods used** and pending updates.
* When a crawl detects a version mismatch, the mod will be flagged so you know exactly what needs updating.

---

## 🏗 Technical Details
* **Core:** .NET 10.0 / WPF
* **Browser Engine:** Playwright (Chromium)
* **Database:** SQLite (Local)
* **Architecture:** Self-contained sidecar (no external dependencies required).

## 🔒 Privacy & Security
Local Processing: All crawling and mod management are performed locally on your machine; no data is sent to external servers.

No Account Required: ModsWatcher does not require you to log in or share your game credentials.

Safe Storage: Your mod library data is stored in a local SQLite database (mods.db) within the application folder.

Open Source: The full source code is available here for audit to ensure transparency and security.

## 📄 License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
